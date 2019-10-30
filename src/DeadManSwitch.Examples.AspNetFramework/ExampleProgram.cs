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
        /// Demonstrates how you can run a worker once, using a dead man's switch
        /// </summary>
        public static async Task Main()
        {
            var loggerFactory = new NLoggerFactory();
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
}