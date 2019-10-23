namespace DeadManSwitch
{
    public enum DeadManSwitchStatus
    {
        /// <summary>
        /// Happy case: we received a notification within the timeout
        /// </summary>
        NotificationReceived,
        
        /// <summary>
        /// The switch is temporarily paused, so it must not stop the worker
        /// </summary>
        Paused,
        
        /// <summary>
        /// The switch has been resumed, the timeout clock must begin anew
        /// </summary>
        Resumed
    }
}