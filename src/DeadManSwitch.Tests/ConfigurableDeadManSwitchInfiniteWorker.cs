/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeadManSwitch.Tests
{
    public class ConfigurableDeadManSwitchInfiniteWorker : IDeadManSwitchInfiniteWorker
    {
        private List<Func<IDeadManSwitch, Task>> Actions { get; }
        private int _actionIndex = 0;

        public ConfigurableDeadManSwitchInfiniteWorker(TimeSpan timeout, TimeSpan delay, IEnumerable<Func<IDeadManSwitch, Task>> actions)
        {
            Timeout = timeout;
            Delay = delay;
            Actions = actions?.ToList() ?? new List<Func<IDeadManSwitch, Task>>();
        }

        public string Name => "Configurable dead man's switch";
        public TimeSpan Timeout { get; }
        public TimeSpan Delay { get; }

        public async Task ExecuteAsync(IDeadManSwitch deadManSwitch)
        {
            if (deadManSwitch == null) throw new ArgumentNullException(nameof(deadManSwitch));
            
            if (deadManSwitch.CancellationToken.IsCancellationRequested)
                return;
            
            if (_actionIndex < Actions.Count)
            {
                await Actions[_actionIndex++](deadManSwitch).ConfigureAwait(false);
            }
            else
            {
                throw new Exception("No more actions");
            }
        }
    }
}*/