using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;

namespace RetroTerm.NET.Models
{
    /// <summary>
    /// Main Theme class that represents a complete theme
    /// </summary>
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
    
    /// <summary>
    /// UI Colors class for form elements
    /// </summary>
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
    
    /// <summary>
    /// Terminal Colors class for xterm.js
    /// </summary>
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
        
        /// <summary>
        /// Converts terminal colors to a dictionary for JSON serialization
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                { "background", Background },
                { "foreground", Foreground },
                { "cursor", Cursor },
                { "cursorAccent", CursorAccent },
                { "selection", Selection },
                { "scrollbarBackground", ScrollbarBackground },
                { "scrollbarThumb", ScrollbarThumb },
                { "ansi", ANSI }
            };
            
            return dict;
        }
    }
}