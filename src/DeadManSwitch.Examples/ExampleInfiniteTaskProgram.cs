using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch.Examples
{
    public class ExampleInfiniteTaskProgram
    {
        /// <summary>
        /// Demonstrates how to run (and stop) an infinite worker, using a dead man's switch
        /// </summary>
        public static async Task Main()
        {
            var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
            var logger = loggerFactory.CreateLogger<ExampleOneTimeTaskProgram>();
            var runner = new DeadManSwitchTaskInfiniteRunner(
                logger,
                new DeadManSwitchManager(
                    logger,
                    new DeadManSwitchSessionFactory(logger, 10),
                    new DeadManSwitchWorkerScheduler(logger)
                )
            );
            var worker = new Example();

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // do not await this, it will never complete until you cancel the token
                var run = runner.RunInfinitelyAsync(worker, cancellationTokenSource.Token);
                
                // let it run for 10s.
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationTokenSource.Token).ConfigureAwait(false);

                // now stop the infinite worker
                cancellationTokenSource.Cancel();

                // collect the results
                var result = await run.ConfigureAwait(false);
                
                Debug.Assert(result.DeadManSwitchesTriggered == 0);    
            }
        }
    }
}