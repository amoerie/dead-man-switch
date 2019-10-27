using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Tests
{
    public class ConfigurableDeadManSwitchWorker<TResult> : IDeadManSwitchWorker<TResult>
    {
        private readonly Func<IDeadManSwitch, CancellationToken, Task<TResult>> _workAsync;

        public ConfigurableDeadManSwitchWorker(Func<IDeadManSwitch, CancellationToken, Task<TResult>> workAsync)
        {
            _workAsync = workAsync ?? throw new ArgumentNullException(nameof(workAsync));
        }

        public string Name => "Configurable worker";
        
        public async Task<TResult> WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
        {
            return await _workAsync(deadManSwitch, cancellationToken).ConfigureAwait(false);
        }
    }
}