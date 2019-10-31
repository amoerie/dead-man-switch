# 💀 The Dead Man's Switch

[![Actions Status](https://github.com/amoerie/dead-man-switch/workflows/build/badge.svg)](https://github.com/amoerie/dead-man-switch/actions) ![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/DeadManSwitch?label=DeadManSwitch) ![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/DeadManSwitch.AspNetCore?label=DeadManSwitch.AspNetCore)


According to Wikipedia, a [**dead man's switch**](https://en.wikipedia.org/wiki/Dead_man%27s_switch) is

> ... a switch that is designed to be activated or deactivated if the human operator becomes incapacitated, such as through death, loss of consciousness, or being bodily removed from control. Originally applied to switches on a vehicle or machine, it has since come to be used to describe other intangible uses like in computer software.


# IDeadManSwitch.cs

In .NET, a dead man's switch is designed to detect a worker task that is no longer making progress and cancel it. It does this by cancelling a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken) that is provided to the worker from the start. In turn, the process is responsible for notifying the dead man's switch in a periodic fashion to prevent its own cancellation.

This library is intended to easily implement long running worker tasks that may or may not freeze/stop making progress somewhere along the way. When that happens, the dead man's switch will trigger after a preconfigured timeout and automatically try to cancel the worker using the provided cancellation token. 

There are two types of workers you can implement:

- IDeadManSwitchWorker<TResult> = this interface indicates you want to run an async Task exactly once, and it produces a TResult
- IInfiniteDeadManSwitchWorker  = this interface indicates you want to infinitely repeat an async Task

# Example

```csharp
    public class ExampleWorker : IDeadManSwitchWorker<double>
    {
        // for diagnostic purposes
        public string Name => "Example one time worker";
        
        public async Task<double> WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken)
        {
            if (deadManSwitch == null)
                throw new ArgumentNullException(nameof(deadManSwitch));

            await deadManSwitch.NotifyAsync("Beginning work", cancellationToken).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

            await deadManSwitch.NotifyAsync("Still busy, please don't cancel", cancellationToken).ConfigureAwait(false);

            await DoSomethingUseful(cancellationToken).ConfigureAwait(false);

            // tell the dead man's switch to stop the clock
            await deadManSwitch.SuspendAsync(cancellationToken).ConfigureAwait(false);

            await DoSomethingThatCanTakeVeryLong(cancellationToken).ConfigureAwait(false);

            // tell the dead man's switch to resume the clock
            await deadManSwitch.ResumeAsync(cancellationToken).ConfigureAwait(false);

            return Math.PI;
        }

        private async Task DoSomethingUseful(CancellationToken cancellationToken)
        {
            // ...
        }

        private async Task DoSomethingThatCanTakeVeryLong(CancellationToken cancellationToken)
        {
            // ...
        }
    }
```

# Getting started

See the examples projects for a sample program that notifies the dead man's switch:

- [Example for .NET Core](https://github.com/amoerie/dead-man-switch/tree/master/src/DeadManSwitch.Examples.AspNetCore)
- [Example for .NET Framework](https://github.com/amoerie/dead-man-switch/tree/master/src/DeadManSwitch.Examples.AspNetFramework)

# NuGet

- [DeadManSwitch](https://www.nuget.org/packages/DeadManSwitch/)
- [DeadManSwitch.AspNetCore](https://www.nuget.org/packages/DeadManSwitch.AspNetCore/)
