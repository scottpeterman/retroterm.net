using System.Collections.Generic;

namespace RetroTerm.NET.Models
{
    public class FolderStructure
    {
        public string folder_name { get; set; }
        public List<SessionInfo> sessions { get; set; }
    }

    public class SessionInfo
    {
        public string DeviceType { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public string SoftwareVersion { get; set; }
        public string Vendor { get; set; }
        public string credsid { get; set; }
        public string display_name { get; set; }
        public string host { get; set; }
        public string port { get; set; }
    }
}