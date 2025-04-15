using System;

namespace SshTerminalComponent
{
    /// <summary>
    /// Arguments for the DataReceived event
    /// </summary>
    public class SshDataEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the data received from the SSH server
        /// </summary>
        public string Data { get; }
        
        /// <summary>
        /// Initializes a new instance of the SshDataEventArgs class
        /// </summary>
        public SshDataEventArgs(string data)
        {
            Data = data;
        }
    }
}