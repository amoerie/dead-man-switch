using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch
{
    public class DeadManSwitchOptions
    {
        /// <summary>
        /// The logger to which diagnostic information is written
        /// </summary>
        public ILogger Logger { get; set; }
        
        /// <summary>
        /// The amount of time the dead man's switch will wait for a signal before canceling the task
        /// </summary>
        public TimeSpan Timeout { get; set; }
        
        /// <summary>
        /// The cancellation token that cancels the dead man's switch. This cancels both the dead man's switch loop and the tasks it is connected to.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
        
        /// <summary>
        /// How many notifications to maintain in memory for diagnostic purposes
        /// </summary>
        public int NumberOfNotificationsToKeep { get; set; }
    }
}