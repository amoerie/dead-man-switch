using System;
using System.Diagnostics.CodeAnalysis;
using DeadManSwitch.Internal;
using DeadManSwitch.Logging;

namespace DeadManSwitch
{
    /// <summary>
    ///     According to Wikipedia, a dead man's switch is a switch that is designed to be activated or deactivated if the human operator becomes incapacitated,
    ///     such as through death, loss of consciousness, or being bodily removed from control.
    ///     Originally applied to switches on a vehicle or machine, it has since come to be used to describe other intangible uses like in computer software.
    /// </summary>
    public interface IDeadManSwitch
    {
        /// <summary>
        ///     Notifies the dead man's switch, postponing the cancellation of the worker
        /// </summary>
        /// <param name="notification">
        ///     A notification message that will be shown when the worker worker is cancelled.
        ///     This can be useful to retrace the last steps of the worker worker.
        /// </param>
        void Notify(string notification);

        /// <summary>
        ///     Pauses the dead man's switch. The worker worker cannot be cancelled until the dead man's switch is resumed.
        /// </summary>
        void Suspend();

        /// <summary>
        ///     Resumes the dead man's switch after pausing it.
        /// </summary>
        [SuppressMessage("Naming", "CA1716", Justification = "Resume is the logical counterpart to Suspend")]
        void Resume();
    }

    /// <inheritdoc />
    [SuppressMessage("Naming", "CA1724", Justification = "This project is named after this class")]
    public sealed class DeadManSwitch : IDeadManSwitch
    {
        private readonly IDeadManSwitchContext _context;
        private readonly IDeadManSwitchLogger<DeadManSwitch> _logger;

        /// <summary>
        ///     Initializes a new instance of <see cref="DeadManSwitch" />
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        internal DeadManSwitch(IDeadManSwitchContext context, IDeadManSwitchLogger<DeadManSwitch> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public void Notify(string notification)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            _logger.Debug("The dead man's switch received a notification: {Notification}", notification);

            _context.AddNotification(new DeadManSwitchNotification(notification));
        }

        /// <inheritdoc />
        public void Suspend()
        {
            _logger.Debug("The dead man's switch received a 'suspend' call");

            _context.Suspend();
        }

        /// <inheritdoc />
        public void Resume()
        {
            _logger.Debug("The dead man's switch received a 'resume' call");

            _context.Resume();
        }
    }
}