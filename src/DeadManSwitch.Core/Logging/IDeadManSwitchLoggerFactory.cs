namespace DeadManSwitch.Logging
{
    /// <summary>
    /// Interface that is capable of creating loggers
    /// </summary>
    public interface IDeadManSwitchLoggerFactory
    {
        /// <summary>
        /// Creates a logger with the provided type as the context
        /// </summary>
        /// <typeparam name="T">The log context</typeparam>
        /// <returns>A new logger</returns>
        IDeadManSwitchLogger<T> CreateLogger<T>();
    }
}