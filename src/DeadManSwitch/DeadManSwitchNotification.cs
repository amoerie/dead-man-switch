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

        public override string ToString()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff}: {Content}";
        }
    }
}