using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch.Examples
{
    public class ExampleInfiniteProgram
    {
        /// <summary>
        /// Demonstrates how to run (and stop) an infinite worker, using a dead man's switch
        /// </summary>
        public static async Task Main()
        {
            var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
            var runner = new InfiniteDeadManSwitchRunner(
                loggerFactory.CreateLogger<InfiniteDeadManSwitchRunner>(),
                new DeadManSwitchSessionFactory(loggerFactory)
            );
            var worker = new ExampleInfiniteWorker();

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var options = new DeadManSwitchOptions
                {
                    Timeout = TimeSpan.FromSeconds(60)
                };
                // do not await this, it will never complete until you cancel the token
                var run = runner.RunAsync(worker, options,  cancellationTokenSource.Token);
                
                // let it run for 10s.
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationTokenSource.Token).ConfigureAwait(false);

                // now stop the infinite worker
                cancellationTokenSource.Cancel();

                // let it finish gracefully
                await run.ConfigureAwait(false);
            }
        }
    }
}