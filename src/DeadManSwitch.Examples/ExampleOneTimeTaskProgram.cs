using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch.Examples
{
    public class ExampleOneTimeTaskProgram
    {
        /// <summary>
        /// Demonstrates how you can run a worker once, using a dead man's switch
        /// </summary>
        public async Task Main()
        {
            var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
            var logger = loggerFactory.CreateLogger<ExampleOneTimeTaskProgram>();
            var runner = new DeadManSwitchManager(
                logger,
                new DeadManSwitchSessionFactory(logger, 10),
                new DeadManSwitchWorkerScheduler(logger)
            );
            var worker = new ExampleOneTime();

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var run = runner.RunOneTimeAsync(worker, cancellationTokenSource.Token);

                // if you want to cancel at some point: cancellationTokenSource.Cancel();

                var result = await run.ConfigureAwait(false);

                Debug.Assert(result.DeadManSwitchResult == DeadManSwitchResult.DeadManSwitchWasTriggered);
            }
        }
    }
}