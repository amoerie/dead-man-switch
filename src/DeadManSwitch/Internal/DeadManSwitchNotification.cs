using System;

namespace DeadManSwitch.Internal
{
    internal class DeadManSwitchNotification
    {
        public DeadManSwitchNotification(string content)
        {
            Timestamp = DateTime.Now;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary>
        ///     The date and time when the notification occurred
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        ///     The content of the notification
        /// </summary>
        public string Content { get; }
    }
}