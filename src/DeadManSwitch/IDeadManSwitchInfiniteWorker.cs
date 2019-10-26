using System;

namespace DeadManSwitch
{
    /// <summary>
    /// Represents a task that should be cancelled automatically if it does not pull the dead man's switch in a timely fashion.
    /// In addition to being a <see cref="IDeadManSwitchWorker{TResult}"/>, an <see cref="IDeadManSwitchInfiniteWorker"/> will be called repeatedly after each execution,
    /// ith the specified <see cref="Delay"/> between each execution. 
    /// </summary>
    public interface IDeadManSwitchInfiniteWorker<TResult> : IDeadManSwitchWorker<TResult>
    {
        /// <summary>
        /// Determines how long the infinite task runner will wait before executing the task again.
        /// This property is evaluated each time the loop will restart, so this value is allowed to change.
        /// </summary>
        TimeSpan Delay { get; }
    }
}