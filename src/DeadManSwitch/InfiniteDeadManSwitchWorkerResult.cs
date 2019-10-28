using System;

namespace DeadManSwitch
{
    public sealed class InfiniteDeadManSwitchWorkerResult
    {
        public TimeSpan NextDelay { get; set; } = TimeSpan.FromSeconds(0);
    }
}