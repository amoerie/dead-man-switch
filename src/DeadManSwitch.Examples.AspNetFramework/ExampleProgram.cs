using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Examples.AspNetFramework.Logging;

namespace DeadManSwitch.Examples.AspNetFramework
{
    public static class ExampleProgram
    {
        /// <summary>
        ///     Demonstrates how you can run a worker once, using a dead man's switch
        /// </summary>
        public static async Task Main()
        {
            // This example uses NLog, but it only requires a trivial amount of code to use any other logging library. 
            var loggerFactory = new NLoggerFactory();
            
            // You can also use Create() which disables logging
            var runner = DeadManSwitchRunner.Create(loggerFactory);

            var worker = new ExampleWorker();

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var options = new DeadManSwitchOptions
                {
                    Timeout = TimeSpan.FromSeconds(60)
                };
                var run = runner.RunAsync(worker, options, cancellationTokenSource.Token);

                // if you want to cancel at some point: cancellationTokenSource.Cancel();

                var result = await run.ConfigureAwait(false);

                Debug.Assert(result == Math.PI);
            }
        }
    }
    
    public class ExampleWorker : IDeadManSwitchWorker<double>
    {
        // for diagnostic purposes
        public string Name => "Example one time worker";

        public async Task<double> WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
        {
            if (deadManSwitch == null)
                throw new ArgumentNullException(nameof(deadManSwitch));

            deadManSwitch.Notify("Beginning work");

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

            deadManSwitch.Notify("Still busy, please don't cancel");

            await DoSomethingUseful(cancellationToken).ConfigureAwait(false);

            // tell the dead man's switch to stop the clock
            deadManSwitch.Suspend();

            await DoSomethingThatCanTakeVeryLongButShouldNotBeCancelledByTheDeadManSwitch(cancellationToken).ConfigureAwait(false);

            // tell the dead man's switch to resume the clock
            deadManSwitch.Resume();

            return Math.PI;
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