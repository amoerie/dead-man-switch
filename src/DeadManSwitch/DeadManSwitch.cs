using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch
{
    public interface IDeadManSwitch 
    {
        /// <summary>
        /// Notifies the dead man's switch, postponing the cancellation of the <see cref="IDeadManSwitchWorker{TResult}"/> worker
        /// </summary>
        /// <param name="notification">
        /// A notification message that will be shown when the worker worker is cancelled.
        /// This can be useful to retrace the last steps of the worker worker.
        /// </param>
        /// <param name="cancellationToken">A cancellation token that will cancel the notification</param>
        /// <returns>A <see cref="ValueTask"/></returns>
        ValueTask NotifyAsync(string notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pauses the dead man's switch. The worker worker cannot be cancelled until the dead man's switch is resumed.
        /// </summary>
        ///<param name="cancellationToken">A cancellation token that will cancel the suspension of the dead man's switch</param>
        /// <returns>A <see cref="ValueTask"/></returns>
        ValueTask SuspendAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Resumes the dead man's switch after pausing it.
        /// </summary>
        ///<param name="cancellationToken">A cancellation token that will cancel the resumption of the dead man's switch</param>
        /// <returns>A <see cref="ValueTask"/></returns>
        ValueTask ResumeAsync(CancellationToken cancellationToken = default);
    }

    public sealed class DeadManSwitch : IDeadManSwitch
    {
        private readonly IDeadManSwitchContext _context;
        private readonly ILogger _logger;

        public DeadManSwitch(IDeadManSwitchContext context, ILogger logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask NotifyAsync(string notification, CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("Received notification: {Notification}", notification);

            if (notification == null) throw new ArgumentNullException(nameof(notification));

            var addNotification = _context.AddNotificationAsync(new DeadManSwitchNotification(notification), cancellationToken);
            var enqueueStatus = _context.EnqueueStatusAsync(DeadManSwitchStatus.NotificationReceived, cancellationToken);

            await addNotification.ConfigureAwait(false);
            await enqueueStatus.ConfigureAwait(false);
        }

        public ValueTask SuspendAsync(CancellationToken cancellationToken = default)
        {
            return _context.EnqueueStatusAsync(DeadManSwitchStatus.Suspended, cancellationToken);
        }

        public ValueTask ResumeAsync(CancellationToken cancellationToken = default)
        {
            return _context.EnqueueStatusAsync(DeadManSwitchStatus.Resumed, cancellationToken);
        }
    }
}