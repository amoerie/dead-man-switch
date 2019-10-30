using System;
using DeadManSwitch.Logging;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch.AspNetCore.Logging
{
    public class DeadManSwitchLogger<T> : IDeadManSwitchLogger<T>
    {
        private readonly ILogger<T> _logger;

        public DeadManSwitchLogger(ILogger<T> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Trace(string message, params object[] args)
        {
            _logger.LogTrace(message, args);
        }

        public void Debug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
        }

        public void Information(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void Warning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void Error(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }

        public void Error(Exception exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }
    }
}