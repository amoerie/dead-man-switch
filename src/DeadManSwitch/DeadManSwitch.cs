using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch
{
    public sealed class DeadManSwitch : IDeadManSwitch
    {
        private readonly ILogger _logger;
        private readonly TimeSpan _timeout;
        private readonly CancellationTokenSource _taskCancellationTokenSource;
        private readonly Channel<DeadManSwitchNotification> _notifications;
        private readonly Channel<DeadManSwitchStatus> _statuses;
        
        public CancellationToken CancellationToken => _taskCancellationTokenSource.Token;

        public IEnumerable<DeadManSwitchNotification> Notifications
        {
            get
            {
                while (_notifications.Reader.TryRead(out var notification))
                    yield return notification;
            }
        }

        public DeadManSwitch(DeadManSwitchOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _logger = options.Logger ?? throw new ArgumentException($"{nameof(DeadManSwitch)} requires a logger. Please provide one on {nameof(DeadManSwitchOptions)}");
            _timeout = options.Timeout;
            _notifications = Channel.CreateBounded<DeadManSwitchNotification>(new BoundedChannelOptions(options.NumberOfNotificationsToKeep)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
            _statuses = Channel.CreateUnbounded<DeadManSwitchStatus>();
            _taskCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(options.CancellationToken);
        }

        public async ValueTask NotifyAsync(string notification)
        {
             _logger.LogTrace($"{nameof(DeadManSwitch)} notification: {notification}");
            
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            await _notifications.Writer.WriteAsync(new DeadManSwitchNotification(notification), CancellationToken).ConfigureAwait(false);
            await _statuses.Writer.WriteAsync(DeadManSwitchStatus.NotificationReceived, CancellationToken).ConfigureAwait(false);
        }

        public ValueTask PauseAsync()
        {
            return _statuses.Writer.WriteAsync(DeadManSwitchStatus.Paused, CancellationToken);
        }

        public ValueTask ResumeAsync()
        {
            return _statuses.Writer.WriteAsync(DeadManSwitchStatus.Resumed, CancellationToken);
        }

        public async ValueTask<DeadManSwitchResult> RunAsync(CancellationToken deadManSwitchCancellationToken)
        {
            var logger = _logger;
            
            logger.LogDebug("Running dead man's switch");

            var status = DeadManSwitchStatus.NotificationReceived;
        
            while (!deadManSwitchCancellationToken.IsCancellationRequested)
            {
                if (status == DeadManSwitchStatus.Paused)
                {
                    logger.LogDebug("The dead man switch is paused. The worker task will not be cancelled until the dead man switch is resumed");

                    // ignore any notifications and wait until the switch goes through the 'Resumed' status
                    while (status != DeadManSwitchStatus.Resumed)
                    {
                        try
                        {
                            status = await _statuses.Reader.ReadAsync(deadManSwitchCancellationToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            logger.LogDebug("Dead man switch was canceled while waiting to be resumed.");
                            return DeadManSwitchResult.DeadManSwitchWasNotTriggered;
                        }
                    }

                    logger.LogDebug("The dead man switch is now resuming.");
                }

                using (var timeoutCancellationTokenSource = new CancellationTokenSource(_timeout))
                using (var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(deadManSwitchCancellationToken, timeoutCancellationTokenSource.Token))
                {
                    try
                    {
                        status = await _statuses.Reader.ReadAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        if (timeoutCancellationTokenSource.IsCancellationRequested)
                        {
                            TriggerDeadManSwitch();

                            return DeadManSwitchResult.DeadManSwitchWasTriggered;
                        }

                        if (deadManSwitchCancellationToken.IsCancellationRequested)
                        {
                            logger.LogDebug("Dead man switch was canceled while waiting for the next notification");
                            return DeadManSwitchResult.DeadManSwitchWasNotTriggered;
                        }
                    }
                }
            }

            logger.LogDebug("Dead man switch was canceled");
            return DeadManSwitchResult.DeadManSwitchWasNotTriggered;
        }

        private void TriggerDeadManSwitch()
        {
            _statuses.Writer.Complete();
            _notifications.Writer.Complete();

            var trace = string.Join(Environment.NewLine, Notifications.Select(n => n.ToString()));
            _logger.LogWarning($"The worker task did not notify the dead man's switch within the agreed timeout of {_timeout.TotalSeconds}s " +
                        $"and will be cancelled.{Environment.NewLine}" +
                        $"Notification trace: {Environment.NewLine}{trace}");

            _taskCancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            _taskCancellationTokenSource?.Dispose();
        }
    }
}