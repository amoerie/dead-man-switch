using System;

namespace DeadManSwitch.Internal
{
    internal interface IDeadManSwitchSession : IDisposable
    {
        IDeadManSwitchContext DeadManSwitchContext { get; }
        IDeadManSwitch DeadManSwitch { get; }
        IDeadManSwitchWatcher DeadManSwitchWatcher { get; }
    }
    
    internal sealed class DeadManSwitchSession : IDeadManSwitchSession 
    {
        public IDeadManSwitchContext DeadManSwitchContext { get; }
        public IDeadManSwitch DeadManSwitch { get; }
        public IDeadManSwitchWatcher DeadManSwitchWatcher { get; }

        public DeadManSwitchSession(
            IDeadManSwitchContext deadManSwitchContext,
            IDeadManSwitch deadManSwitch,
            IDeadManSwitchWatcher deadManSwitchWatcher)
        {
            DeadManSwitchContext = deadManSwitchContext ?? throw new ArgumentNullException(nameof(deadManSwitchContext));
            DeadManSwitch = deadManSwitch ?? throw new ArgumentNullException(nameof(deadManSwitch));
            DeadManSwitchWatcher = deadManSwitchWatcher ?? throw new ArgumentNullException(nameof(deadManSwitchWatcher));
        }

        public void Dispose()
        {
            DeadManSwitchContext.Dispose();
        }
    }
}