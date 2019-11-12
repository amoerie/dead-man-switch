using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using DeadManSwitch.Logging;

namespace DeadManSwitch
{
    /// <summary>
    /// According to Wikipedia, a dead man's switch is a switch that is designed to be activated or deactivated if the human operator becomes incapacitated,
    /// such as through death, loss of consciousness, or being bodily removed from control.
    /// Originally applied to switches on a vehicle or machine, it has since come to be used to describe other intangible uses like in computer software.
    /// </summary>
    public interface IDeadManSwitch 
    {
        /// <summary>
        /// Notifies the dead man's switch, postponing the cancellation of the worker
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

    /// <inheritdoc />
    [SuppressMessage("Naming", "CA1724", Justification = "This project is named after this class")]
    public sealed class DeadManSwitch : IDeadManSwitch
    {
        private readonly IDeadManSwitchContext _context;
        private readonly IDeadManSwitchLogger<DeadManSwitch> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="DeadManSwitch"/>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        internal DeadManSwitch(IDeadManSwitchContext context, IDeadManSwitchLogger<DeadManSwitch> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async ValueTask NotifyAsync(string notification, CancellationToken cancellationToken = default)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            _logger.Debug("The dead man's switch received a notification: {Notification}", notification);
            
            var enqueueStatus = _context.EnqueueStatusAsync(DeadManSwitchStatus.NotificationReceived, cancellationToken);
            var addNotification = _context.AddNotificationAsync(new DeadManSwitchNotification(notification), cancellationToken);

            await enqueueStatus.ConfigureAwait(false);
            await addNotification.ConfigureAwait(false);
        }

        /// <inheritdoc />
        public ValueTask SuspendAsync(CancellationToken cancellationToken = default)
        {
            _logger.Debug("The dead man's switch received a 'suspend' call");

            return _context.EnqueueStatusAsync(DeadManSwitchStatus.Suspended, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask ResumeAsync(CancellationToken cancellationToken = default)
        {
            _logger.Debug("The dead man's switch received a 'resume' call");

            return _context.EnqueueStatusAsync(DeadManSwitchStatus.Resumed, cancellationToken);
        }
    }
}