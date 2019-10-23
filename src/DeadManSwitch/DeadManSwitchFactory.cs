using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch
{
    public interface IDeadManSwitchFactory
    {
        IDeadManSwitch Create(TimeSpan timeout, CancellationToken cancellationToken);
    }

    public class DeadManSwitchFactory : IDeadManSwitchFactory
    {
        private readonly ILogger _logger;
        private readonly int _numberOfNotificationsToKeep;

        public DeadManSwitchFactory(ILogger logger, int numberOfNotificationsToKeep)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _numberOfNotificationsToKeep = numberOfNotificationsToKeep;
        }
        
        public IDeadManSwitch Create(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return new DeadManSwitch(new DeadManSwitchOptions
            {
                Logger = _logger,
                Timeout = timeout,
                NumberOfNotificationsToKeep = _numberOfNotificationsToKeep,
                CancellationToken = cancellationToken
            });
        }
    }
}