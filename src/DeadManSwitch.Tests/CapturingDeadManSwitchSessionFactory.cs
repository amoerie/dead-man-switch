using System;
using DeadManSwitch.Internal;

namespace DeadManSwitch.Tests
{
    internal class CapturingDeadManSwitchSessionFactory : IDeadManSwitchSessionFactory
    {
        private readonly IDeadManSwitchSessionFactory _inner;
        
        public IDeadManSwitchSession Session { get; private set; }

        public CapturingDeadManSwitchSessionFactory(IDeadManSwitchSessionFactory inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IDeadManSwitchSession Create(DeadManSwitchOptions deadManSwitchOptions)
        {
            return Session = _inner.Create(deadManSwitchOptions);
        }
    }
}