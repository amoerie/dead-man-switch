using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Examples.AspNetFramework.Logging;

namespace DeadManSwitch.Examples.AspNetFramework
{
    public static class ExampleInfiniteProgramUsingDependencyInjection
    {
        /// <summary>
        ///     Demonstrates how to run (and stop) an infinite worker, using a dead man's switch
        /// </summary>
        public static async Task Main()
        {
            // This example uses NLog, but it only requires a trivial amount of code to use any other logging library. 
            var loggerFactory = new NLoggerFactory();

            // You can also use Create() which disables logging
            var infiniteRunner = InfiniteDeadManSwitchRunner.Create(loggerFactory);
            var worker = new ExampleInfiniteWorker();

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var options = new DeadManSwitchOptions
                {
                    Timeout = TimeSpan.FromSeconds(60)
                };
                // do not await this, it will never complete until you cancel the token
                var run = infiniteRunner.RunAsync(worker, options, cancellationTokenSource.Token);

                // let it run for 10s.
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationTokenSource.Token).ConfigureAwait(false);

                // now stop the infinite worker
                cancellationTokenSource.Cancel();

                // let it finish gracefully
                await run.ConfigureAwait(false);
            }
        }
    }
    
    public class ExampleInfiniteWorker : IInfiniteDeadManSwitchWorker
    {
        // for diagnostic purposes
        public string Name => "Example one time worker";

        public async Task WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
        {
            if (deadManSwitch is null) throw new ArgumentNullException(nameof(deadManSwitch));

            deadManSwitch.Notify("Beginning work again");

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

            deadManSwitch.Notify("Still busy, please don't cancel");

            await DoSomethingUseful(cancellationToken).ConfigureAwait(false);

            // tell the dead man's switch to stop the clock
            deadManSwitch.Suspend();
            await DoSomethingThatCanTakeVeryLongButShouldNotBeCancelledByTheDeadManSwitch(cancellationToken).ConfigureAwait(false);

            // tell the dead man's switch to resume the clock
            deadManSwitch.Resume();
        }

        private async Task DoSomethingUseful(CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        private async Task DoSomethingThatCanTakeVeryLongButShouldNotBeCancelledByTheDeadManSwitch(CancellationToken cancellationToken)
        {
            await Task.Delay(100000, cancellationToken).ConfigureAwait(false);
        }
    }
}