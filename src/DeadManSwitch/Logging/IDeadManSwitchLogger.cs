using System;
using System.Diagnostics.CodeAnalysis;

namespace DeadManSwitch.Logging
{
    /// <summary>
    /// A logger that can write log messages
    /// </summary>
    /// <typeparam name="T">The log context</typeparam>
    public interface IDeadManSwitchLogger<out T>
    {
        /// <summary>
        /// Logs a trace message 
        /// </summary>
        void Trace(string message, params object[] args);
        
        /// <summary>
        /// Logs a debug message
        /// </summary>
        void Debug(string message, params object[] args);
        
        /// <summary>
        /// Logs an information message
        /// </summary>
        void Information(string message, params object[] args);
        
        /// <summary>
        /// Logs a warning message
        /// </summary>
        void Warning(string message, params object[] args);
        
        /// <summary>
        /// Logs an error message
        /// </summary>
        [SuppressMessage("ReSharper", "CA1716", Justification = "This is a common name for a log method")]
        void Error(string message, params object[] args);
        
        /// <summary>
        /// Logs an error message with an exception
        /// </summary>
        [SuppressMessage("ReSharper", "CA1716", Justification = "This is a common name for a log method")]
        void Error(Exception exception, string message, params object[] args);
    }
}