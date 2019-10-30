using System;
using DeadManSwitch.Logging;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch.AspNetCore.Logging
{
    /// <summary>
    /// Thin wrapper around <see cref="ILoggerFactory"/>
    /// </summary>
    public class DeadManSwitchLoggerFactory : IDeadManSwitchLoggerFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Creates a new <see cref="DeadManSwitchLoggerFactory"/> 
        /// </summary>
        public DeadManSwitchLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <inheritdoc />
        public IDeadManSwitchLogger<T> CreateLogger<T>()
        {
            return new DeadManSwitchLogger<T>(_loggerFactory.CreateLogger<T>());
        }
    }
}