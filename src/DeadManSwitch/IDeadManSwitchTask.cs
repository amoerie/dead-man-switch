using System;
using System.Threading.Tasks;

namespace DeadManSwitch
{
    /// <summary>
    /// This is the one you need to implement yourself
    /// </summary>
    public interface IDeadManSwitchTask
    {
        /// <summary>
        /// The name of this task
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Determines how long this task can remain silent (i.e. not notify the dead man's switch) before it should be canceled
        /// </summary>
        TimeSpan Timeout { get; }
        
        /// <summary>
        /// Executes a task using a dead man's switch. If the switch is not notified in a periodical, timely manner, the
        /// work will be cancelled using the cancellation token.
        /// </summary>
        /// <param name="deadManSwitch">The dead man's switch that should be notified every x seconds</param>
        Task ExecuteAsync(IDeadManSwitch deadManSwitch);
    }
}