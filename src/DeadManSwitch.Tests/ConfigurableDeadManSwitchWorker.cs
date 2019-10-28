using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Tests
{
    public class ConfigurableDeadManSwitchWorker<TResult> : IDeadManSwitchWorker<TResult>
    {
        private readonly IEnumerable<Func<IDeadManSwitch, CancellationToken, Task>> _tasks;
        private readonly Task<TResult> _result;

        public string Name => "Configurable worker";

        public ConfigurableDeadManSwitchWorker(IEnumerable<Func<IDeadManSwitch, CancellationToken, Task>> tasks, Task<TResult> result)
        {
            _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
            _result = result ?? throw new ArgumentNullException(nameof(result));
        }

        public async Task<TResult> WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
        {
            if(cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);
            foreach (var task in _tasks)
            {
                if(cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);
                    
                await task(deadManSwitch, cancellationToken).ConfigureAwait(false);
            }

            return await _result.ConfigureAwait(false);
        }
    }
}