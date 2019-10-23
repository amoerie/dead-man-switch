namespace DeadManSwitch
{
    public enum DeadManSwitchResult
    {
        /// <summary>
        /// The dead man's switch was triggered and the work has been canceled
        /// </summary>
        DeadManSwitchWasTriggered,
        
        /// <summary>
        /// The dead man's switch did not trigger
        /// </summary>
        DeadManSwitchWasNotTriggered
    }
}