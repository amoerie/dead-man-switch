using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch
{
    public interface IDeadManSwitch : IDisposable
    {
        /// <summary>
        /// The cancellation token that will be marked as canceled when the dead man's switch is triggered
        /// </summary>
        CancellationToken CancellationToken { get; }
        
        /// <summary>
        /// Runs the dead man's switch. When no notifications are received within the proper timeout period, the <see cref="CancellationToken"/>
        /// will be cancelled automatically. You should pass this cancellation token to any task that must be cancelled.
        /// </summary>
        /// <returns>A value indicating whether the dead man switch triggered or not</returns>
        ValueTask<DeadManSwitchResult> RunAsync(CancellationToken deadManSwitchCancellationToken);

        ValueTask NotifyAsync(string notification);
        ValueTask PauseAsync();
        ValueTask ResumeAsync();
        
        IEnumerable<DeadManSwitchNotification> Notifications { get; }
    }
}