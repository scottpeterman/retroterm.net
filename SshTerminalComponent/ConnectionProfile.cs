using System;

namespace SshTerminalComponent
{
    public class ConnectionProfile
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public int Port { get; set; } = 22;
        public string Username { get; set; }
        public string EncryptedPassword { get; set; }
        public DateTime LastConnected { get; set; }
    }
}