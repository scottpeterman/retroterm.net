using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RetroTerm.NET.Models;

namespace RetroTerm.NET.Services
{
    /// <summary>
    /// Service to manage saved SSH connection profiles
    /// </summary>
    public class ConnectionDirectoryService
    {
        private string directoryPath;
        private List<ConnectionProfile> profiles = new List<ConnectionProfile>();
        
        /// <summary>
        /// Event raised when the profiles collection changes
        /// </summary>
        public event EventHandler ProfilesChanged;
        
        /// <summary>
        /// Get all profiles
        /// </summary>
        public IReadOnlyList<ConnectionProfile> Profiles => profiles.AsReadOnly();
        
        /// <summary>
        /// Constructor
        /// </summary>
        public ConnectionDirectoryService(string directoryPath)
        {
            this.directoryPath = directoryPath;
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            // Load profiles from directory
            LoadProfiles();
        }
        
        /// <summary>
        /// Default constructor that uses AppData folder
        /// </summary>
        public ConnectionDirectoryService() 
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RetroTerm.NET", "Connections"))
        {
        }
        
        /// <summary>
        /// Load profiles from JSON files in the directory
        /// </summary>
        private void LoadProfiles()
        {
            profiles.Clear();
            
            try
            {
                // Get all profile JSON files
                string[] files = Directory.GetFiles(directoryPath, "*.json");
                
                foreach (string file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var profile = JsonSerializer.Deserialize<ConnectionProfile>(json);
                        
                        if (profile != null)
                        {
                            profiles.Add(profile);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading profile from {file}: {ex.Message}");
                    }
                }
                
                // Sort by name
                profiles = profiles.OrderBy(p => p.Name).ToList();
                
                // Notify listeners
                OnProfilesChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading profiles: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Save a profile
        /// </summary>
        public void SaveProfile(ConnectionProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
                
            // Ensure unique ID
            if (string.IsNullOrEmpty(profile.Id))
            {
                profile.Id = Guid.NewGuid().ToString();
            }
            
            // Generate filename
            string filename = $"{profile.Id}.json";
            string path = Path.Combine(directoryPath, filename);
            
            // Serialize profile to JSON
            string json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { 
                WriteIndented = true 
            });
            
            // Write to file
            File.WriteAllText(path, json);
            
            // Update or add profile in the list
            var existingProfile = profiles.FirstOrDefault(p => p.Id == profile.Id);
            if (existingProfile != null)
            {
                int index = profiles.IndexOf(existingProfile);
                profiles[index] = profile;
            }
            else
            {
                profiles.Add(profile);
            }
            
            // Sort by name
            profiles = profiles.OrderBy(p => p.Name).ToList();
            
            // Notify listeners
            OnProfilesChanged();
        }
        
        /// <summary>
        /// Save a profile with a specific slot number (for backward compatibility)
        /// </summary>
        public void SaveProfile(int slot, ConnectionProfile profile)
        {
            // Ensure the profile has a name that indicates its slot if not already set
            if (string.IsNullOrEmpty(profile.Name) || profile.Name.StartsWith("Connection "))
            {
                profile.Name = $"Connection {slot}";
            }
            
            // Save the profile
            SaveProfile(profile);
        }
        
        /// <summary>
        /// Delete a profile
        /// </summary>
        public void DeleteProfile(string profileId)
        {
            var profile = profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile == null)
                return;
                
            // Remove file
            string filename = $"{profile.Id}.json";
            string path = Path.Combine(directoryPath, filename);
            
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            
            // Remove from list
            profiles.Remove(profile);
            
            // Notify listeners
            OnProfilesChanged();
        }
        
        /// <summary>
        /// Delete a profile by slot (for backward compatibility)
        /// </summary>
        public void DeleteProfile(int slot)
        {
            if (slot > 0 && slot <= profiles.Count)
            {
                // Get the profile at the specified index (slot-1 since slots are 1-based)
                var profile = profiles[slot - 1];
                DeleteProfile(profile.Id);
            }
        }
        
        /// <summary>
        /// Get a profile by ID
        /// </summary>
        public ConnectionProfile GetProfile(string profileId)
        {
            return profiles.FirstOrDefault(p => p.Id == profileId);
        }
        
        /// <summary>
        /// Get a profile by name
        /// </summary>
        public ConnectionProfile GetProfileByName(string name)
        {
            return profiles.FirstOrDefault(p => p.Name == name);
        }
        
        /// <summary>
        /// Search profiles by name, host, or username
        /// </summary>
        public List<ConnectionProfile> SearchProfiles(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return profiles.ToList();
                
            searchTerm = searchTerm.ToLowerInvariant();
            
            return profiles.Where(p => 
                p.Name.ToLowerInvariant().Contains(searchTerm) ||
                p.Host.ToLowerInvariant().Contains(searchTerm) ||
                p.Username.ToLowerInvariant().Contains(searchTerm)
            ).ToList();
        }
        
        /// <summary>
        /// Get all profiles as a dictionary with slot numbers as keys
        /// (for backward compatibility)
        /// </summary>
        public Dictionary<int, ConnectionProfile> GetAllProfiles()
        {
            Dictionary<int, ConnectionProfile> result = new Dictionary<int, ConnectionProfile>();
            
            for (int i = 0; i < profiles.Count && i < 10; i++)
            {
                result[i + 1] = profiles[i];
            }
            
            return result;
        }
        
        /// <summary>
        /// Raise the ProfilesChanged event
        /// </summary>
        protected virtual void OnProfilesChanged()
        {
            ProfilesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}