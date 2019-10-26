using DeadManSwitch.Internal;

namespace DeadManSwitch
{
    public class DeadManSwitchTaskOneTimeRunnerResult
    {
        public DeadManSwitchTaskExecutionResult DeadManSwitchTaskExecutionResult { get; }
        public DeadManSwitchResult DeadManSwitchResult { get; }

        public DeadManSwitchTaskOneTimeRunnerResult(DeadManSwitchTaskExecutionResult deadManSwitchTaskExecutionResult, DeadManSwitchResult deadManSwitchResult)
        {
            DeadManSwitchTaskExecutionResult = deadManSwitchTaskExecutionResult;
            DeadManSwitchResult = deadManSwitchResult;
        }
    }
}