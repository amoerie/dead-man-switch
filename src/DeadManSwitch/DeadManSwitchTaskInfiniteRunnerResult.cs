namespace DeadManSwitch
{
    public class DeadManSwitchTaskInfiniteRunnerResult
    {
        public int TasksThatFinishedGracefully { get; set; }
        public int TasksThatThrewAnException { get; set; }
        public int TasksThatWereCanceled { get; set; }
        public int DeadManSwitchesTriggered { get; set; }

        public void Report(DeadManSwitchTaskExecutionResult executionResult, DeadManSwitchResult switchResult)
        {
            switch (executionResult)
            {
                case DeadManSwitchTaskExecutionResult.TaskFinishedGracefully:
                    TasksThatFinishedGracefully++;
                    break;
                case DeadManSwitchTaskExecutionResult.TaskThrewAnException:
                    TasksThatThrewAnException++;
                    break;
                case DeadManSwitchTaskExecutionResult.TaskWasCancelled:
                    TasksThatWereCanceled++;
                    break;
            }

            switch (switchResult)
            {    
                case DeadManSwitchResult.DeadManSwitchWasTriggered:
                    DeadManSwitchesTriggered++;
                    break;
            }
        }
    }
}