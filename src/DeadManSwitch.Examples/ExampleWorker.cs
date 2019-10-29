using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Examples
{
    public class ExampleWorker : IDeadManSwitchWorker<double>
    {
        // for diagnostic purposes
        public string Name => "Example one time worker";
        
        public async Task<double> WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
        {
            if (deadManSwitch == null)
                throw new ArgumentNullException(nameof(deadManSwitch));

            await deadManSwitch.NotifyAsync("Beginning work", cancellationToken).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

            await deadManSwitch.NotifyAsync("Still busy, please don't cancel", cancellationToken).ConfigureAwait(false);

            await DoSomethingUseful(cancellationToken).ConfigureAwait(false);

            // tell the dead man's switch to stop the clock
            await deadManSwitch.SuspendAsync(cancellationToken).ConfigureAwait(false);

            await DoSomethingThatCanTakeVeryLongButShouldNotBeCancelled().ConfigureAwait(false);

            // tell the dead man's switch to resume the clock
            await deadManSwitch.ResumeAsync(cancellationToken).ConfigureAwait(false);

            return Math.PI;
        }

        private Task DoSomethingUseful(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private Task DoSomethingThatCanTakeVeryLongButShouldNotBeCancelled()
        {
            return Task.CompletedTask;
        }
    }
}