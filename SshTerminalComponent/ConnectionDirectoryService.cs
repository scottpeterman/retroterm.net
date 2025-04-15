using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace SshTerminalComponent
{
    public class ConnectionDirectoryService
    {
        private const string RegistryPath = @"Software\RetroTerm.net\Connections";
        private const int MaxConnections = 10;
        
        // Save a connection profile to the registry
        public void SaveProfile(int slot, ConnectionProfile profile)
        {
            if (slot < 1 || slot > MaxConnections)
                throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be between 1 and 10");
                
            using (var key = Registry.CurrentUser.CreateSubKey($"{RegistryPath}\\{slot}"))
            {
                key.SetValue("Name", profile.Name ?? string.Empty);
                key.SetValue("Host", profile.Host ?? string.Empty);
                key.SetValue("Port", profile.Port);
                key.SetValue("Username", profile.Username ?? string.Empty);
                key.SetValue("Password", EncryptPassword(profile.EncryptedPassword));
                key.SetValue("LastConnected", profile.LastConnected.ToString("o"));
            }
        }
        
        // Load a connection profile from the registry
        public ConnectionProfile LoadProfile(int slot)
        {
            if (slot < 1 || slot > MaxConnections)
                throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be between 1 and 10");
                
            using (var key = Registry.CurrentUser.OpenSubKey($"{RegistryPath}\\{slot}"))
            {
                if (key == null)
                    return null;
                    
                return new ConnectionProfile
                {
                    Name = key.GetValue("Name") as string,
                    Host = key.GetValue("Host") as string,
                    Port = Convert.ToInt32(key.GetValue("Port", 22)),
                    Username = key.GetValue("Username") as string,
                    EncryptedPassword = DecryptPassword(key.GetValue("Password") as string),
                    LastConnected = DateTime.Parse(key.GetValue("LastConnected", DateTime.MinValue.ToString("o")) as string)
                };
            }
        }
        
        // Get all saved profiles
        public Dictionary<int, ConnectionProfile> GetAllProfiles()
        {
            var profiles = new Dictionary<int, ConnectionProfile>();
            
            for (int i = 1; i <= MaxConnections; i++)
            {
                var profile = LoadProfile(i);
                if (profile != null)
                    profiles.Add(i, profile);
            }
            
            return profiles;
        }
        
        // Delete a profile
        public void DeleteProfile(int slot)
        {
            if (slot < 1 || slot > MaxConnections)
                throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be between 1 and 10");
                
            Registry.CurrentUser.DeleteSubKey($"{RegistryPath}\\{slot}", false);
        }
        
        // Encrypt password using DPAPI
        private string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;
                
            byte[] encryptedData = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(password),
                null,
                DataProtectionScope.CurrentUser);
                
            return Convert.ToBase64String(encryptedData);
        }
        
        // Decrypt password using DPAPI
        private string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return string.Empty;
                
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);
                byte[] decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    null,
                    DataProtectionScope.CurrentUser);
                    
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}