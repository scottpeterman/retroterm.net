using System;

namespace SshTerminalComponent
{

    public class SshTerminalErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// Gets the exception that caused the error
        /// </summary>
        public Exception Exception { get; }
        
        /// <summary>
        /// Initializes a new instance of the SshTerminalErrorEventArgs class
        /// </summary>
        public SshTerminalErrorEventArgs(string message, Exception exception = null)
        {
            Message = message;
            Exception = exception;
        }
    }
    
    /// <summary>
    /// Event arguments for terminal resize events
    /// </summary>
    public class TerminalResizeEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the number of columns
        /// </summary>
        public uint Columns { get; }
        
        /// <summary>
        /// Gets the number of rows
        /// </summary>
        public uint Rows { get; }
        
        /// <summary>
        /// Initializes a new instance of the TerminalResizeEventArgs class
        /// </summary>
        public TerminalResizeEventArgs(uint columns, uint rows)
        {
            Columns = columns;
            Rows = rows;
        }
    }
}