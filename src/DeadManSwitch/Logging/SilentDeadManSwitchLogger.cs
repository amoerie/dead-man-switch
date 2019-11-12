using System;

namespace DeadManSwitch.Logging
{
    internal sealed class SilentDeadManSwitchLogger<T>: IDeadManSwitchLogger<T>
    {
        public void Trace(string message, params object[] args)
        {
            
        }

        public void Debug(string message, params object[] args)
        {
        }

        public void Information(string message, params object[] args)
        {
        }

        public void Warning(string message, params object[] args)
        {
        }

        public void Error(string message, params object[] args)
        {
        }

        public void Error(Exception exception, string message, params object[] args)
        {
        }
    }
}