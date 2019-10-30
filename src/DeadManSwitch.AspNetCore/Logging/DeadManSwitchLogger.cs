using System;
using DeadManSwitch.Logging;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch.AspNetCore.Logging
{
    /// <summary>
    /// Thin wrapper around <see cref="ILogger{TCategoryName}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DeadManSwitchLogger<T> : IDeadManSwitchLogger<T>
    {
        private readonly ILogger<T> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="DeadManSwitchLogger{T}"/>
        /// </summary>
        public DeadManSwitchLogger(ILogger<T> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public void Trace(string message, params object[] args)
        {
            _logger.LogTrace(message, args);
        }

        /// <inheritdoc />
        public void Debug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
        }

        /// <inheritdoc />
        public void Information(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        /// <inheritdoc />
        public void Warning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        /// <inheritdoc />
        public void Error(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }

        /// <inheritdoc />
        public void Error(Exception exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }
    }
}