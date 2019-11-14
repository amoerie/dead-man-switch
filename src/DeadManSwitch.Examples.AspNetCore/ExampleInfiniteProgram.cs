using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch.Examples.AspNetCore
{
    public static class ExampleInfiniteProgram
    {
        /// <summary>
        ///     Demonstrates how to run (and stop) an infinite worker, using a dead man's switch
        /// </summary>
        public static async Task Main()
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var options = new DeadManSwitchOptions
                {
                    Timeout = TimeSpan.FromSeconds(60)
                };
                // do not await this, it will never complete until you cancel the token
                var run = InfiniteDeadManSwitchTask.RunAsync(async (deadManSwitch, cancellationToken) =>
                {
                    deadManSwitch.Notify("Beginning work again");

                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

                    deadManSwitch.Notify("Still busy, please don't cancel");

                    await DoSomethingUseful(cancellationToken).ConfigureAwait(false);

                }, options, cancellationTokenSource.Token);

                // let it run for 10s.
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationTokenSource.Token).ConfigureAwait(false);

                // now stop the infinite worker
                cancellationTokenSource.Cancel();

                // let it finish gracefully
                await run.ConfigureAwait(false);
            }
        }

        private static async Task DoSomethingUseful(CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }
    }
}