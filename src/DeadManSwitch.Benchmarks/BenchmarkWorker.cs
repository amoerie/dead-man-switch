using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Benchmarks
{
    public class BenchmarkWorker : IDeadManSwitchWorker<double>
    {
        // for diagnostic purposes
        public string Name => "Benchmark worker";
        
        public async Task<double> WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
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

            return Math.PI;
        }
    }
}