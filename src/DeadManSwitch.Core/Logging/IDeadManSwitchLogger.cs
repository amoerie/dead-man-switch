using System;

namespace DeadManSwitch.Logging
{
    public interface IDeadManSwitchLogger<T>
    {
        void Trace(string message, params object[] args);
        void Debug(string message, params object[] args);
        void Information(string message, params object[] args);
        void Warning(string message, params object[] args);
        void Error(string message, params object[] args);
        void Error(Exception exception, string message, params object[] args);
    }
}