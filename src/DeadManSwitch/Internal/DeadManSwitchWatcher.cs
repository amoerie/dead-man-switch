using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Logging;

namespace DeadManSwitch.Internal
{
    internal interface IDeadManSwitchWatcher
    {
        Task WatchAsync(CancellationToken cancellationToken);
    }

    internal sealed class DeadManSwitchWatcher : IDeadManSwitchWatcher
    {
        private readonly IDeadManSwitchContext _context;
        private readonly DeadManSwitchOptions _options;
        private readonly IDeadManSwitchTriggerer _deadManSwitchTriggerer;
        private readonly IDeadManSwitchLogger<DeadManSwitchWatcher> _logger;

        public DeadManSwitchWatcher(IDeadManSwitchContext deadManSwitchContext,
            DeadManSwitchOptions deadManSwitchOptions,
            IDeadManSwitchTriggerer deadManSwitchTriggerer,
            IDeadManSwitchLogger<DeadManSwitchWatcher> logger)
        {
            _context = deadManSwitchContext ?? throw new ArgumentNullException(nameof(deadManSwitchContext));
            _options = deadManSwitchOptions ?? throw new ArgumentNullException(nameof(deadManSwitchOptions));
            _deadManSwitchTriggerer = deadManSwitchTriggerer ?? throw new ArgumentNullException(nameof(deadManSwitchTriggerer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Starts a worker that watches the dead man's switch.
        /// When no notifications are received within the proper timeout period, the <see cref="CancellationToken"/> will be cancelled automatically.
        /// You should pass this cancellation token to any worker that must be cancelled.
        /// </summary>
        /// <returns>A value indicating whether the dead man's switch triggered or not</returns>
        public async Task WatchAsync(CancellationToken cancellationToken)
        {
            _logger.Debug("Watching dead man's switch");

            while (!cancellationToken.IsCancellationRequested)
            {
                // TODO: IsSuspended
                TimeSpan timeSinceLastNotification;

                if (!_context.IsSuspended)
                {
                    timeSinceLastNotification = TimeSpan.FromTicks(DateTimeOffset.UtcNow.UtcTicks - _context.LastNotifiedTicks); ;

                    if (timeSinceLastNotification > _options.Timeout)
                    {
                        _deadManSwitchTriggerer.Trigger();
                        return;
                    }
                } 
                else
                {
                    _logger.Debug("The dead man's switch is suspended. The worker will not be cancelled until the dead man's switch is resumed");

                    timeSinceLastNotification = TimeSpan.Zero;
                }

                var timeRemaining = _options.Timeout - timeSinceLastNotification;

                await Task.Delay(timeRemaining, cancellationToken)
                    .ConfigureAwait(false);
            }

            _logger.Debug("Dead man switch watcher was canceled");
        }
    }
}