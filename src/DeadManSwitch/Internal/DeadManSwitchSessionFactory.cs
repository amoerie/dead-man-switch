using System;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch.Internal
{
    public interface IDeadManSwitchSessionFactory
    {
        IDeadManSwitchSession Create(DeadManSwitchOptions deadManSwitchOptions);
    }

    public class DeadManSwitchSessionFactory : IDeadManSwitchSessionFactory
    {
        private readonly ILogger _logger;

        public DeadManSwitchSessionFactory(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IDeadManSwitchSession Create(DeadManSwitchOptions deadManSwitchOptions)
        {
            var deadManSwitchContext = new DeadManSwitchContext(deadManSwitchOptions);
            var deadManSwitch = new DeadManSwitch(deadManSwitchContext, _logger);
            var deadManSwitchTriggerer = new DeadManSwitchTriggerer(deadManSwitchContext, deadManSwitchOptions, _logger);
            var deadManSwitchWatcher = new DeadManSwitchWatcher(deadManSwitchContext, deadManSwitchOptions, deadManSwitchTriggerer, _logger);
            return new DeadManSwitchSession(deadManSwitchContext, deadManSwitch, deadManSwitchWatcher);
        }
    }
}