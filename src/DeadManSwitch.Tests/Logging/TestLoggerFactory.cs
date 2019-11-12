using System;
using DeadManSwitch.Logging;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch.Tests.Logging
{
    /// <summary>
    /// Thin wrapper around <see cref="ILoggerFactory"/>
    /// </summary>
    public class TestLoggerFactory : IDeadManSwitchLoggerFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Creates a new <see cref="DeadManSwitchLoggerFactory"/> 
        /// </summary>
        public TestLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <inheritdoc />
        public IDeadManSwitchLogger<T> CreateLogger<T>()
        {
            return new TestLogger<T>(_loggerFactory.CreateLogger<T>());
        }
    }
}