using System;
using DeadManSwitch.Logging;
using NLog;

namespace DeadManSwitch.Examples.AspNetFramework.Logging
{
    /// <summary>
    ///     Thin wrapper around NLog.Logger
    /// </summary>
    public class NLogger<T> : IDeadManSwitchLogger<T>
    {
        private readonly Logger _logger;

        public NLogger(Logger inner)
        {
            _logger = inner;
        }

        public void Trace(string message, params object[] args)
        {
            _logger.Trace(message, args);
        }

        public void Debug(string message, params object[] args)
        {
            _logger.Debug(message, args);
        }

        public void Information(string message, params object[] args)
        {
            _logger.Info(message, args);
        }

        public void Warning(string message, params object[] args)
        {
            _logger.Warn(message, args);
        }

        public void Error(string message, params object[] args)
        {
            _logger.Error(message, args);
        }

        public void Error(Exception exception, string message, params object[] args)
        {
            _logger.Error(exception, message, args);
        }
    }
}