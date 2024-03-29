﻿using DeadManSwitch.Logging;

namespace DeadManSwitch.Internal
{
    internal interface IDeadManSwitchSessionFactory
    {
        IDeadManSwitchSession Create(DeadManSwitchOptions deadManSwitchOptions);
    }

    internal class DeadManSwitchSessionFactory : IDeadManSwitchSessionFactory
    {
        private readonly IDeadManSwitchLoggerFactory _loggerFactory;

        public DeadManSwitchSessionFactory(IDeadManSwitchLoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public IDeadManSwitchSession Create(DeadManSwitchOptions deadManSwitchOptions)
        {
            var deadManSwitchContext = new DeadManSwitchContext(deadManSwitchOptions);
            var deadManSwitch = new DeadManSwitch(deadManSwitchContext, _loggerFactory.CreateLogger<DeadManSwitch>());
            var deadManSwitchTriggerer = new DeadManSwitchTriggerer(deadManSwitchContext, deadManSwitchOptions, _loggerFactory.CreateLogger<DeadManSwitchTriggerer>());
            var deadManSwitchWatcher =
                new DeadManSwitchWatcher(deadManSwitchContext, deadManSwitchOptions, deadManSwitchTriggerer, _loggerFactory.CreateLogger<DeadManSwitchWatcher>());
            return new DeadManSwitchSession(deadManSwitchContext, deadManSwitch, deadManSwitchWatcher);
        }
    }
}