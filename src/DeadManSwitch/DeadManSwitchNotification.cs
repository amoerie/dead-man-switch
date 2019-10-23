using System;

namespace DeadManSwitch
{
    public class DeadManSwitchNotification
    {
        public DateTime Timestamp { get; }
        public string Content { get; }

        public DeadManSwitchNotification(string content)
        {
            Timestamp = DateTime.Now;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }
    }
}