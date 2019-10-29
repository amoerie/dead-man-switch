using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch.Examples
{
    public class ExampleProgram
    {
        /// <summary>
        /// Demonstrates how you can run a worker once, using a dead man's switch
        /// </summary>
        public async Task Main()
        {
            var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
            var runner = new DeadManSwitchRunner(
                loggerFactory.CreateLogger<DeadManSwitchRunner>(),
                new DeadManSwitchSessionFactory(loggerFactory)
            );
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