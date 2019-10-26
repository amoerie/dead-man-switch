using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch.Internal
{
    internal interface IDeadManSwitchWatcher
    {
        ValueTask<DeadManSwitchResult> WatchAsync(CancellationToken cancellationToken);
    }

    internal sealed class DeadManSwitchWatcher : IDeadManSwitchWatcher
    {
        private readonly IDeadManSwitchContext _context;
        private readonly DeadManSwitchOptions _options;
        private readonly IDeadManSwitchTriggerer _deadManSwitchTriggerer;
        private readonly ILogger _logger;

        public DeadManSwitchWatcher(IDeadManSwitchContext deadManSwitchContext,
            DeadManSwitchOptions deadManSwitchOptions,
            IDeadManSwitchTriggerer deadManSwitchTriggerer,
            ILogger logger)
        {
            _context = deadManSwitchContext ?? throw new ArgumentNullException(nameof(deadManSwitchContext));
            _options = deadManSwitchOptions ?? throw new ArgumentNullException(nameof(deadManSwitchOptions));
            _deadManSwitchTriggerer = deadManSwitchTriggerer ?? throw new ArgumentNullException(nameof(deadManSwitchTriggerer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Starts a task that watches the dead man's switch.
        /// When no notifications are received within the proper timeout period, the <see cref="CancellationToken"/> will be cancelled automatically.
        /// You should pass this cancellation token to any task that must be cancelled.
        /// </summary>
        /// <returns>A value indicating whether the dead man's switch triggered or not</returns>
        public async ValueTask<DeadManSwitchResult> WatchAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Watching dead man's switch");

            var status = DeadManSwitchStatus.NotificationReceived;

            while (!cancellationToken.IsCancellationRequested)
            {
                switch (status)
                {
                    case DeadManSwitchStatus.Resumed:
                        continue;
                    case DeadManSwitchStatus.Suspended:
                    {
                        _logger.LogDebug("The dead man's switch is suspended. The worker will not be cancelled until the dead man's switch is resumed");

                        // ignore any notifications and wait until the switch goes through the 'Resumed' status
                        while (status != DeadManSwitchStatus.Resumed)
                        {
                            try
                            {
                                status = await _context.DequeueStatusAsync(cancellationToken).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                _logger.LogDebug("Dead man switch was canceled while waiting to be resumed.");
                                return DeadManSwitchResult.DeadManSwitchWasNotTriggered;
                            }
                        }

                        _logger.LogDebug("The dead man's switch is now resuming.");
                        break;
                    }
                    case DeadManSwitchStatus.NotificationReceived:
                        using (var timeoutCTS = new CancellationTokenSource(_options.Timeout))
                        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCTS.Token))
                        {
                            try
                            {
                                status = await _context.DequeueStatusAsync(cts.Token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                if (timeoutCTS.IsCancellationRequested)
                                {
                                    await _deadManSwitchTriggerer.TriggerAsync(cancellationToken).ConfigureAwait(false);

                                    return DeadManSwitchResult.DeadManSwitchWasTriggered;
                                }

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    _logger.LogDebug("Dead man switch watcher was canceled while waiting for the next notification");
                                    return DeadManSwitchResult.DeadManSwitchWasNotTriggered;
                                }
                            }
                        }

                        break;
                }
            }

            _logger.LogDebug("Dead man switch watcher was canceled");
            return DeadManSwitchResult.DeadManSwitchWasNotTriggered;
        }
    }
}