using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Benchmarks
{
    public class BenchmarkWorker : IDeadManSwitchWorker<double>
    {
        // for diagnostic purposes
        public string Name => "Benchmark worker";

        public Task<double> WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
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

            return Task.FromResult(Math.PI);
        }
    }
}