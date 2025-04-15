using System;

namespace SshTerminalComponent
{
    /// <summary>
    /// Arguments for the ConnectionStateChanged event
    /// </summary>
    public class SshConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether the connection is established
        /// </summary>
        public bool IsConnected { get; }
        
        /// <summary>
        /// Gets the host address
        /// </summary>
        public string Host { get; }
        
        /// <summary>
        /// Gets the port number
        /// </summary>
        public int Port { get; }
        
        /// <summary>
        /// Gets the username
        /// </summary>
        public string Username { get; }
        
        /// <summary>
        /// Initializes a new instance of the SshConnectionEventArgs class
        /// </summary>
        public SshConnectionEventArgs(bool isConnected, string host = "", int port = 0, string username = "")
        {
            IsConnected = isConnected;
            Host = host;
            Port = port;
            Username = username;
        }
    }
}