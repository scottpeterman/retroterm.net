using System;
using Microsoft.Win32;

namespace RetroTerm.NET.Services
{
    /// <summary>
    /// Service to manage application settings
    /// </summary>
    public class SettingsManager
    {
        private const string RegistryKey = @"SOFTWARE\RetroTerm.NET";
        
        // Application settings
        public string LastThemeName { get; set; } = "Borland Classic";
        public bool EnableModemSound { get; set; } = false;
        public int FontSize { get; set; } = 14;
        public int WindowWidth { get; set; } = 800;
        public int WindowHeight { get; set; } = 600;
        public bool IsMaximized { get; set; } = false;
        
        // Event for settings changes
        public event EventHandler SettingsChanged;
        
        /// <summary>
        /// Loads settings from the registry
        /// </summary>
        public bool LoadSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey))
                {
                    if (key != null)
                    {
                        // Read application settings
                        LastThemeName = key.GetValue("LastThemeName", LastThemeName)?.ToString() ?? LastThemeName;
                        EnableModemSound = Convert.ToInt32(key.GetValue("EnableModemSound", EnableModemSound ? 1 : 0) ?? 0) == 1;
                        FontSize = Convert.ToInt32(key.GetValue("FontSize", FontSize) ?? FontSize);
                        
                        // Read window settings
                        WindowWidth = Convert.ToInt32(key.GetValue("WindowWidth", WindowWidth) ?? WindowWidth);
                        WindowHeight = Convert.ToInt32(key.GetValue("WindowHeight", WindowHeight) ?? WindowHeight);
                        IsMaximized = Convert.ToInt32(key.GetValue("IsMaximized", IsMaximized ? 1 : 0) ?? 0) == 1;
                        
                        // Notify listeners
                        OnSettingsChanged();
                        
                        return true;
                    }
                }
                
                // If no settings found, use defaults
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Saves settings to the registry
        /// </summary>
        public bool SaveSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryKey))
                {
                    if (key != null)
                    {
                        // Save application settings
                        key.SetValue("LastThemeName", LastThemeName);
                        key.SetValue("EnableModemSound", EnableModemSound ? 1 : 0);
                        key.SetValue("FontSize", FontSize);
                        
                        // Save window settings
                        key.SetValue("WindowWidth", WindowWidth);
                        key.SetValue("WindowHeight", WindowHeight);
                        key.SetValue("IsMaximized", IsMaximized ? 1 : 0);
                        
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        public void ResetSettings()
        {
            // Reset to default values
            LastThemeName = "Borland Classic";
            EnableModemSound = false;
            FontSize = 14;
            WindowWidth = 800;
            WindowHeight = 600;
            IsMaximized = false;
            
            // Notify listeners
            OnSettingsChanged();
        }
        
        /// <summary>
        /// Raises the SettingsChanged event
        /// </summary>
        protected virtual void OnSettingsChanged()
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}