using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using RetroTerm.NET.Models;

namespace RetroTerm.NET.Services
{
    /// <summary>
    /// Theme manager class to handle loading and applying themes
    /// </summary>
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
        public string CurrentThemeName => currentTheme?.Name ?? "Default";
        
        /// <summary>
        /// Constructor
        /// </summary>
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
        
         public void ResetToBuiltInThemes()
        {
            try
            {
                // Get a list of all themes that aren't built-in
                var builtInThemeNames = new HashSet<string>
                {
                    "Borland Classic",
                    "Classic Light",
                    "Modern Dark",
                    "Norton Commander"
                };
                
                // Remove all theme JSON files from the themes directory
                foreach (string file in Directory.GetFiles(themesDirectory, "*.json"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting theme file {file}: {ex.Message}");
                    }
                }
                
                // Remove all non-built-in themes from the availableThemes list
                availableThemes.RemoveAll(theme => !builtInThemeNames.Contains(theme.Name));
                
                // If current theme was removed, set to default
                if (!builtInThemeNames.Contains(CurrentThemeName))
                {
                    // Set default theme (first Borland if available, otherwise first theme)
                    currentTheme = availableThemes.FirstOrDefault(t => t.Name == "Borland Classic") 
                                ?? availableThemes.FirstOrDefault();
                                
                    // Notify listeners of theme change
                    if (currentTheme != null)
                    {
                        OnThemeChanged(new ThemeChangedEventArgs(currentTheme));
                    }
                }
                
                Console.WriteLine($"Reset to {availableThemes.Count} built-in themes.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting to built-in themes: {ex.Message}");
                throw;
            }
        }
 
        /// <summary>
        /// Get list of available theme names
        /// </summary>
        public List<string> GetAvailableThemeNames()
        {
            return availableThemes.Select(t => t.Name).ToList();
        }
        
        /// <summary>
        /// Get theme by name
        /// </summary>
        public Theme GetTheme(string name)
        {
            return availableThemes.FirstOrDefault(t => t.Name == name);
        }
        
        /// <summary>
        /// Apply theme by name
        /// </summary>
        public Theme ApplyTheme(string themeName)
        {
            var theme = availableThemes.FirstOrDefault(t => t.Name == themeName);
            if (theme == null)
                return null;
                
            currentTheme = theme;
            OnThemeChanged(new ThemeChangedEventArgs(theme));
            return theme;
        }
        
        /// <summary>
        /// Apply the current theme to a form and all its controls recursively
        /// </summary>
        public void ApplyThemeToForm(Form form)
        {
            // if (CurrentTheme == null || form == null)
            //     return;
                
            ApplyThemeToControlRecursively(form, CurrentTheme);
        }
        
        /// <summary>
        /// Helper method to recursively apply theme to all controls
        /// </summary>
        private void ApplyThemeToControlRecursively(Control control, Theme theme)
        {
            // Apply theme based on control type
            ApplyThemeToControl(control, theme);
            
            // Recursively apply to child controls
            foreach (Control child in control.Controls)
            {
                ApplyThemeToControlRecursively(child, theme);
            }
            
            // Handle special case of MenuStrip items which aren't in Controls collection
            if (control is MenuStrip strip)
            {
                foreach (ToolStripItem item in strip.Items)
                {
                    if (item is ToolStripMenuItem menuItem)
                    {
                        ApplyThemeToToolStripItem(menuItem, theme);
                        
                        // Recursively handle dropdown items
                        ApplyThemeToMenuItems(menuItem.DropDownItems, theme);
                    }
                }
            }
        }
        
        /// <summary>
        /// Apply theme to a specific control based on its type
        /// </summary>
        private void ApplyThemeToControl(Control control, Theme theme)
        {
            if (control is Form form)
            {
                form.BackColor = Theme.HexToColor(theme.UI.Background);
                form.ForeColor = Theme.HexToColor(theme.UI.Text);
            }
            else if (control is Button button)
            {
                // Special handling for connected/disconnected buttons
                if (button.Name == "connectButton" || button.Text == "CONNECT")
                {
                    button.BackColor = Theme.HexToColor(theme.UI.DisconnectedButtonBackground);
                    button.ForeColor = Theme.HexToColor(theme.UI.DisconnectedButtonText);
                }
                else if (control is Splitter splitter)
                    {
                        // Apply the border color from theme to the splitter
                        splitter.BackColor = Theme.HexToColor(theme.UI.ButtonText);
                        

                    }
                else if (button.Name == "disconnectButton" || button.Text == "DISCONNECT")
                {
                    button.BackColor = Theme.HexToColor(theme.UI.ConnectedButtonBackground);
                    button.ForeColor = Theme.HexToColor(theme.UI.ConnectedButtonText);
                }

    else if (control is TabControl tabControl)
    {
        tabControl.BackColor = Theme.HexToColor(theme.UI.Background);
        tabControl.ForeColor = Theme.HexToColor(theme.UI.Text);
        
        // Apply to all tab pages immediately
        foreach (TabPage page in tabControl.TabPages)
        {
            page.BackColor = Theme.HexToColor(theme.UI.Background);
            page.ForeColor = Theme.HexToColor(theme.UI.Text);
            
            // Ensure all controls within the tab page are themed
            foreach (Control pageControl in page.Controls)
            {
                ApplyThemeToControlRecursively(pageControl, theme);
            }
        }
    }
                else
                {
                    button.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
                    button.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
                }
                
                if (button.FlatStyle == FlatStyle.Flat)
                    button.FlatAppearance.BorderColor = Theme.HexToColor(theme.UI.Border);
            }
            else if (control is Panel panel)
            {
                // Special handling for status panel
                if (panel.Name == "statusPanel")
                {
                    panel.BackColor = Theme.HexToColor(theme.UI.StatusBarBackground);
                    panel.ForeColor = Theme.HexToColor(theme.UI.StatusBarText);
                }
                else if (panel.Parent != null && panel.Parent.Name == "functionKeyPanel")
                {
                    panel.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                }
                else
                {
                    panel.BackColor = Theme.HexToColor(theme.UI.Background);
                }
            }
            else if (control is Label label)
            {
                // Special handling for status label
                if (label.Name == "statusLabel")
                {
                    label.BackColor = Theme.HexToColor(theme.UI.StatusBarBackground);
                    label.ForeColor = Theme.HexToColor(theme.UI.StatusBarText);
                }
                // Special handling for function key labels
                else if (control.Parent != null && (control.Parent.Name == "keyFlow" || 
                         (control.Parent.Parent != null && control.Parent.Parent.Name == "functionKeyPanel")))
                {
                    label.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                    label.ForeColor = Theme.HexToColor(theme.UI.MenuText);
                    label.Invalidate(); // Force repaint for custom rendering
                }
                else
                {
                    label.ForeColor = Theme.HexToColor(theme.UI.Text);
                }
            }
            else if (control is TableLayoutPanel tablePanel)
            {
                if (tablePanel.Name == "toolbarContainer")
                {
                    tablePanel.BackColor = Theme.HexToColor(theme.UI.Background);
                }
                else if (tablePanel.Name == "leftButtonPanel" || tablePanel.Name == "rightButtonPanel")
                {
                    tablePanel.BackColor = Theme.HexToColor(theme.UI.Background);
                }
                else
                {
                    tablePanel.BackColor = Theme.HexToColor(theme.UI.Background);
                }
            }
            else if (control is FlowLayoutPanel flowPanel)
            {
                if (flowPanel.Name == "keyFlow")
                {
                    flowPanel.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                }
                else
                {
                    flowPanel.BackColor = Theme.HexToColor(theme.UI.Background);
                }
            }
            else if (control is MenuStrip menuStrip)
            {
                menuStrip.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                menuStrip.ForeColor = Theme.HexToColor(theme.UI.MenuText);
            }
            else if (control is TabControl tabControl)
            {
                tabControl.BackColor = Theme.HexToColor(theme.UI.Background);
                tabControl.ForeColor = Theme.HexToColor(theme.UI.Text);
            }
            else if (control is TabPage tabPage)
            {
                tabPage.BackColor = Theme.HexToColor(theme.UI.Background);
                tabPage.ForeColor = Theme.HexToColor(theme.UI.Text);
            }
        }
        
// Add this debugging method to ThemeManager


/// <summary>
/// Debug method to visualize control hierarchy with special focus on tab controls
/// </summary>
public void DebugControlHierarchy(Control parentControl, int level = 0)
{
    return;
    // Create a StringBuilder to collect debug info
    System.Text.StringBuilder debugInfo = new System.Text.StringBuilder();
    CollectDebugInfo(parentControl, debugInfo, level);
    
    // Show the debug info in a message box or dedicated form
    ShowDebugInfo(debugInfo.ToString());
    
    // Apply visual debugging highlights
    ApplyDebugHighlights(parentControl);
}

public void UpdateSplitter(Splitter splitter)
{if (splitter != null && CurrentTheme != null)
    {
        splitter.BackColor = Theme.HexToColor(CurrentTheme.UI.Border);
        splitter.Invalidate(); // Force a redraw
    }
}
private void CollectDebugInfo(Control control, System.Text.StringBuilder output, int level)
{
    // Skip form itself
    if (!(control is Form))
    {
        string indent = new string(' ', level * 2);
        string controlType = control.GetType().Name;
        
        // Special handling for tab controls
        if (control is TabControl tabControl)
        {
            output.AppendLine($"{indent}[TAB CONTROL]: {controlType}, Name: {control.Name}");
            output.AppendLine($"{indent}  DrawMode: {tabControl.DrawMode}, Appearance: {tabControl.Appearance}");
            output.AppendLine($"{indent}  BackColor: {tabControl.BackColor}, ItemSize: {tabControl.ItemSize}");
            output.AppendLine($"{indent}  Parent: {tabControl.Parent?.GetType().Name}, Parent.BackColor: {tabControl.Parent?.BackColor}");
            output.AppendLine($"{indent}  TabCount: {tabControl.TabCount}");
        }
        else if (control is TabPage tabPage)
        {
            output.AppendLine($"{indent}[TAB PAGE]: {controlType}, Text: \"{tabPage.Text}\", Name: {control.Name}");
            output.AppendLine($"{indent}  BackColor: {tabPage.BackColor}, Tag: {tabPage.Tag ?? "null"}");
            output.AppendLine($"{indent}  Parent: {tabPage.Parent?.GetType().Name}, Parent.BackColor: {tabPage.Parent?.BackColor}");
        }
        else
        {
            // General control info
            string prefix = "";
            if (controlType.Contains("Container") || controlType.Contains("Layout") || 
                control.Name?.Contains("container", StringComparison.OrdinalIgnoreCase) == true)
            {
                prefix = "[CONTAINER] ";
            }
            
            output.AppendLine($"{indent}{prefix}Control: {controlType}, Name: {control.Name}, " +
                             $"BackColor: {control.BackColor}, Size: {control.Size}");
        }
    }
    
    // Recursively process child controls
    foreach (Control child in control.Controls)
    {
        CollectDebugInfo(child, output, level + 1);
    }
}

private void ApplyDebugHighlights(Control control)
{
    // Skip form itself
    if (!(control is Form))
    {
        // Add visual debug elements based on control type
        if (control is Panel panel)
        {
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.BackColor = Color.FromArgb(255, panel.BackColor.R, panel.BackColor.G, panel.BackColor.B);
        }
        else if (control is TableLayoutPanel tablePanel)
        {
            tablePanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.InsetDouble;
            tablePanel.BackColor = Color.FromArgb(255, tablePanel.BackColor.R, tablePanel.BackColor.G, tablePanel.BackColor.B);
        }
        else if (control is TabControl tabControl)
        {
            // Highlight tab controls with a red tint
            tabControl.BackColor = Color.FromArgb(255, Math.Min(255, tabControl.BackColor.R + 40), 
                                                tabControl.BackColor.G, tabControl.BackColor.B);
        }
        else if (control is TabPage tabPage)
        {
            // Highlight tab pages with a border
            tabPage.BorderStyle = BorderStyle.FixedSingle;
        }
    }
    
    // Recursively process child controls
    foreach (Control child in control.Controls)
    {
        ApplyDebugHighlights(child);
    }
}

private void ShowDebugInfo(string debugInfo)
{
    // Create a dedicated form to display the debug info
    Form debugForm = new Form
    {
        Text = "Control Hierarchy Debug Info",
        Width = 800,
        Height = 600,
        StartPosition = FormStartPosition.CenterScreen
    };
    
    TextBox textBox = new TextBox
    {
        Multiline = true,
        ScrollBars = ScrollBars.Both,
        Dock = DockStyle.Fill,
        ReadOnly = true,
        Font = new Font("Consolas", 9),
        Text = debugInfo
    };
    
    debugForm.Controls.Add(textBox);
    debugForm.Show();
}

// Add this method to ThemeManager
public void FixTabPageBackgrounds(Form form)
{
    // Find all TabControls in the form
    foreach (Control control in form.Controls)
    {
        if (control is TabControl tabControl)
        {
            ApplyThemeToTabControl(tabControl);
        }
        else
        {
            // Search for TabControls in child controls
            FindAndFixTabControls(control);
        }
    }
}

private void FindAndFixTabControls(Control parent)
{
    foreach (Control control in parent.Controls)
    {
        if (control is TabControl tabControl)
        {
            ApplyThemeToTabControl(tabControl);
        }
        else if (control.HasChildren)
        {
            FindAndFixTabControls(control);
        }
    }
}

private void ApplyThemeToTabControl(TabControl tabControl)
{
    // Apply theme to TabControl
    tabControl.BackColor = Theme.HexToColor(CurrentTheme.UI.Background);
    
    // The key part - set each TabPage's background color
    foreach (TabPage page in tabControl.TabPages)
    {
        // Force the TabPage to take the theme color
        page.BackColor = Theme.HexToColor(CurrentTheme.UI.Background);
        page.ForeColor = Theme.HexToColor(CurrentTheme.UI.Text);
        
        // Also apply to all child controls of the TabPage
        foreach (Control child in page.Controls)
        {
            if (!(child is WebView2)) // Skip WebView2 controls as they handle their own rendering
            {
                child.BackColor = Theme.HexToColor(CurrentTheme.UI.Background);
                child.ForeColor = Theme.HexToColor(CurrentTheme.UI.Text);
            }
        }
    }
    
        
    // Force redraw
    tabControl.Invalidate(true);
}

        /// <summary>
        /// Apply theme to ToolStripItem
        /// </summary>
        private void ApplyThemeToToolStripItem(ToolStripItem item, Theme theme)
        {
            item.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
            item.ForeColor = Theme.HexToColor(theme.UI.MenuText);
        }
        
        /// <summary>
        /// Helper method for menu items
        /// </summary>
        private void ApplyThemeToMenuItems(ToolStripItemCollection items, Theme theme)
        {
            foreach (ToolStripItem item in items)
            {
                ApplyThemeToToolStripItem(item, theme);
                
                if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
                {
                    ApplyThemeToMenuItems(menuItem.DropDownItems, theme);
                }
            }
        }
        
        /// <summary>
        /// Save a new theme or update existing one
        /// </summary>
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

        /// <summary>
        /// Get theme by partial name (for searching/loading)
        /// </summary>
        public Theme GetThemeByPartialName(string partialName)
        {
            return availableThemes.FirstOrDefault(t => 
                t.Name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        
        /// <summary>
        /// Import a theme from file
        /// </summary>
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
        
        /// <summary>
        /// Export a theme to file
        /// </summary>
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
        
        /// <summary>
        /// Get standard Borland colors from current theme
        /// </summary>
        public Color GetBorlandBlue()
        {
            return CurrentTheme != null 
                ? Theme.HexToColor(CurrentTheme.UI.Background) 
                : Color.FromArgb(0, 0, 170); // Default Borland Blue
        }
        
        public Color GetBorlandCyan()
        {
            return CurrentTheme != null 
                ? Theme.HexToColor(CurrentTheme.UI.ButtonBackground) 
                : Color.FromArgb(0, 170, 170); // Default Borland Cyan
        }
        
        public Color GetBorlandDarkBlue()
        {
            return CurrentTheme != null 
                ? Theme.HexToColor(CurrentTheme.UI.MenuHighlight) 
                : Color.FromArgb(0, 0, 128); // Default Borland Dark Blue
        }
        
        /// <summary>
        /// Load built-in themes
        /// </summary>
        private void LoadBuiltInThemes()
        {
            // Borland Classic
            var borlandTheme = new Theme
            {
                Name = "Borland Classic",
                Description = "Classic Borland IDE blue theme",
                Author = "RetroTerm.NET",
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
            
            // Rest of the built-in themes remain unchanged
            // Light theme
            var lightTheme = new Theme
            {
                Name = "Classic Light",
                Description = "Light theme with blue accents",
                Author = "RetroTerm.NET",
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
                Author = "RetroTerm.NET",
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
            
            // Norton Commander-like theme
            var nortonTheme = new Theme
            {
                Name = "Norton Commander",
                Description = "Classic Norton Commander blue theme",
                Author = "RetroTerm.NET",
                Version = "1.0",
                BaseTheme = "dark",
                UI = new UIColors
                {
                    Background = "#000080",              // Navy Blue
                    Text = "#FFFFFF",                    // White
                    InputText = "#FFFF00",               // Yellow
                    ButtonBackground = "#00AAFF",        // Light Blue
                    ButtonText = "#000000",              // Black
                    ButtonBorder = "#000000",            // Black
                    Highlight = "#00AAFF",               // Light Blue
                    Border = "#FFFFFF",                  // White
                    StatusBarBackground = "#00AAFF",     // Light Blue
                    StatusBarText = "#000000",           // Black
                    MenuBackground = "#888888",          // Gray
                    MenuText = "#000000",                // Black
                    MenuHighlight = "#0000AA",           // Dark Blue
                    MenuHighlightText = "#FFFFFF",       // White
                    HotkeyText = "#FFFF00",              // Yellow
                    ConnectedButtonBackground = "#AA0000", // Dark Red
                    ConnectedButtonText = "#FFFFFF",     // White
                    DisconnectedButtonBackground = "#00AAFF", // Light Blue
                    DisconnectedButtonText = "#000000"   // Black
                },
                Terminal = new TerminalColors
                {
                    Background = "#000080",             // Navy Blue
                    Foreground = "#AAAAAA",             // Light Gray
                    Cursor = "#FFFFFF",                 // White
                    CursorAccent = "#000000",           // Black
                    Selection = "#00AAFF",              // Light Blue
                    ScrollbarBackground = "#333333",    // Dark Gray
                    ScrollbarThumb = "#888888"          // Gray
                }
            };
            availableThemes.Add(nortonTheme);
        }
        
        /// <summary>
        /// Load user themes from the themes directory
        /// </summary>
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
        
        /// <summary>
        /// Raise theme changed event
        /// </summary>
        protected virtual void OnThemeChanged(ThemeChangedEventArgs e)
        {
            ThemeChanged?.Invoke(this, e);
        }
    }
    
    /// <summary>
    /// Event args for theme changes
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public Theme Theme { get; }
        
        public ThemeChangedEventArgs(Theme theme)
        {
            Theme = theme;
        }
    }
}