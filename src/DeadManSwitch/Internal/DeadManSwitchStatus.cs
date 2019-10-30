namespace DeadManSwitch.Internal
{
    internal enum DeadManSwitchStatus
    {
        /// <summary>
        /// Happy case: we received a notification within the timeout
        /// </summary>
        NotificationReceived,
        
        /// <summary>
        /// The switch is temporarily suspended, so it must not cancel the worker
        /// </summary>
        Suspended,
        
        /// <summary>
        /// The switch has been resumed, the timeout clock must begin anew
        /// </summary>
        Resumed
    }
}