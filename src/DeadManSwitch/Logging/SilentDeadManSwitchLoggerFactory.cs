namespace DeadManSwitch.Logging
{
    internal sealed class SilentDeadManSwitchLoggerFactory : IDeadManSwitchLoggerFactory
    {
        public IDeadManSwitchLogger<T> CreateLogger<T>()
        {
            return new SilentDeadManSwitchLogger<T>();
        }
    }
}