using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Benchmarks
{
    public class InfiniteBenchmarkWorker : IInfiniteDeadManSwitchWorker
    {
        private int _remainingIterations;
        private readonly Action _afterLastIteration;

        // for diagnostic purposes
        public string Name => "Infinite benchmark worker";

        public InfiniteBenchmarkWorker(int remainingIterations, Action afterLastIteration)
        {
            _remainingIterations = remainingIterations;
            _afterLastIteration = afterLastIteration;
        }
        
        public async Task WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
        {
            if (deadManSwitch == null)
                throw new ArgumentNullException(nameof(deadManSwitch));

                        
            for (int i = 0; i < 100; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await deadManSwitch.NotifyAsync("Working " + i, cancellationToken).ConfigureAwait(false);
            }
            await deadManSwitch.SuspendAsync(cancellationToken).ConfigureAwait(false);
            await deadManSwitch.ResumeAsync(cancellationToken).ConfigureAwait(false);

            _remainingIterations--;
            if (_remainingIterations == 0)
                _afterLastIteration();
        }
    }
}