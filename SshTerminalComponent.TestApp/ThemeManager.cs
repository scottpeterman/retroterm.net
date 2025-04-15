using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SshTerminalComponent
{
    // Main Theme class that represents a complete theme
    public class Theme
    {
        // Theme metadata
        public string Name { get; set; } = "Default Theme";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public string Version { get; set; } = "1.0";
        public string BaseTheme { get; set; } = "dark"; // "dark" or "light"
        
        // UI and Terminal colors
        public UIColors UI { get; set; } = new UIColors();
        public TerminalColors Terminal { get; set; } = new TerminalColors();
        
        // Helper to determine if this is a dark theme
        [JsonIgnore]
        public bool IsDarkTheme => BaseTheme?.ToLowerInvariant() == "dark";
        
        // Utility method to convert hex color to System.Drawing.Color
        public static Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Color.Empty;
                
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);
                
            try
            {
                if (hex.Length == 6)
                    return Color.FromArgb(
                        Convert.ToInt32(hex.Substring(0, 2), 16),
                        Convert.ToInt32(hex.Substring(2, 2), 16),
                        Convert.ToInt32(hex.Substring(4, 2), 16));
            }
            catch
            {
                // Return a default color if conversion fails
                Console.WriteLine($"Invalid hex color: {hex}");
                return Color.Gray;
            }
            
            return Color.Gray;
        }
        
        // Utility method to convert Color to hex string
        public static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
    
    // UI Colors class for form elements
    public class UIColors
    {
        // Main UI colors
        public string Background { get; set; } = "#0000AA"; // Default Borland Blue
        public string Text { get; set; } = "#FFFFFF";
        public string InputText { get; set; } = "#FFFF00";
        public string ButtonBackground { get; set; } = "#00AAAA";
        public string ButtonText { get; set; } = "#000000";
        public string ButtonBorder { get; set; } = "#000000";
        public string Highlight { get; set; } = "#00AAAA";
        public string Border { get; set; } = "#FFFFFF";
        
        // Status bar colors
        public string StatusBarBackground { get; set; } = "#00AAAA";
        public string StatusBarText { get; set; } = "#000000";
        
        // Menu colors
        public string MenuBackground { get; set; } = "#C0C0C0";
        public string MenuText { get; set; } = "#000000";
        public string MenuHighlight { get; set; } = "#00AAAA";
        public string MenuHighlightText { get; set; } = "#FFFFFF";
        public string HotkeyText { get; set; } = "#FF0000";
        
        // Connection state colors
        public string ConnectedButtonBackground { get; set; } = "#FF0000";
        public string ConnectedButtonText { get; set; } = "#FFFFFF";
        public string DisconnectedButtonBackground { get; set; } = "#00AAAA";
        public string DisconnectedButtonText { get; set; } = "#000000";
        
        // Function key colors
        public string FunctionKeyText { get; set; } = "#FF0000";
        public string FunctionKeyDescriptionText { get; set; } = "#000000";
    }
    
    // Terminal Colors class for xterm.js
    public class TerminalColors
    {
        // Terminal display colors
        public string Background { get; set; } = "#000000";
        public string Foreground { get; set; } = "#C0C0C0";
        public string Cursor { get; set; } = "#FFFFFF";
        public string CursorAccent { get; set; } = "#000000";
        public string Selection { get; set; } = "#00AAAA";
        
        // Terminal UI element colors
        public string ScrollbarBackground { get; set; } = "#333333";
        public string ScrollbarThumb { get; set; } = "#666666";
        
        // ANSI colors (16 standard colors)
        public List<string> ANSI { get; set; } = new List<string>
        {
            "#000000", // Black
            "#AA0000", // Red
            "#00AA00", // Green
            "#AA5500", // Yellow
            "#0000AA", // Blue
            "#AA00AA", // Magenta
            "#00AAAA", // Cyan
            "#AAAAAA", // White
            "#555555", // Bright Black
            "#FF5555", // Bright Red
            "#55FF55", // Bright Green
            "#FFFF55", // Bright Yellow
            "#5555FF", // Bright Blue
            "#FF55FF", // Bright Magenta
            "#55FFFF", // Bright Cyan
            "#FFFFFF"  // Bright White
        };
    }
    
    // Theme manager class to handle loading and applying themes
    public class ThemeManager
    {
        private List<Theme> availableThemes = new List<Theme>();
        private Theme currentTheme;
        private string themesDirectory;
        
        // Event for theme changes
        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;
        
        // Properties
        public Theme CurrentTheme => currentTheme;
        public IReadOnlyList<Theme> AvailableThemes => availableThemes.AsReadOnly();
        
        // Constructor
        public ThemeManager(string themesDirectory)
        {
            this.themesDirectory = themesDirectory;
            
            // Create themes directory if it doesn't exist
            if (!Directory.Exists(themesDirectory))
            {
                Directory.CreateDirectory(themesDirectory);
            }
            
            // Add built-in themes
            LoadBuiltInThemes();
            
            // Load user themes
            LoadUserThemes();
            
            // Set default theme (first Borland if available, otherwise first theme)
            currentTheme = availableThemes.FirstOrDefault(t => t.Name == "Borland Classic") 
                        ?? availableThemes.FirstOrDefault();
        }
        
        // Get list of available theme names
        public List<string> GetAvailableThemeNames()
        {
            return availableThemes.Select(t => t.Name).ToList();
        }
        
        // Get theme by name
        public Theme GetTheme(string name)
        {
            return availableThemes.FirstOrDefault(t => t.Name == name);
        }
        
        // Apply theme by name
        public bool ApplyTheme(string themeName)
        {
            var theme = availableThemes.FirstOrDefault(t => t.Name == themeName);
            if (theme == null)
                return false;
                
            currentTheme = theme;
            OnThemeChanged(new ThemeChangedEventArgs(theme));
            return true;
        }
        
        // Save a new theme or update existing one
        public void SaveTheme(Theme theme)
        {
            if (string.IsNullOrEmpty(theme.Name))
                throw new ArgumentException("Theme must have a name");
                
            // Generate filename from theme name
            string filename = theme.Name.Replace(" ", "_") + ".json";
            string path = Path.Combine(themesDirectory, filename);
            
            // Serialize theme to JSON
            string json = JsonSerializer.Serialize(theme, new JsonSerializerOptions { 
                WriteIndented = true 
            });
            
            // Write to file
            File.WriteAllText(path, json);
            
            // Add or update theme in available themes
            var existingTheme = availableThemes.FirstOrDefault(t => t.Name == theme.Name);
            if (existingTheme != null)
            {
                int index = availableThemes.IndexOf(existingTheme);
                availableThemes[index] = theme;
            }
            else
            {
                availableThemes.Add(theme);
            }
        }

        public Theme GetThemeByPartialName(string partialName)
{
    return availableThemes.FirstOrDefault(t => 
        t.Name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0);
}
        
        // Import a theme from file
        public Theme ImportTheme(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var theme = JsonSerializer.Deserialize<Theme>(json);
                
                if (theme == null || string.IsNullOrEmpty(theme.Name))
                    throw new FormatException("Invalid theme format - missing name");
                    
                // Add to available themes
                var existingTheme = availableThemes.FirstOrDefault(t => t.Name == theme.Name);
                if (existingTheme != null)
                {
                    int index = availableThemes.IndexOf(existingTheme);
                    availableThemes[index] = theme;
                }
                else
                {
                    availableThemes.Add(theme);
                }
                
                // Save to themes directory
                SaveTheme(theme);
                
                return theme;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to import theme: {ex.Message}");
                throw;
            }
        }
        
        // Export a theme to file
        public void ExportTheme(string themeName, string filePath)
        {
            var theme = availableThemes.FirstOrDefault(t => t.Name == themeName);
            if (theme == null)
                throw new ArgumentException($"Theme '{themeName}' not found");
                
            string json = JsonSerializer.Serialize(theme, new JsonSerializerOptions { 
                WriteIndented = true 
            });
            
            File.WriteAllText(filePath, json);
        }
        
        // Load built-in themes
        private void LoadBuiltInThemes()
        {
            // Borland Classic
            var borlandTheme = new Theme
            {
                Name = "Borland Classic",
                Description = "Classic Borland IDE blue theme",
                Author = "SSH Terminal App",
                Version = "1.0",
                BaseTheme = "dark",
                UI = new UIColors
                {
                    Background = "#0000AA",             // BorlandBlue
                    Text = "#FFFFFF",                   // BorlandWhite
                    InputText = "#FFFF00",              // BorlandYellow
                    ButtonBackground = "#00AAAA",       // BorlandCyan
                    ButtonText = "#000000",             // BorlandBlack
                    ButtonBorder = "#000000",           // BorlandBlack
                    Highlight = "#00AAAA",              // BorlandCyan
                    Border = "#FFFFFF",                 // BorlandWhite
                    StatusBarBackground = "#00AAAA",    // BorlandCyan
                    StatusBarText = "#000000",          // BorlandBlack
                    MenuBackground = "#C0C0C0",         // BorlandGray
                    MenuText = "#000000",               // BorlandBlack
                    MenuHighlight = "#00AAAA",          // BorlandCyan
                    MenuHighlightText = "#FFFFFF",      // BorlandWhite
                    HotkeyText = "#FF0000",             // BorlandRed
                    ConnectedButtonBackground = "#FF0000", // BorlandRed
                    ConnectedButtonText = "#FFFFFF",    // BorlandWhite
                    DisconnectedButtonBackground = "#00AAAA", // BorlandCyan
                    DisconnectedButtonText = "#000000", // BorlandBlack
                    FunctionKeyText = "#FF0000",        // BorlandRed
                    FunctionKeyDescriptionText = "#000000" // BorlandBlack
                },
                Terminal = new TerminalColors
                {
                    Background = "#0000AA",           // Borland Blue
                    Foreground = "#C0C0C0",           // Light gray
                    Cursor = "#FFFFFF",               // White
                    CursorAccent = "#000000",         // Black
                    Selection = "#00AAAA",            // BorlandCyan
                    ScrollbarBackground = "#333333",  // Dark gray
                    ScrollbarThumb = "#666666"        // Gray
                }
            };
            availableThemes.Add(borlandTheme);
            
            // Light theme
            var lightTheme = new Theme
            {
                Name = "Classic Light",
                Description = "Light theme with blue accents",
                Author = "SSH Terminal App",
                Version = "1.0",
                BaseTheme = "light",
                UI = new UIColors
                {
                    Background = "#B0C4DE",             // Light steel blue
                    Text = "#000080",                   // Navy blue
                    InputText = "#000000",              // Black
                    ButtonBackground = "#AAAAFF",       // Light blue
                    ButtonText = "#000000",             // Black
                    ButtonBorder = "#000080",           // Navy blue
                    Highlight = "#800000",              // Maroon
                    Border = "#000080",                 // Navy blue
                    StatusBarBackground = "#AAAAFF",    // Light blue
                    StatusBarText = "#000080",          // Navy blue
                    MenuBackground = "#D0D0D0",         // Light gray
                    MenuText = "#000000",               // Black
                    MenuHighlight = "#800000",          // Maroon
                    MenuHighlightText = "#FFFFFF",      // White
                    HotkeyText = "#FF0000",             // Red
                    ConnectedButtonBackground = "#FF8080", // Light red
                    ConnectedButtonText = "#000000",    // Black
                    DisconnectedButtonBackground = "#AAAAFF", // Light blue
                    DisconnectedButtonText = "#000000"  // Black
                },
                Terminal = new TerminalColors
                {
                    Background = "#FFFFFF",           // White
                    Foreground = "#000000",           // Black
                    Cursor = "#000000",               // Black
                    CursorAccent = "#FFFFFF",         // White
                    Selection = "#ADD8E6",            // Light blue
                    ScrollbarBackground = "#DDDDDD",  // Light gray
                    ScrollbarThumb = "#999999"        // Gray
                }
            };
            availableThemes.Add(lightTheme);
            
            // Modern Dark theme
            var modernDarkTheme = new Theme
            {
                Name = "Modern Dark",
                Description = "Contemporary dark theme with cooler colors",
                Author = "SSH Terminal App",
                Version = "1.0",
                BaseTheme = "dark",
                UI = new UIColors
                {
                    Background = "#1E1E1E",              // Dark gray
                    Text = "#FFFFFF",                    // White
                    InputText = "#CCCCCC",               // Light gray
                    ButtonBackground = "#2D2D2D",        // Medium gray
                    ButtonText = "#FFFFFF",              // White
                    ButtonBorder = "#3F3F3F",            // Lighter gray
                    Highlight = "#007ACC",               // Bright blue
                    Border = "#3F3F3F",                  // Medium gray
                    StatusBarBackground = "#007ACC",     // Bright blue
                    StatusBarText = "#FFFFFF",           // White
                    MenuBackground = "#2D2D2D",          // Medium gray
                    MenuText = "#FFFFFF",                // White
                    MenuHighlight = "#007ACC",           // Bright blue
                    MenuHighlightText = "#FFFFFF",       // White
                    HotkeyText = "#FF6464",              // Soft red
                    ConnectedButtonBackground = "#2EA043", // Green
                    ConnectedButtonText = "#FFFFFF",     // White
                    DisconnectedButtonBackground = "#2D2D2D", // Medium gray
                    DisconnectedButtonText = "#FFFFFF"   // White
                },
                Terminal = new TerminalColors
                {
                    Background = "#1E1E1E",             // Dark gray
                    Foreground = "#D4D4D4",             // Light gray
                    Cursor = "#CCCCCC",                 // Gray
                    CursorAccent = "#1E1E1E",           // Dark gray
                    Selection = "#264F78",              // Dark blue
                    ScrollbarBackground = "#1E1E1E",    // Dark gray
                    ScrollbarThumb = "#424242"          // Medium gray
                }
            };
            availableThemes.Add(modernDarkTheme);
        }
        
        // Load user themes from the themes directory
        private void LoadUserThemes()
        {
            try
            {
                foreach (string file in Directory.GetFiles(themesDirectory, "*.json"))
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var theme = JsonSerializer.Deserialize<Theme>(json);
                        
                        if (theme != null && !string.IsNullOrEmpty(theme.Name))
                        {
                            // Don't add if a theme with this name already exists
                            if (!availableThemes.Any(t => t.Name == theme.Name))
                            {
                                availableThemes.Add(theme);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading theme from {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user themes: {ex.Message}");
            }
        }
        
        // Raise theme changed event
        protected virtual void OnThemeChanged(ThemeChangedEventArgs e)
        {
            ThemeChanged?.Invoke(this, e);
        }
    }
    
    // Event args for theme changes
    public class ThemeChangedEventArgs : EventArgs
    {
        public Theme Theme { get; }
        
        public ThemeChangedEventArgs(Theme theme)
        {
            Theme = theme;
        }
    }
}