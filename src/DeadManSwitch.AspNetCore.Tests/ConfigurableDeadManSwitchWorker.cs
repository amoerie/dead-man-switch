using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.AspNetCore.Tests
{
    public class ConfigurableDeadManSwitchWorker<TResult> : IDeadManSwitchWorker<TResult>
    {
        private readonly Func<IDeadManSwitch, CancellationToken, Task> _work;
        private readonly Task<TResult> _result;

        public string Name => "Configurable worker";

        public ConfigurableDeadManSwitchWorker(Func<IDeadManSwitch, CancellationToken, Task> work, Task<TResult> result)
        {
            _work = work ?? throw new ArgumentNullException(nameof(work));
            _result = result ?? throw new ArgumentNullException(nameof(result));
        }

        public async Task<TResult> WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
        {
            await _work(deadManSwitch, cancellationToken).ConfigureAwait(false);

            return await _result.ConfigureAwait(false);
        }
    }
}