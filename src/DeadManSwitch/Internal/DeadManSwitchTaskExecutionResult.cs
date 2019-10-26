namespace DeadManSwitch.Internal
{
    internal enum DeadManSwitchTaskExecutionResult
    {
        TaskWasCancelled,
        TaskFinishedGracefully,
        TaskThrewAnException,
    }
}