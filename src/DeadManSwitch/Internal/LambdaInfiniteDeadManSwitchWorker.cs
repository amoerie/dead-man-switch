using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Internal
{
    internal sealed class LambdaInfiniteDeadManSwitchWorker : IInfiniteDeadManSwitchWorker
    {
        private readonly Func<IDeadManSwitch, CancellationToken, Task> _lambda;

        public LambdaInfiniteDeadManSwitchWorker(Func<IDeadManSwitch, CancellationToken, Task> lambda)
        {
            _lambda = lambda ?? throw new ArgumentNullException(nameof(lambda));
        }

        public string Name => "Lambda Infinite Worker";

        public Task WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
        {
            return _lambda(deadManSwitch, cancellationToken);
        }
    }
}