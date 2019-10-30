using System;
using DeadManSwitch.Logging;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch.AspNetCore.Logging
{
    public class DeadManSwitchLoggerFactory : IDeadManSwitchLoggerFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public DeadManSwitchLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }
        
        public IDeadManSwitchLogger<T> CreateLogger<T>()
        {
            return new DeadManSwitchLogger<T>(_loggerFactory.CreateLogger<T>());
        }
    }
}