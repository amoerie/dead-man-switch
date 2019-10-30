using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.AspNetCore.DependencyInjection;
using DeadManSwitch.AspNetCore.Logging;
using DeadManSwitch.Internal;
using Microsoft.Extensions.DependencyInjection;
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
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole())
                .AddDeadManSwitch()
                .BuildServiceProvider();
            var runner = serviceProvider.GetRequiredService<IDeadManSwitchRunner>();
            
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