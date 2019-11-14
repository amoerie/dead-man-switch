using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Benchmarks
{
    public class InfiniteBenchmarkWorker : IInfiniteDeadManSwitchWorker
    {
        private readonly Action _afterLastIteration;
        private int _remainingIterations;

        public InfiniteBenchmarkWorker(int remainingIterations, Action afterLastIteration)
        {
            _remainingIterations = remainingIterations;
            _afterLastIteration = afterLastIteration;
        }

        // for diagnostic purposes
        public string Name => "Infinite benchmark worker";

        public Task WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
        {
            if (deadManSwitch == null)
                throw new ArgumentNullException(nameof(deadManSwitch));

            for (var i = 0; i < 100; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                deadManSwitch.Notify("Working " + i);
            }

            deadManSwitch.Suspend();
            deadManSwitch.Resume();

            _remainingIterations--;
            if (_remainingIterations == 0)
                _afterLastIteration();

            return Task.CompletedTask;
        }
    }
}