using System;

namespace SshTerminalComponent
{
    /// <summary>
    /// Custom exception for SSH connection errors
    /// </summary>
    public class CustomSshConnectionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the CustomSshConnectionException class
        /// </summary>
        public CustomSshConnectionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CustomSshConnectionException class with an inner exception
        /// </summary>
        public CustomSshConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}