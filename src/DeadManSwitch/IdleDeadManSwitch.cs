using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch
{
    /// <summary>
    /// A rather casual switch who doesn't really do anything, it just stands idle.
    /// Useful in scenarios where you don't have a real switch but the existing infrastructure does expect some switch to be present.
    /// </summary>
    public class IdleDeadManSwitch : IDeadManSwitch
    {
        public IEnumerable<DeadManSwitchNotification> Notifications => Enumerable.Empty<DeadManSwitchNotification>();
        
        public CancellationToken CancellationToken { get; } = default(CancellationToken);

        public ValueTask<DeadManSwitchResult> RunAsync(CancellationToken deadManSwitchCancellationToken)
        {
            return new ValueTask<DeadManSwitchResult>(DeadManSwitchResult.DeadManSwitchWasNotTriggered);
        }

        public ValueTask NotifyAsync(string notification)
        {
            return default;
        }

        public ValueTask PauseAsync()
        {
            return default;
        }

        public ValueTask ResumeAsync()
        {
            return default;
        }
        
        public void Dispose()
        {
            
        }
    }
}