using System;

namespace DeadManSwitch
{
    /// <summary>
    /// This is the one you need to implement yourself
    /// </summary>
    public interface IDeadManSwitchInfiniteTask : IDeadManSwitchTask
    {
        /// <summary>
        /// Determines how long the infinite task runner will wait before executing the task again.
        /// This property is evaluated each time the loop will restart, so this value is allowed to change.
        /// </summary>
        TimeSpan Delay { get; }
    }
}