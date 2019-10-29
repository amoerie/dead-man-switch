using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Tests
{
    public class ConfigurableDeadManSwitchInfiniteWorker : IInfiniteDeadManSwitchWorker
    {
        private List<Func<IDeadManSwitch, CancellationToken, Task>> Iterations { get; }
        
        private int _iterationIndex = 0;

        public ConfigurableDeadManSwitchInfiniteWorker(IEnumerable<Func<IDeadManSwitch, CancellationToken, Task>> iterations)
        {
            Iterations = iterations?.ToList() ?? new List<Func<IDeadManSwitch, CancellationToken, Task>>();
        }

        public string Name => "Configurable dead man's switch";

        public async Task WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
        {
            if (deadManSwitch == null) throw new ArgumentNullException(nameof(deadManSwitch));
            
            if (cancellationToken.IsCancellationRequested)
                return;
            
            if (_iterationIndex < Iterations.Count)
            {
                await Iterations[_iterationIndex++](deadManSwitch, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new Exception("No more actions");
            }
        }
    }
}