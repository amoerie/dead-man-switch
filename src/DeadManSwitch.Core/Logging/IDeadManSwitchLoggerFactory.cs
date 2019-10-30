namespace DeadManSwitch.Logging
{
    public interface IDeadManSwitchLoggerFactory
    {
        IDeadManSwitchLogger<T> CreateLogger<T>();
    }
}