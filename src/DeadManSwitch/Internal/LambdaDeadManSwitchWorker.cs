using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Internal
{
    internal sealed class LambdaDeadManSwitchWorker<TResult> : IDeadManSwitchWorker<TResult>
    {
        private readonly Func<IDeadManSwitch, CancellationToken, Task<TResult>> _lambda;

        public LambdaDeadManSwitchWorker(Func<IDeadManSwitch, CancellationToken, Task<TResult>> lambda)
        {
            _lambda = lambda ?? throw new ArgumentNullException(nameof(lambda));
        }

        public string Name => "Lambda Worker";

        public Task<TResult> WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
        {
            return _lambda(deadManSwitch, cancellationToken);
        }
    }
}