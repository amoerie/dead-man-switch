using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Examples
{
    public class ExampleOneTime : IDeadManSwitchWorker<>
    {
        // for diagnostic purposes
        public string Name => "Example one time task";

        // the dead man's switch should receive a notification at least every 75s
        public TimeSpan Timeout => TimeSpan.FromSeconds(75);
        
        public async Task ExecuteAsync(IDeadManSwitch deadManSwitch)
        {
            if (deadManSwitch == null)
                throw new ArgumentNullException(nameof(deadManSwitch));

            await deadManSwitch.NotifyAsync("Beginning work").ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromSeconds(1), deadManSwitch.CancellationToken).ConfigureAwait(false);

            await deadManSwitch.NotifyAsync("Still busy, please don't cancel").ConfigureAwait(false);

            await DoSomethingUseful(deadManSwitch.CancellationToken).ConfigureAwait(false);

            // tell the dead man's switch to stop the clock
            await deadManSwitch.SuspendAsync().ConfigureAwait(false);

            await DoSomethingThatCanTakeVeryLongButShouldNotBeCancelled().ConfigureAwait(false);

            // tell the dead man's switch to resume the clock
            await deadManSwitch.ResumeAsync().ConfigureAwait(false);
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