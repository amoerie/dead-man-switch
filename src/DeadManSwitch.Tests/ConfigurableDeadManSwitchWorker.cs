using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeadManSwitch.Tests
{
    public class ConfigurableDeadManSwitchWorker : IDeadManSwitchWorker<>
    {
        private List<Func<IDeadManSwitch, Task>> Actions { get; }

        public ConfigurableDeadManSwitchWorker(TimeSpan timeout, IEnumerable<Func<IDeadManSwitch, Task>> actions)
        {
            Timeout = timeout;
            Actions = actions?.ToList() ?? new List<Func<IDeadManSwitch, Task>>();
        }

        public string Name => "Configurable dead man's switch";
        
        public TimeSpan Timeout { get; }

        public async Task ExecuteAsync(IDeadManSwitch deadManSwitch)
        {
            foreach (var action in Actions)
            {
                if (deadManSwitch.CancellationToken.IsCancellationRequested)
                    break;
                
                await action(deadManSwitch).ConfigureAwait(false);
            }
        }
    }
}