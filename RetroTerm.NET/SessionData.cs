using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using Aga.Controls.Tree;

namespace RetroTerm.NET
{
    /// <summary>
    /// Represents a session for SSH/terminal connection
    /// </summary>
    public class SessionData
    {
        [YamlMember(Alias = "Host", ApplyNamingConventions = false)]
        public string Host { get; set; } = "";

        [YamlMember(Alias = "Port", ApplyNamingConventions = false)]
        public int Port { get; set; } = 22;

        [YamlMember(Alias = "Username", ApplyNamingConventions = false)]
        public string Username { get; set; } = "";

        [YamlMember(Alias = "Password", ApplyNamingConventions = false)]
        public string Password { get; set; } = "";

        [YamlMember(Alias = "DisplayName", ApplyNamingConventions = false)]
        public string DisplayName { get; set; } = "";
        
        // Device information properties
        [YamlMember(Alias = "DeviceType", ApplyNamingConventions = false)]
        public string DeviceType { get; set; } = "";
        
        [YamlMember(Alias = "Model", ApplyNamingConventions = false)]
        public string Model { get; set; } = "";
        
        [YamlMember(Alias = "SerialNumber", ApplyNamingConventions = false)]
        public string SerialNumber { get; set; } = "";
        
        [YamlMember(Alias = "SoftwareVersion", ApplyNamingConventions = false)]
        public string SoftwareVersion { get; set; } = "";
        
        [YamlMember(Alias = "Vendor", ApplyNamingConventions = false)]
        public string Vendor { get; set; } = "";
        
        [YamlMember(Alias = "credsid", ApplyNamingConventions = false)]
public string CredsId { get; set; } = "";
    }

    /// <summary>
    /// Represents a folder containing multiple sessions
    /// </summary>
    public class SessionFolder
    {
        [YamlMember(Alias = "FolderName", ApplyNamingConventions = false)]
        public string FolderName { get; set; } = "";
        
        [YamlMember(Alias = "sessions", ApplyNamingConventions = false)]
        public List<SessionData> Sessions { get; set; } = new List<SessionData>();
    }

}