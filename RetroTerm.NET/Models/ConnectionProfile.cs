using System;

namespace RetroTerm.NET.Models
{
    /// <summary>
    /// Represents a saved SSH connection profile
    /// </summary>
    public class ConnectionProfile
    {
        /// <summary>
        /// Unique identifier for the profile
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Display name for the profile
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// Host name or IP address
        /// </summary>
        public string Host { get; set; } = "";
        
        /// <summary>
        /// Port number (default: 22)
        /// </summary>
        public int Port { get; set; } = 22;
        
        /// <summary>
        /// Username for authentication
        /// </summary>
        public string Username { get; set; } = "";
        
        /// <summary>
        /// Password for authentication (should be encrypted in real application)
        /// </summary>
        public string EncryptedPassword { get; set; } = "";
        
        /// <summary>
        /// Path to private key file (if using key-based authentication)
        /// </summary>
        public string PrivateKeyFile { get; set; } = "";
        
        /// <summary>
        /// Passphrase for private key (if needed)
        /// </summary>
        public string PrivateKeyPassphrase { get; set; } = "";
        
        /// <summary>
        /// Whether to use key-based authentication
        /// </summary>
        public bool UseKeyAuthentication => !string.IsNullOrEmpty(PrivateKeyFile);
        
        /// <summary>
        /// Date and time when this profile was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Date and time when this profile was last accessed
        /// </summary>
        public DateTime LastAccessedDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Number of times this profile has been used
        /// </summary>
        public int UsageCount { get; set; } = 0;
        
        /// <summary>
        /// Gets a formatted display string for this profile
        /// </summary>
        public string DisplayString => $"{Name} ({Username}@{Host}:{Port})";
        
        /// <summary>
        /// Update the LastAccessedDate and increment UsageCount
        /// </summary>
        public void RecordUse()
        {
            LastAccessedDate = DateTime.Now;
            UsageCount++;
        }
        
        /// <summary>
        /// Create a clone of this profile
        /// </summary>
        public ConnectionProfile Clone()
        {
            return new ConnectionProfile
            {
                Id = Guid.NewGuid().ToString(), // New ID for the clone
                Name = $"{Name} (Copy)",
                Host = Host,
                Port = Port,
                Username = Username,
                EncryptedPassword = EncryptedPassword,
                PrivateKeyFile = PrivateKeyFile,
                PrivateKeyPassphrase = PrivateKeyPassphrase,
                CreatedDate = DateTime.Now,
                LastAccessedDate = DateTime.Now,
                UsageCount = 0,
                
            };
        }
    }
}