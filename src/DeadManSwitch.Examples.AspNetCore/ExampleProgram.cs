using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Examples.AspNetCore
{
    public static class ExampleProgram
    {
        /// <summary>
        ///     Demonstrates how you can run a worker once, using a dead man's switch
        /// </summary>
        public static async Task Main()
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var options = new DeadManSwitchOptions
                {
                    Timeout = TimeSpan.FromSeconds(60)
                };
                var run = DeadManSwitchTask.RunAsync(async (deadManSwitch, cancellationToken) =>
                {
                    deadManSwitch.Notify("Beginning work");

                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

                    deadManSwitch.Notify("Still busy, please don't cancel");

                    // tell the dead man's switch to stop the clock
                    deadManSwitch.Suspend();

                    // tell the dead man's switch to resume the clock
                    deadManSwitch.Resume();

                    return Math.PI;
                }, options, cancellationTokenSource.Token);

                // if you want to cancel at some point: cancellationTokenSource.Cancel();

                var result = await run.ConfigureAwait(false);

                Debug.Assert(result.Equals(Math.PI));
            }
        }
    }
}