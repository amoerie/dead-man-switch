# 💀 The Dead Man's Switch

[![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2Famoerie%2Fdead-man-switch%2Fbadge%3Fref%3Dmaster&style=for-the-badge&label=Build)](https://actions-badge.atrox.dev/amoerie/dead-man-switch/goto?ref=master) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/DeadManSwitch?label=DeadManSwitch&style=for-the-badge)](https://www.nuget.org/packages/DeadManSwitch) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/DeadManSwitch.AspNetCore?label=DeadManSwitch.AspNetCore&style=for-the-badge)](https://www.nuget.org/packages/DeadManSwitch.AspNetCore)


According to Wikipedia, a [**dead man's switch**](https://en.wikipedia.org/wiki/Dead_man%27s_switch) is

> ... a switch that is designed to be activated or deactivated if the human operator becomes incapacitated, such as through death, loss of consciousness, or being bodily removed from control. Originally applied to switches on a vehicle or machine, it has since come to be used to describe other intangible uses like in computer software.


# DeadManSwitch.cs

In .NET, a dead man's switch is designed to detect a worker task that is no longer making progress and cancel it. It does this by cancelling a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken) that is provided to the worker from the start. In turn, the process is responsible for notifying the dead man's switch in a periodic fashion to prevent its own cancellation.

This library is intended to easily implement long running worker tasks that may or may not freeze/stop making progress somewhere along the way. When that happens, the dead man's switch will trigger after a preconfigured timeout and automatically try to cancel the worker using the provided cancellation token. 

There are two types of workers:

- A "worker" that runs once = run an async Task exactly once, and it produces a result
- An "infinite worker" that is run over and over again infinitely = you want to infinitely repeat an async Task

# Examples

## A worker that returns a result

```csharp
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

                    await DoSomethingThatCanTakeVeryLongButShouldNotBeCancelledByTheDeadManSwitch((cancellationToken).ConfigureAwait(false);

                    // tell the dead man's switch to resume the clock
                    deadManSwitch.Resume();

                    return Math.PI;
                }, options, cancellationTokenSource.Token);

                // if you want to cancel at some point: 
                // cancellationTokenSource.Cancel();

                var result = await run.ConfigureAwait(false);

                Debug.Assert(result.Equals(Math.PI));
            }
        }

        private static async Task DoSomethingThatCanTakeVeryLongButShouldNotBeCancelledByTheDeadManSwitch(CancellationToken cancellationToken)
        {
            await Task.Delay(100000, cancellationToken).ConfigureAwait(false);
        }
    }
```

## A worker that runs infinitely

```csharp
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
```

## Other examples

For more examples, including ones that use dependency injection and allow you to hook into the logging infrastructure, see the following links:

- [Examples for .NET Core](https://github.com/amoerie/dead-man-switch/tree/master/src/DeadManSwitch.Examples.AspNetCore)
- [Examples for .NET Framework](https://github.com/amoerie/dead-man-switch/tree/master/src/DeadManSwitch.Examples.AspNetFramework)

# NuGet

- [DeadManSwitch](https://www.nuget.org/packages/DeadManSwitch/) is the main project that contains the necessary classes to use the Dead Man's Switch
- [DeadManSwitch.AspNetCore](https://www.nuget.org/packages/DeadManSwitch.AspNetCore/) contains a utility function to hook into the dependency injection and logging infrastructure of ASP.NET Core


# Changelog 

See the [CHANGELOG.MD](https://github.com/amoerie/dead-man-switch/tree/master/CHANGELOG.MD) file

# Contributors 

See the [CONTRIBUTORS.MD](https://github.com/amoerie/dead-man-switch/tree/master/CONTRIBUTORS.MD) file
