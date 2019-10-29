# 💀 The Dead Man's Switch

[![Actions Status](https://github.com/amoerie/dead-man-switch/workflows/.NET+Core/badge.svg)](https://github.com/amoerie/dead-man-switch/actions)


According to a Wikipedia, a **dead man's switch** is

> ... a switch that is designed to be activated or deactivated if the human operator becomes incapacitated, such as through death, loss of consciousness, or being bodily removed from control. Originally applied to switches on a vehicle or machine, it has since come to be used to describe other intangible uses like in computer software.



# DeadManSwitch.cs

In .NET, a dead man's switch is designed to detect a process that is no longer making progress and cancel it. It does this by cancelling a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken) that is provided to the process from the start. In turn, the process is responsible for notifying the dead man's switch in a periodic fashion to prevent its own cancellation.

# Getting started

See [the examples](https://github.com/amoerie/dead-man-switch/tree/master/src/DeadManSwitch.Examples) for a sample program that notifies the dead man's switch.

# NuGet

Coming soon