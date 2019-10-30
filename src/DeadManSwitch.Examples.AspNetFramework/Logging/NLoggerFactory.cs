using DeadManSwitch.Logging;
using NLog;

namespace DeadManSwitch.Examples.AspNetFramework.Logging
{
    /// <summary>
    /// Thin wrapper around NLog.LogManager
    /// </summary>
    public class NLoggerFactory : IDeadManSwitchLoggerFactory
    {
        public IDeadManSwitchLogger<T> CreateLogger<T>()
        {
            return new NLogger<T>(LogManager.GetLogger(typeof(T).FullName));
        }
    }
}