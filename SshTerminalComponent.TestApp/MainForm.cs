using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SshTerminalComponent;
using System.Media;
using SshTerminalComponent.TestApp;
using Microsoft.Win32;
using System.Diagnostics;

public partial class MainForm : Form
{
    private bool useRetroFont = false;
private ApplicationSettings appSettings = new ApplicationSettings();
private const string RegistryKey = @"SOFTWARE\RetroTerm.NET";
    private ThemeManager themeManager;
 private bool enableModemSound = false;
    private Button closeButton;
    // DOS/Borland color palette
    private static readonly Color BorlandBlue = Color.FromArgb(0, 0, 170);
    private static readonly Color BorlandCyan = Color.FromArgb(0, 170, 170);  // Darker cyan for better visibility
    private static readonly Color BorlandWhite = Color.FromArgb(255, 255, 255);
    private static readonly Color BorlandYellow = Color.FromArgb(255, 255, 0); // Bright yellow for input text

    private static readonly Color BorlandGreen = Color.FromArgb(0, 255, 0); // Bright green for buttons


    private static readonly Color BorlandBlack = Color.FromArgb(0, 0, 0);
    private static readonly Color BorlandRed = Color.FromArgb(255, 0, 0);
    private static readonly Color BorlandGray = Color.FromArgb(192, 192, 192);

    private static readonly Color BorlandCyanText = Color.FromArgb(0, 255, 255);
    private static readonly Color BorlandLightGray = Color.FromArgb(192, 192, 192);
    private static readonly Color BorlandCodeGreen = Color.FromArgb(0, 255, 0);
    private static readonly Color BorlandCodeRed = Color.FromArgb(255, 0, 0);
    
    // Light theme colors
    private static readonly Color LightBg = Color.FromArgb(176, 196, 222);    // Light steel blue
    private static readonly Color LightText = Color.FromArgb(0, 0, 128);      // Navy blue
    private static readonly Color LightHighlight = Color.FromArgb(128, 0, 0); // Maroon
    private static readonly Color LightBorder = Color.FromArgb(0, 0, 128);    // Navy blue
    
    private SshTerminalControl terminal;
    private TextBox hostTextBox;
    private TextBox portTextBox;
    private TextBox usernameTextBox;
    private TextBox passwordTextBox;
    private Button connectButton;
    private CheckBox darkModeCheckbox;
    private PrivateFontCollection fontCollection;
    private Font dosFont;
    private ToolStripStatusLabel statusLabel;
    private Panel resizeGrip;
    private bool isDarkMode = true;
    private Panel menuPanel;

public class ApplicationSettings
    {
        // Connection settings
        public string LastHost { get; set; } = "";
        public string LastPort { get; set; } = "22";
        public string LastUsername { get; set; } = "";
        
        // Application settings
        public string LastThemeName { get; set; } = "Borland Classic";
        public bool EnableModemSound { get; set; } = false;
        
        // Terminal settings
        public int FontSize { get; set; } = 14;
        
        // Window settings
        public int WindowWidth { get; set; } = 800;
        public int WindowHeight { get; set; } = 600;
        public bool IsMaximized { get; set; } = false;
    }
    
    public MainForm()
    {
        // Initialize theme manager
    string themesDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SSHTerminal", "Themes");
        
    themeManager = new ThemeManager(themesDirectory);
    themeManager.ThemeChanged += ThemeManager_ThemeChanged;
        // Set form style before initialization
        this.FormBorderStyle = FormBorderStyle.None;
        // this.BackColor = BorlandBlue;
        // this.ForeColor = BorlandWhite;
        
        // Initialize DOS font
        InitializeDOSFont();
        
        // Initialize form components
        InitializeComponent();
        
        // Apply the current theme
        ApplyCurrentTheme();

        // Set up the menu bar
        CreateDOSMenuBar();
        
        // Set up function key bar
        Panel functionKeyBar = CreateFunctionKeyBar();
        this.Controls.Add(functionKeyBar);        
        
        
        // Set up events
        SetupEvents();
        LoadSettings();
    }
    


private bool SaveSettings()
{
    try
    {
        // Update settings with current values
        appSettings.LastHost = hostTextBox.Text;
        appSettings.LastPort = portTextBox.Text;
        appSettings.LastUsername = usernameTextBox.Text;
        
        // Get current theme name directly from theme manager
        appSettings.LastThemeName = themeManager.CurrentTheme?.Name ?? "Borland Classic";
        
        // Save modem sound setting
        appSettings.EnableModemSound = enableModemSound;
        
        // Get font size from terminal
        if (terminal != null && terminal.TerminalSettings != null)
        {
            appSettings.FontSize = terminal.TerminalSettings.FontSize;
        }
        
        // Save window size and state if not minimized
        if (this.WindowState != FormWindowState.Minimized)
        {
            appSettings.IsMaximized = (this.WindowState == FormWindowState.Maximized);
            
            if (!appSettings.IsMaximized)
            {
                appSettings.WindowWidth = this.Width;
                appSettings.WindowHeight = this.Height;
            }
        }
        
        // Save to registry
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKey))
        {
            if (key == null)
            {
                UpdateStatus("Failed to create registry key");
                return false;
            }
            
            // Connection settings
            key.SetValue("LastHost", appSettings.LastHost);
            key.SetValue("LastPort", appSettings.LastPort);
            key.SetValue("LastUsername", appSettings.LastUsername);
            
            // Application settings
            key.SetValue("LastThemeName", appSettings.LastThemeName);
            key.SetValue("EnableModemSound", appSettings.EnableModemSound ? 1 : 0);
            key.SetValue("FontSize", appSettings.FontSize);
            
            // Window settings
            key.SetValue("WindowWidth", appSettings.WindowWidth);
            key.SetValue("WindowHeight", appSettings.WindowHeight);
            key.SetValue("IsMaximized", appSettings.IsMaximized ? 1 : 0);
        }
        
        UpdateStatus("Settings saved successfully");
        return true;
    }
    catch (Exception ex)
    {
        UpdateStatus($"Error saving settings: {ex.Message}");
        return false;
    }
}

private void LoadSettings()
{
    try
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKey))
        {
            if (key != null)
            {
                // Connection settings
                appSettings.LastHost = key.GetValue("LastHost", "")?.ToString() ?? "";
                appSettings.LastPort = key.GetValue("LastPort", "22")?.ToString() ?? "22";
                appSettings.LastUsername = key.GetValue("LastUsername", "")?.ToString() ?? "";
                
                // Application settings
                appSettings.LastThemeName = key.GetValue("LastThemeName", "Borland Classic")?.ToString() ?? "Borland Classic";
                appSettings.EnableModemSound = Convert.ToInt32(key.GetValue("EnableModemSound", 0) ?? 0) == 1;
                appSettings.FontSize = Convert.ToInt32(key.GetValue("FontSize", 14) ?? 14);
                
                // Window settings
                appSettings.WindowWidth = Convert.ToInt32(key.GetValue("WindowWidth", 800) ?? 800);
                appSettings.WindowHeight = Convert.ToInt32(key.GetValue("WindowHeight", 600) ?? 600);
                appSettings.IsMaximized = Convert.ToInt32(key.GetValue("IsMaximized", 0) ?? 0) == 1;
                
                // Apply loaded settings
                hostTextBox.Text = appSettings.LastHost;
                portTextBox.Text = appSettings.LastPort;
                usernameTextBox.Text = appSettings.LastUsername;
                enableModemSound = appSettings.EnableModemSound;
                
                // Apply window size and state
                if (appSettings.IsMaximized)
                {
                    this.WindowState = FormWindowState.Maximized;
                }
                else if (appSettings.WindowWidth > 0 && appSettings.WindowHeight > 0)
                {
                    this.Width = appSettings.WindowWidth;
                    this.Height = appSettings.WindowHeight;
                }
                
                // Update modem sound menu item
                UpdateModemSoundMenuItem();
                
                string themesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SSHTerminal", "Themes");
                
               

// Set default theme (first partial match for "Borland", otherwise first theme)
                Theme loadTheme = themeManager.GetThemeByPartialName(appSettings.LastThemeName);
                // Apply theme (done last to ensure UI is ready)
                if (!string.IsNullOrEmpty(appSettings.LastThemeName))
                {
                    themeManager.ApplyTheme(appSettings.LastThemeName);
                }
                ApplyXtermTheme(loadTheme);
                UpdateStatus("Settings loaded");
            }
        }
    }
    catch (Exception ex)
    {
        // Log error but don't disrupt startup
        UpdateStatus($"Error loading settings: {ex.Message}");
    }
}


private void UpdateModemSoundMenuItem()
{
    // Find and update the modem sound menu item
    foreach (ToolStripItem item in this.MainMenuStrip.Items)
    {
        if (item is ToolStripMenuItem menuItem && menuItem.Text == "Terminal")
        {
            foreach (ToolStripItem subItem in menuItem.DropDownItems)
            {
                if (subItem is ToolStripMenuItem subMenuItem && subMenuItem.Text.Contains("Modem Sound"))
                {
                    subMenuItem.Checked = enableModemSound;
                    break;
                }
            }
            break;
        }
    }
}

    private void ResetToDefaultThemes()
{
    DialogResult result = MessageBox.Show(
        "This will restore the default themes and remove any custom themes. Continue?",
        "Reset Themes",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question);
        
    if (result == DialogResult.Yes)
    {
        try
        {
            // Get the themes directory
            string themesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SSHTerminal", "Themes");
                
            // Clear the directory if it exists
            if (Directory.Exists(themesDirectory))
            {
                // Delete all theme files
                foreach (string file in Directory.GetFiles(themesDirectory, "*.json"))
                {
                    File.Delete(file);
                }
            }
            
            // Recreate the ThemeManager to initialize default themes
            themeManager = new ThemeManager(themesDirectory);
            themeManager.ThemeChanged += ThemeManager_ThemeChanged;
            
            // Refresh the themes menu
            RefreshThemesMenu();
            
            // Apply the Borland Classic theme
            ApplyTheme(true);
            
            UpdateStatus("Themes reset to defaults");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error resetting themes: {ex.Message}", 
                "Reset Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("Failed to reset themes");
        }
    }
}

// Add helper to recursively update controls:

// Updated method for MainForm class - replace the existing UpdateControlColors method
private void UpdateControlColors(Control parent, Theme theme)
{
    foreach (Control control in parent.Controls)
    {
        // Skip the terminal control - it has its own theme handling
        if (control is SshTerminalControl)
            continue;
            
        // Handle specific control types
        if (control is Label label)
        {
            label.BackColor = Theme.HexToColor(theme.UI.Background);
            label.ForeColor = Theme.HexToColor(theme.UI.Text);
        }
        else if (control is TextBox textBox && !(textBox is TextBox))
        {
            textBox.BackColor = Theme.HexToColor(theme.UI.Background);
            textBox.ForeColor = Theme.HexToColor(theme.UI.InputText);
            textBox.BorderStyle = BorderStyle.Fixed3D;
        }
        else if (control is TextBox TextBox)
        {
            // Properly update TextBox properties with theme colors
            TextBox.BackColor = Theme.HexToColor(theme.UI.Background);
            TextBox.ForeColor = Theme.HexToColor(theme.UI.InputText);
            
            // Explicitly set the border color to match the theme
            Color borderColor = Theme.HexToColor(theme.UI.Border);
            // TextBox.BorderColor = borderColor;
            
            // Force immediate visual refresh
            TextBox.Invalidate();
            TextBox.Update();
        }
        else if (control is CheckBox checkBox)
        {
            checkBox.BackColor = Theme.HexToColor(theme.UI.Background);
            checkBox.ForeColor = Theme.HexToColor(theme.UI.Text);
        }
        else if (control is Button button && button != closeButton)
        {
            // Don't change button if it's the connect button and we're connected
            if (button == connectButton && terminal.IsConnected)
            {
                button.BackColor = Theme.HexToColor(theme.UI.ConnectedButtonBackground);
                button.ForeColor = Theme.HexToColor(theme.UI.ConnectedButtonText);
            }
            else
            {
                button.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
                button.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
            }
            
            // Ensure button gets redrawn with new colors
            button.Refresh();
        }
        else if (control is Panel panel)
        {
            // Skip resize grip
            if (panel == resizeGrip)
                continue;
                
            // Status panel has special colors
            if (panel.Controls.OfType<Label>().Any(l => (string)l.Tag == "statusTextLabel"))
            {
                panel.BackColor = Theme.HexToColor(theme.UI.StatusBarBackground);
                panel.ForeColor = Theme.HexToColor(theme.UI.StatusBarText);
                
                // Update status label too
                foreach (Control c in panel.Controls)
                {
                    if (c is Label statusLabel)
                    {
                        statusLabel.BackColor = Theme.HexToColor(theme.UI.StatusBarBackground);
                        statusLabel.ForeColor = Theme.HexToColor(theme.UI.StatusBarText);
                    }
                }
            }
            else
            {
                panel.BackColor = Theme.HexToColor(theme.UI.Background);
                panel.ForeColor = Theme.HexToColor(theme.UI.Text);
            }
        }
        else if (control is TableLayoutPanel tablePanel)
        {
            tablePanel.BackColor = Theme.HexToColor(theme.UI.Background);
            tablePanel.ForeColor = Theme.HexToColor(theme.UI.Text);
        }
        
        // Recursively update child controls
        if (control.Controls.Count > 0)
        {
            UpdateControlColors(control, theme);
        }
    }
}

// Additional method to add in MainForm class to force redrawing all TextBoxes
private void ForceRedrawTextBoxes()
{
    // Create a helper method to find and refresh all TextBoxes
    void RefreshTextBoxes(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is TextBox TextBox)
            {
                // Force redraw
                TextBox.Invalidate();
                TextBox.Update();
            }
            
            // Recursively check child controls
            if (control.Controls.Count > 0)
            {
                RefreshTextBoxes(control);
            }
        }
    }
    
    // Start the recursive search from the form
    RefreshTextBoxes(this);
}


// Modify the Terminal_ConnectionStateChanged method to use theme colors:
private void Terminal_ConnectionStateChanged(object sender, SshConnectionEventArgs e)
{
    if (this.InvokeRequired)
    {
        this.BeginInvoke(new Action(() => Terminal_ConnectionStateChanged(sender, e)));
        return;
    }

    connectButton.Text = e.IsConnected ? "DISCONNECT" : "CONNECT";
    connectButton.Enabled = true;

    var theme = themeManager.CurrentTheme;
    if (theme != null)
    {
        if (e.IsConnected)
        {
            // Connected - use theme's connected button colors
            connectButton.BackColor = Theme.HexToColor(theme.UI.ConnectedButtonBackground);
            connectButton.ForeColor = Theme.HexToColor(theme.UI.ConnectedButtonText);
            
            this.Text = $"SSH TERMINAL - CONNECTED TO {e.Host}";
            UpdateStatus($"Connected to {e.Username}@{e.Host}:{e.Port}");
        }
        else
        {
            // Disconnected - use theme's disconnected button colors
            connectButton.BackColor = Theme.HexToColor(theme.UI.DisconnectedButtonBackground);
            connectButton.ForeColor = Theme.HexToColor(theme.UI.DisconnectedButtonText);
            
            this.Text = "SSH TERMINAL";
            UpdateStatus("Disconnected");
        }
    }
}


// Add theme changed handler:
private void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e)
{
    // Apply the new theme
    ApplyCurrentTheme();
}

// Update the ApplyCurrentTheme method to call our new method
private void ApplyCurrentTheme()
{
    var theme = themeManager.CurrentTheme;
    if (theme == null) return;
    
    // Set isDarkMode based on theme
    isDarkMode = theme.IsDarkTheme;
    
    // Apply UI colors
    try
    {
        // Main form colors
        this.BackColor = Theme.HexToColor(theme.UI.Background);
        this.ForeColor = Theme.HexToColor(theme.UI.Text);
        
        // Update all child controls with theme colors
        UpdateControlColors(this, theme);
        
        // Update menu panel and menu strip specifically
        if (menuPanel != null)
        {
            menuPanel.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
            
            // Find and update the MenuStrip inside the panel
            foreach (Control control in menuPanel.Controls)
            {
                if (control is MenuStrip menuStrip)
                {
                    menuStrip.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                    
                    // If the menu strip has a custom renderer, update it too
                    if (menuStrip.Renderer is DOSMenuRenderer menuRenderer)
                    {
                        menuRenderer.IsDarkMode = isDarkMode;
                        menuRenderer.UpdateThemeColors(theme);
                    }
                    
                    break;
                }
            }
        }
        
        // Update function key bar at the bottom to match menu background
        foreach (Control control in this.Controls)
        {
            if (control is Panel panel && panel.Dock == DockStyle.Bottom && panel.Height == 45)
            {
                // This is the function key bar
                panel.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                panel.ForeColor = Theme.HexToColor(theme.UI.MenuText);
                
                // Update child controls (function key labels)
                foreach (Control childControl in panel.Controls)
                {
                    if (childControl is FlowLayoutPanel flowPanel)
                    {
                        flowPanel.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                        
                        // Force redraw of all labels
                        foreach (Control labelControl in flowPanel.Controls)
                        {
                            labelControl.Invalidate();
                        }
                    }
                }
            }
        }
        
        // Terminal colors
        if (terminal != null)
        {
            // Set terminal theme (dark/light mode)
            terminal.SetTheme(isDarkMode ? TerminalTheme.Dark : TerminalTheme.Light);
            
            // Apply xterm.js specific colors
            terminal.SetTerminalColorsAsync(
                theme.Terminal.Background,
                theme.Terminal.Foreground,
                theme.Terminal.Cursor,
                theme.Terminal.ScrollbarBackground,
                theme.Terminal.ScrollbarThumb
            ).ConfigureAwait(false);
        }
        
        // Update close button
        if (closeButton != null)
        {
            closeButton.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
            closeButton.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
        }
        
        // Update menu renderer
        if (this.MainMenuStrip != null && this.MainMenuStrip.Renderer is DOSMenuRenderer mainRenderer)
        {
            mainRenderer.IsDarkMode = isDarkMode;
            mainRenderer.UpdateThemeColors(theme);
            this.MainMenuStrip.Invalidate();
        }
        
        // Force redraw of TextBoxes to ensure borders are updated
        ForceRedrawTextBoxes();
        
        // Refresh all painting
        this.Invalidate(true);
    }
    catch (Exception ex)
    {
        UpdateStatus($"Error applying theme: {ex.Message}");
    }
}
private void ApplyXtermTheme(Theme theme)
{
    if (theme == null || terminal == null) return;

    // First, make terminal invisible until properly themed to avoid color flashing
    terminal.Visible = false;
    
    // Use the terminal ready event to apply colors at the right time
    EventHandler terminalReadyHandler = null;
    terminalReadyHandler = (sender, e) => {
        try
        {
            // Remove the event handler to avoid multiple calls
            terminal.TerminalReady -= terminalReadyHandler;
            
            // Apply terminal theme colors
            terminal.SetTheme(theme.IsDarkTheme ? TerminalTheme.Dark : TerminalTheme.Light);
            terminal.SetTerminalColorsAsync(
                theme.Terminal.Background,
                theme.Terminal.Foreground,
                theme.Terminal.Cursor,
                theme.Terminal.ScrollbarBackground,
                theme.Terminal.ScrollbarThumb
            ).ContinueWith(t => {
                // Show terminal after coloring is complete
                this.BeginInvoke(new Action(() => {
                    terminal.Visible = true;
                }));
            });
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error applying xterm theme: {ex.Message}");
            terminal.Visible = true; // Make terminal visible even if theming fails
        }
    };
    
    // Subscribe to the terminal ready event
    terminal.TerminalReady += terminalReadyHandler;
    
    // If the terminal is already ready, invoke the handler directly
    if (terminal.WebViewInitialized)
    {
        terminalReadyHandler(null, EventArgs.Empty);
    }
}
    private void InitializeDOSFont()
    {
        // Try to use Perfect DOS VGA 437 if available, otherwise fall back to Consolas
        try
        {
            dosFont = new Font("Perfect DOS VGA 437", 12, FontStyle.Regular);
        }
        catch
        {
            try 
            {
                dosFont = new Font("Consolas", 14, FontStyle.Regular);
            }
            catch
            {
                // Last resort fallback
                dosFont = new Font(FontFamily.GenericMonospace, 14, FontStyle.Regular);
            }
        }
    }
    

    private void InitializeComponent()
{
    this.SuspendLayout();
    
    // Set up form properties
    this.Text = "SSH TERMINAL";
    // Calculate size based on screen dimensions
    Screen currentScreen = Screen.FromPoint(Cursor.Position);
    int width = (int)(currentScreen.WorkingArea.Width * 0.8);
    int height = (int)(currentScreen.WorkingArea.Height * 0.8);
    
    // Set the form size and position
    this.Size = new Size(width, height);
    this.StartPosition = FormStartPosition.CenterScreen;
    this.Font = dosFont;
    
    // Create a TableLayoutPanel as the main container
    TableLayoutPanel mainLayout = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 3,
        Padding = new Padding(1, 24, 1, 1), // Consistent padding
        Margin = new Padding(0)
    };
    
    // Configure rows: connection panel, terminal, status bar
    mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Connection panel
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Terminal
    mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Status bar
    
    // Create connection panel with DOS layout
    TableLayoutPanel connectionPanel = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(2),
        ColumnCount = 5,
        RowCount = 2,
        AutoSize = true,
        BackColor = BorlandBlue,
        ForeColor = BorlandWhite
    };
    
    connectionPanel.ColumnStyles.Clear();
    connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
    connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
    connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
    connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
    connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
    
    // Set explicit row heights
    connectionPanel.RowStyles.Clear();
    connectionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // Increased for host/port row
    connectionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // Increased for username/password row
    
    // Add host controls with DOS styling
    Label hostLabel = new Label
    {
        Text = "Host:",
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        TextAlign = ContentAlignment.MiddleRight,
        AutoSize = true,
        Font = dosFont,
        ForeColor = BorlandWhite,
        BackColor = BorlandBlue
    };
    connectionPanel.Controls.Add(hostLabel, 0, 0);
    
    hostTextBox = new TextBox
    {
        Dock = DockStyle.Fill,
        // Font = dosFont,
            Font = new Font(dosFont.FontFamily, dosFont.Size, dosFont.Style), // Reduced font size

        BackColor = BorlandBlue,
        ForeColor = BorlandYellow,
        BorderStyle = BorderStyle.None
    };
    connectionPanel.Controls.Add(hostTextBox, 1, 0);
    
    Label portLabel = new Label
    {
        Text = "Port:",
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        TextAlign = ContentAlignment.MiddleRight,
        AutoSize = true,
        Font = dosFont,
        ForeColor = BorlandWhite,
        BackColor = BorlandBlue
    };
    connectionPanel.Controls.Add(portLabel, 2, 0);
    
    portTextBox = new TextBox
    {
        Dock = DockStyle.Fill,
        Text = "22",
            Font = new Font(dosFont.FontFamily, dosFont.Size, dosFont.Style), // Reduced font size
        BackColor = BorlandBlue,
        ForeColor = BorlandYellow,
        BorderStyle = BorderStyle.None
    };
    connectionPanel.Controls.Add(portTextBox, 3, 0);
    
    // Add username/password controls
    Label usernameLabel = new Label
    {
        Text = "Username:",
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        TextAlign = ContentAlignment.MiddleRight,
        AutoSize = true,
        Font = dosFont,
        ForeColor = BorlandWhite,
        BackColor = BorlandBlue
    };
    connectionPanel.Controls.Add(usernameLabel, 0, 1);
    
    usernameTextBox = new TextBox
    {
        Dock = DockStyle.Fill,
        Font = new Font(dosFont.FontFamily, dosFont.Size, dosFont.Style), // Reduced font size
        BackColor = BorlandBlue,
        ForeColor = BorlandYellow,
        BorderStyle = BorderStyle.None
    };
    connectionPanel.Controls.Add(usernameTextBox, 1, 1);
    
    Label passwordLabel = new Label
    {
        Text = "Password:",
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        TextAlign = ContentAlignment.MiddleRight,
        AutoSize = true,
        Font = dosFont,
        ForeColor = BorlandWhite,
        BackColor = BorlandBlue
    };
    connectionPanel.Controls.Add(passwordLabel, 2, 1);
    
    passwordTextBox = new TextBox
    {
        Dock = DockStyle.Fill,
        PasswordChar = '*',
            Font = new Font(dosFont.FontFamily, dosFont.Size, dosFont.Style), // Reduced font size
        BackColor = BorlandBlue,
        ForeColor = BorlandYellow,
        BorderStyle = BorderStyle.None
    };
    connectionPanel.Controls.Add(passwordTextBox, 3, 1);
    
    // Connect button with DOS styling
    connectButton = new Button
    {
        Text = "CONNECT",
        Dock = DockStyle.Fill,
        FlatStyle = FlatStyle.Flat,
        BackColor = BorlandCyan,
        ForeColor = BorlandBlack,
        Width = 200,
        Height = 30, // Increased to match the taller rows
        Font = new Font(dosFont.FontFamily, dosFont.Size - 5, dosFont.Style),
        FlatAppearance = { BorderColor = Color.Black, BorderSize = 1 }
    };
    connectionPanel.Controls.Add(connectButton, 4, 1);
    
    // Dark mode checkbox
    darkModeCheckbox = new CheckBox
    {
        Text = "Dark Theme",
        Checked = true,
        AutoSize = true,
        Font = dosFont,
        ForeColor = BorlandWhite,
        BackColor = BorlandBlue
    };
    darkModeCheckbox.Visible = false;
    
    connectionPanel.Controls.Add(darkModeCheckbox, 4, 0);

    
    // Create a Panel for the terminal with double-line border
    Panel terminalPanel = new Panel
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(4),
        Margin = new Padding(4),
        BackColor = BorlandBlue
    };
    
    // Add DOS-style double-line border
    terminalPanel.Paint += (s, e) => {
        // Use current theme colors
        Color borderColor = Theme.HexToColor(themeManager.CurrentTheme?.UI.Border ?? (isDarkMode ? "#FFFFFF" : "#000080"));
        
        // Outer border
        e.Graphics.DrawRectangle(new Pen(borderColor), 0, 0, 
            terminalPanel.Width - 1, terminalPanel.Height - 1);
    
        // Inner border (double-line effect)
        e.Graphics.DrawRectangle(new Pen(borderColor), 2, 2, 
            terminalPanel.Width - 5, terminalPanel.Height - 5);
    };
    
    // Terminal control
    terminal = new SshTerminalControl
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(4) // Add margin inside the double border
    };
    terminalPanel.Controls.Add(terminal);
    
    // Create status panel with double-line border and title
    Panel statusPanel = new Panel
    {
        Dock = DockStyle.Fill,
        Height = 60,
        BackColor = BorlandCyan,
        ForeColor = BorlandBlack,
        Padding = new Padding(5, 5, 5, 5),
        Margin = new Padding(4, 0, 4, 4)
    };
    
    // Status text label
    statusLabel = new ToolStripStatusLabel("Ready");
    
    // Label to display status content
    Label statusTextLabel = new Label
    {
        Dock = DockStyle.Fill,
        Text = "Ready",
        Font = dosFont,
        BackColor = BorlandCyan,
        ForeColor = BorlandBlack,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(5, 0, 0, 0)
    };
    statusPanel.Controls.Add(statusTextLabel);
    
    // Store reference to status text label for updates
    statusTextLabel.Tag = "statusTextLabel";
    
    // Paint handler to draw the double-line border with title
    statusPanel.Paint += (s, e) => {
        Color borderColor = Theme.HexToColor(themeManager.CurrentTheme?.UI.Border ?? "#000000");
    
        // Draw outer border
        e.Graphics.DrawRectangle(new Pen(borderColor), 0, 0, 
            statusPanel.Width - 1, statusPanel.Height - 1);

        // Draw inner border (double-line effect)
        e.Graphics.DrawRectangle(new Pen(borderColor), 3, 3, 
            statusPanel.Width - 7, statusPanel.Height - 7);
    
        // Draw title background (to cover the top border lines)
        string title = " Status ";
        SizeF titleSize = e.Graphics.MeasureString(title, dosFont);
        int titleX = 10; // Position from left
    
        // Draw the title text on top of everything
        using (Font titleFont = new Font(dosFont.FontFamily, dosFont.Size, FontStyle.Bold))
        {
            e.Graphics.DrawString(title, titleFont, new SolidBrush(borderColor), titleX, 0);
        }
    };
    
    // Add all components to the main layout
    mainLayout.Controls.Add(connectionPanel, 0, 0);
    mainLayout.Controls.Add(terminalPanel, 0, 1);
    mainLayout.Controls.Add(statusPanel, 0, 2);
    
    // Add the main layout to the form
    this.Controls.Add(mainLayout);
    
    this.ResumeLayout(false);
    // Add a close button to the form
    closeButton = new Button
    {
        Text = "[X]",
        Font = new Font(dosFont.FontFamily, dosFont.Size, FontStyle.Bold),
        ForeColor = BorlandBlue,
        BackColor = BorlandGray,
        FlatStyle = FlatStyle.Flat,
        Size = new Size(70, 45),
        Location = new Point(this.Width - 75, 1), // Position adjusted to accommodate width

        Anchor = AnchorStyles.Top | AnchorStyles.Right,
        TabStop = false,
        Cursor = Cursors.Hand
    };

    // Configure flat appearance
    closeButton.FlatAppearance.BorderColor = BorlandBlack;
    closeButton.FlatAppearance.BorderSize = 0;

    // Add click event to close the application
    closeButton.Click += (s, e) => this.Close();

    // Add the button to the form and bring it to front
    this.Controls.Add(closeButton);
    closeButton.BringToFront();
}


    // Play WAV file before connecting
private async Task PlayConnectionSound()
{
    // Skip if sound is disabled
    if (!enableModemSound)
        return;
        
    string soundFilePath = Path.Combine(Environment.CurrentDirectory, "modem_short.wav");

    if (System.IO.File.Exists(soundFilePath))
    {
        using (SoundPlayer player = new SoundPlayer(soundFilePath))
        {
            player.PlaySync(); // Wait until sound finishes
        }
    }

    await Task.Delay(50); // Small delay to avoid UI lag
}


private void CreateDOSMenuBar()
{
    // Create a panel for the menu area to make it draggable
    menuPanel = new Panel
    {
        Dock = DockStyle.Top,
        Height = 60, 
        BackColor = BorlandGray
    };
    
    MenuStrip menuStrip = new MenuStrip
    {
        Dock = DockStyle.Top,
        BackColor = BorlandGray,
        ForeColor = BorlandBlack,
        Font = dosFont,
        Padding = new Padding(1, 2, 0, 2),
        RenderMode = ToolStripRenderMode.Professional
    };
    
    // Set custom renderer
    menuStrip.Renderer = new DOSMenuRenderer(isDarkMode);
    
    // File menu
    ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
    fileMenu.DropDownItems.Add(CreateMenuItem("Directory", 'D', (s, e) => ShowConnectionDirectory()));
    // fileMenu.DropDownItems.Add(CreateMenuItem("Open", 'O', (s, e) => { /* Add Open file handler here */ }));
    // fileMenu.DropDownItems.Add(CreateMenuItem("Save", 'S', (s, e) => { /* Add Save file handler here */ }));
    fileMenu.DropDownItems.Add(new ToolStripSeparator());
    fileMenu.DropDownItems.Add(CreateMenuItem("Exit", 'x', (s, e) => this.Close()));
    
    // Edit menu
    // ToolStripMenuItem editMenu = new ToolStripMenuItem("Edit");
    // editMenu.DropDownItems.Add(CreateMenuItem("Copy", 'C', async (s, e) => await terminal.CopySelection()));
    // editMenu.DropDownItems.Add(CreateMenuItem("Paste", 'P', async (s, e) => await terminal.PasteText()));
    // editMenu.DropDownItems.Add(new ToolStripSeparator());
    // editMenu.DropDownItems.Add(CreateMenuItem("Clear Screen", 'S', async (s, e) => await terminal.ClearScreen()));
    
    // Terminal menu
    ToolStripMenuItem terminalMenu = new ToolStripMenuItem("Terminal");
    ToolStripMenuItem soundMenuItem = CreateMenuItem("Modem Sound", 'M', (s, e) => ToggleModemSound());
    soundMenuItem.Checked = enableModemSound;
    terminalMenu.DropDownItems.Add(soundMenuItem);

    terminalMenu.DropDownItems.Add(new ToolStripSeparator());
    ToolStripMenuItem retroFontMenuItem = CreateMenuItem("Toggle Retro Font", 'F', (s, e) => ToggleTerminalFont());
    terminalMenu.DropDownItems.Add(retroFontMenuItem);


    // Add Themes submenu
    ToolStripMenuItem themesMenu = new ToolStripMenuItem("Themes");
    
    // Add built-in themes
    themesMenu.DropDownItems.Add(CreateMenuItem("Borland Classic", 'B', (s, e) => ApplyTheme(true)));
    themesMenu.DropDownItems.Add(CreateMenuItem("Light Theme", 'L', (s, e) => ApplyTheme(false)));
    
    // Add dynamic themes from ThemeManager if available
    if (themeManager != null)
    {
        var themeNames = themeManager.GetAvailableThemeNames();
        if (themeNames.Any())
        {
            // Add separator if we already have themes
            if (themesMenu.DropDownItems.Count > 0)
            {
                themesMenu.DropDownItems.Add(new ToolStripSeparator());
            }
            
            // Add each available theme
            foreach (var themeName in themeNames)
            {
                // Skip themes we've already added
                if (themeName == "Borland Classic" || themeName == "Light Theme")
                    continue;
                    
                // Create a menu item for each theme
                ToolStripMenuItem themeItem = new ToolStripMenuItem(themeName);
                themeItem.Click += (s, e) => themeManager.ApplyTheme(themeName);
                themesMenu.DropDownItems.Add(themeItem);
            }
        }
    }
    
    // Add theme management options
    themesMenu.DropDownItems.Add(new ToolStripSeparator());
    themesMenu.DropDownItems.Add(CreateMenuItem("Import Theme...", 'I', (s, e) => ImportTheme()));
    themesMenu.DropDownItems.Add(CreateMenuItem("Export Current Theme...", 'E', (s, e) => ExportCurrentTheme()));

    // Add this to your CreateDOSMenuBar method after adding other theme management options
themesMenu.DropDownItems.Add(CreateMenuItem("Open Themes Folder", 'O', (s, e) => OpenThemesFolder()));
    // Add reset option after Export option
    themesMenu.DropDownItems.Add(new ToolStripSeparator());
    themesMenu.DropDownItems.Add(CreateMenuItem("Reset to Default Themes", 'R', (s, e) => ResetToDefaultThemes()));
    // Add the themes menu to the terminal menu
    terminalMenu.DropDownItems.Add(new ToolStripSeparator());
    terminalMenu.DropDownItems.Add(themesMenu);
    
    // Add menus to strip
    menuStrip.Items.Add(fileMenu);
    // menuStrip.Items.Add(editMenu);
    menuStrip.Items.Add(terminalMenu);
    
// In your CreateDOSMenuBar method, add this after your existing menus
ToolStripMenuItem viewMenu = new ToolStripMenuItem("View");
viewMenu.DropDownItems.Add(CreateMenuItem("Maximize", 'M', (s, e) => MaximizeWindow()));
viewMenu.DropDownItems.Add(CreateMenuItem("Restore", 'R', (s, e) => RestoreWindow()));
// Add the view menu to your menu strip
menuStrip.Items.Add(viewMenu);


ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help");
helpMenu.DropDownItems.Add(CreateMenuItem("About", 'A', (s, e) => ShowAboutDialog()));
menuStrip.Items.Add(helpMenu);

    // Add the menu strip to the panel
    menuPanel.Controls.Add(menuStrip);
    
    // Add the panel to the form
    this.Controls.Add(menuPanel);
    this.MainMenuStrip = menuStrip;
    
    // Make the entire menu panel draggable
    menuPanel.MouseDown += (s, e) => 
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }
    };
    
    // Also make the menu strip itself draggable for areas not occupied by menu items
    menuStrip.MouseDown += (s, e) => 
    {
        if (e.Button == MouseButtons.Left)
        {
            // Check if clicking on the menu items area
            ToolStripItem clickedItem = menuStrip.GetItemAt(e.Location);
            
            // If not clicking directly on a menu item, allow dragging
            if (clickedItem == null)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    };
}

private void ShowAboutDialog()
{
    using (AboutForm aboutForm = new AboutForm())
    {
        aboutForm.ShowDialog();
    }
}

private void MaximizeWindow()
{
    if (this.WindowState != FormWindowState.Maximized)
    {
        this.WindowState = FormWindowState.Maximized;
        UpdateStatus("Window maximized");
    }
}

private void RestoreWindow()
{
    if (this.WindowState != FormWindowState.Normal)
    {
        this.WindowState = FormWindowState.Normal;
        UpdateStatus("Window restored");
    }
}
private void ToggleModemSound()
{
    enableModemSound = !enableModemSound;
    
    // Update the menu item checkmark
    foreach (ToolStripItem item in this.MainMenuStrip.Items)
    {
        if (item is ToolStripMenuItem menuItem && menuItem.Text == "Terminal")
        {
            foreach (ToolStripItem subItem in menuItem.DropDownItems)
            {
                if (subItem is ToolStripMenuItem subMenuItem && subMenuItem.Text.Contains("Modem Sound"))
                {
                    subMenuItem.Checked = enableModemSound;
                    break;
                }
            }
            break;
        }
    }
    
    // Show status update
    UpdateStatus(enableModemSound ? "Modem sound enabled" : "Modem sound disabled");
}


private void OpenThemesFolder()
{
    try
    {
        // Get the themes directory path
        string themesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SSHTerminal", "Themes");
            
        // Ensure the directory exists
        if (!Directory.Exists(themesDirectory))
        {
            Directory.CreateDirectory(themesDirectory);
        }
        
        // Open the folder in File Explorer
        Process.Start("explorer.exe", themesDirectory);
        
        UpdateStatus($"Opened themes folder: {themesDirectory}");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error opening themes folder: {ex.Message}", 
            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        UpdateStatus("Failed to open themes folder");
    }
}

private void ToggleTerminalFont()
{
    useRetroFont = !useRetroFont;
    
    // Update the menu item checkmark
    foreach (ToolStripItem item in this.MainMenuStrip.Items)
    {
        if (item is ToolStripMenuItem menuItem && menuItem.Text == "Terminal")
        {
            foreach (ToolStripItem subItem in menuItem.DropDownItems)
            {
                if (subItem is ToolStripMenuItem subMenuItem && subMenuItem.Text.Contains("Toggle Retro Font"))
                {
                    subMenuItem.Checked = useRetroFont;
                    break;
                }
            }
            break;
        }
    }
    
    // Apply the font change using existing terminal methods
    ApplyFontChange();
    
    // Show status update
    UpdateStatus(useRetroFont ? "Retro font enabled" : "Modern font enabled");
}

private async void ApplyFontChange()
{
    if (terminal == null) return;
    
    try
    {
        // Use terminal's ExecuteScriptAsync method which should be available
        string script = useRetroFont 
            ? "term.setOption('fontFamily', '\"VT323\", \"Perfect DOS VGA 437\", monospace'); term.setOption('fontSize', 20); fitTerminal();"
            : "term.setOption('fontFamily', '\"Consolas\", \"Courier New\", monospace'); term.setOption('fontSize', 18); fitTerminal();";
        
        // Execute the script if terminal has the method
        if (terminal.WebViewInitialized)
        {
            // This assumes terminal has an ExecuteScriptAsync method
            // If it doesn't, you'll need to add it to SshTerminalControl
            await terminal.ExecuteScriptAsync(script);
        }
    }
    catch (Exception ex)
    {
        UpdateStatus($"Error changing font: {ex.Message}");
    }
}



private void ExportCurrentTheme()
{
    if (themeManager.CurrentTheme == null)
    {
        MessageBox.Show("No theme is currently active", 
            "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }
    
    using (SaveFileDialog dialog = new SaveFileDialog())
    {
        dialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
        dialog.Title = "Export Current Theme";
        dialog.FileName = $"{themeManager.CurrentTheme.Name.Replace(" ", "_")}.json";
        
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                themeManager.ExportTheme(themeManager.CurrentTheme.Name, dialog.FileName);
                UpdateStatus($"Theme '{themeManager.CurrentTheme.Name}' exported to {dialog.FileName}");
                MessageBox.Show($"Theme exported successfully to {dialog.FileName}", 
                    "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting theme: {ex.Message}", 
                    "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"Theme export failed: {ex.Message}");
            }
        }
    }
}

// Add these methods to handle theme import/export
private void ImportTheme()
{
    using (OpenFileDialog dialog = new OpenFileDialog())
    {
        dialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
        dialog.Title = "Import Theme";
        
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                Theme theme = themeManager.ImportTheme(dialog.FileName);
                UpdateStatus($"Theme '{theme.Name}' imported successfully");
                
                // Ask if user wants to apply the imported theme
                if (MessageBox.Show($"Do you want to apply the imported theme '{theme.Name}'?", 
                    "Apply Theme", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    themeManager.ApplyTheme(theme.Name);
                }
                
                // Refresh themes menu to show the newly imported theme
                RefreshThemesMenu();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing theme: {ex.Message}", 
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"Theme import failed: {ex.Message}");
            }
        }
    }
}


private void ShowConnectionDirectory()
{
    using (ConnectionDirectoryForm directoryForm = new ConnectionDirectoryForm(themeManager))
    {
        if (directoryForm.ShowDialog() == DialogResult.OK && directoryForm.SelectedProfile != null)
        {
            // Fill the connection fields with the selected profile
            hostTextBox.Text = directoryForm.SelectedProfile.Host;
            portTextBox.Text = directoryForm.SelectedProfile.Port.ToString();
            usernameTextBox.Text = directoryForm.SelectedProfile.Username;
            passwordTextBox.Text = directoryForm.SelectedProfile.EncryptedPassword;
            

        }
    }
}
    private ToolStripMenuItem CreateMenuItem(string text, char hotkey, EventHandler clickHandler)
    {
        // Find the position of the hotkey in the text
        int hotkeyPos = text.IndexOf(hotkey, StringComparison.OrdinalIgnoreCase);
        
        ToolStripMenuItem menuItem = new ToolStripMenuItem(text);
        menuItem.Click += clickHandler;
        
        // Store the hotkey position for rendering
        menuItem.Tag = hotkeyPos >= 0 ? hotkeyPos : -1;
        
        return menuItem;
    }

private Panel CreateFunctionKeyBar()
{
    Panel functionKeyBar = new Panel
    {
        Dock = DockStyle.Bottom,
        Height = 45,
        BackColor = Theme.HexToColor(themeManager.CurrentTheme?.UI.MenuBackground ?? "#C0C0C0"),
    ForeColor = Theme.HexToColor(themeManager.CurrentTheme?.UI.MenuText ?? "#000000"),
        Padding = new Padding(5, 0, 4, 5)
    };
    
    // Function key definitions in Turbo C style
    (string Key, string Description)[] functionKeys = {
        ("F1", "Help "),
        ("F2", "Save "),
        ("F4", "Directory "),
        ("F9", "Quit "),
        ("F10", "Menu ")
    };
    
    // Use FlowLayoutPanel for automatic spacing
    FlowLayoutPanel keyFlow = new FlowLayoutPanel
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        AutoScroll = false,
        BackColor = Color.Transparent,
        Padding = new Padding(4, 0, 4, 0)
    };
    
    // Add function key labels
    foreach (var (key, description) in functionKeys)
    {
        Label keyLabel = new Label
        {
            AutoSize = true,
            Margin = new Padding(8, 3, 15, 3),
            Text = $"{key}-{description}", // Text is set but will be rendered in the Paint event
            Font = dosFont,
            BackColor = Color.Transparent
        };
        
        // Custom paint for Turbo C style with F-key in red, description in black

keyLabel.Paint += (s, e) =>
{
    // Get background color from theme
    Color bgColor = Theme.HexToColor(themeManager.CurrentTheme?.UI.MenuBackground ?? "#C0C0C0");
    e.Graphics.Clear(bgColor);
    
    // Use theme colors for function keys and descriptions
    Color functionKeyColor = Theme.HexToColor(themeManager.CurrentTheme?.UI.FunctionKeyText ?? "#FF0000");
    Color descriptionColor = Theme.HexToColor(themeManager.CurrentTheme?.UI.FunctionKeyDescriptionText ?? "#000000");
    
    // Draw the function key part with theme color
    using (Brush keyBrush = new SolidBrush(functionKeyColor))
    {
        e.Graphics.DrawString(key, dosFont, keyBrush, 0, 0);
    }
    
    // Get key width to position description
    SizeF keySize = e.Graphics.MeasureString(key, dosFont);
    
    // Draw the hyphen and description with theme color
    using (Brush descBrush = new SolidBrush(descriptionColor))
    {
        e.Graphics.DrawString($"-{description}", dosFont, descBrush, keySize.Width, 0);
    }
};
                // Add click handlers for the function key labels
    if (key == "F4")
    {
        keyLabel.Click += (s, e) => ShowConnectionDirectory();
        keyLabel.Cursor = Cursors.Hand;
    }
    else if (key == "F9")
    {
        keyLabel.Click += (s, e) => HandleQuit();
        keyLabel.Cursor = Cursors.Hand;
    }
        
        keyFlow.Controls.Add(keyLabel);
    }
    
    functionKeyBar.Controls.Add(keyFlow);
    
    // Create the resize grip for the bottom-right corner
    resizeGrip = new Panel
    {
        Size = new Size(16, 16),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        Cursor = Cursors.SizeNWSE,
        BackColor = Color.Transparent,
        Margin = new Padding(0),
        Padding = new Padding(0)
    };
    
    // Paint handler for resize grip
resizeGrip.Paint += (s, e) =>
{
    Color lineColor = Theme.HexToColor(themeManager.CurrentTheme?.UI.Border ?? 
        (isDarkMode ? "#000000" : "#000080"));
    
    // Draw diagonal lines for the resize grip effect
    using (Pen pen = new Pen(lineColor, 1))
    {
        // Draw 3 diagonal lines
        for (int i = 1; i <= 3; i++)
        {
            int offset = i * 4;
            e.Graphics.DrawLine(pen, 
                resizeGrip.Width - offset, resizeGrip.Height, 
                resizeGrip.Width, resizeGrip.Height - offset);
        }
    }
};

    // Handle mouse events for resizing
    resizeGrip.MouseDown += (s, e) =>
    {
        if (e.Button == MouseButtons.Left)
        {
            // Use Windows API to initiate resize from bottom-right
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xF008, 0);
        }
    };
    
    // Add the grip to the function key bar and ensure it's on top
    functionKeyBar.Controls.Add(resizeGrip);
    resizeGrip.BringToFront();
    
    // Position the grip correctly accounting for padding
    void PositionGrip()
    {
        int rightPosition = functionKeyBar.ClientSize.Width - resizeGrip.Width - functionKeyBar.Padding.Right;
        int bottomPosition = functionKeyBar.ClientSize.Height - resizeGrip.Height - functionKeyBar.Padding.Bottom;
        resizeGrip.Location = new Point(rightPosition, bottomPosition);
    }
    
    // Set initial position
    functionKeyBar.HandleCreated += (s, e) => PositionGrip();
    
    // Update position on resize
    functionKeyBar.Resize += (s, e) => PositionGrip();
    
    // Force layout to position grip as soon as the panel is ready
    functionKeyBar.Layout += (s, e) => PositionGrip();
    
    return functionKeyBar;
}


protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
{
    // Check if it's F4 or F9
    if (keyData == Keys.F4)
    {
        ShowConnectionDirectory();
        return true;
    }
    else if (keyData == Keys.F2)
    {      MessageBox.Show("F2 key pressed! Will now save settings.", 
                        "F2 Key Event", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
         if (SaveSettings())
        {
            UpdateStatus("F2: Settings Saved");
                Application.DoEvents();

            return true;
        }
        else
        {
            UpdateStatus("Save Settings Failed");
            return false;
        }
        return false;
    }
    else if (keyData == Keys.F9)
    {
        HandleQuit();
        return true;
    }
    
    // Handle Ctrl+ and Ctrl- for font sizing (keep your existing logic)
    if (keyData == (Keys.Control | Keys.Add) || keyData == (Keys.Control | Keys.Oemplus))
    {
        IncreaseFontSize();
        return true;
    }
    else if (keyData == (Keys.Control | Keys.Subtract) || keyData == (Keys.Control | Keys.OemMinus))
    {
        DecreaseFontSize();
        return true;
    }
    
    return base.ProcessCmdKey(ref msg, keyData);
}

private void HandleQuit()
{
    // Check if currently connected
    if (terminal.IsConnected)
    {
        // Show confirmation dialog
        DialogResult result = MessageBox.Show(
            "You are currently connected. Are you sure you want to quit?",
            "Confirm Exit",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2); // Default to "No"
            
        if (result == DialogResult.Yes)
        {
            this.Close();
        }
    }
    else
    {
        // Not connected, just close
        this.Close();
    }
}

    private void SetupEvents()
    {
        // Terminal events
        terminal.ConnectionStateChanged += Terminal_ConnectionStateChanged;
        terminal.TerminalError += Terminal_TerminalError;
        terminal.TerminalResized += Terminal_TerminalResized;
        
        // Add mouse wheel event for font scaling
        terminal.MouseWheel += Terminal_MouseWheel;
        
        // Control events
        connectButton.Click += ConnectButton_Click;
        darkModeCheckbox.CheckedChanged += DarkModeCheckbox_CheckedChanged;
        
        // Form events for custom window handling
        this.Paint += MainForm_Paint;
        this.MouseDown += MainForm_MouseDown;
        
        // Load event to set initial size/position
        this.Load += (s, e) => {
            // Set focus to host textbox
            hostTextBox.Focus();
        };
    }
    
    // Font size adjustment methods
    private void IncreaseFontSize()
    {
        if (terminal.TerminalSettings.FontSize < 24) // Max font size
        {
            terminal.SetFontSize(terminal.TerminalSettings.FontSize + 1);
            UpdateStatus($"Font size increased to {terminal.TerminalSettings.FontSize}");
        }
    }

    private void DecreaseFontSize()
    {
        if (terminal.TerminalSettings.FontSize > 8) // Min font size
        {
            terminal.SetFontSize(terminal.TerminalSettings.FontSize - 1);
            UpdateStatus($"Font size decreased to {terminal.TerminalSettings.FontSize}");
        }
    }
    
    // Mouse wheel handler for font scaling
    private void Terminal_MouseWheel(object sender, MouseEventArgs e)
    {
        // Check if Ctrl key is pressed during mouse wheel
        if (ModifierKeys == Keys.Control)
        {
            if (e.Delta > 0)
            {
                IncreaseFontSize();
            }
            else if (e.Delta < 0)
            {
                DecreaseFontSize();
            }
        }
    }
    
    
    private void Terminal_TerminalError(object sender, SshTerminalErrorEventArgs e)
    {
        UpdateStatus($"Error: {e.Message}");
        MessageBox.Show(e.Message + (e.Exception != null ? $"\n\n{e.Exception.Message}" : ""),
            "Terminal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void Terminal_TerminalResized(object sender, TerminalResizeEventArgs e)
    {
        // UpdateStatus($"Terminal resized to {e.Columns}x{e.Rows}");
    }

    // Apply theme changes - significantly improved
    private void ApplyTheme(bool darkMode)
    {
        // Find the appropriate theme based on dark/light mode
    string themeName = darkMode ? "Borland Classic" : "Classic Light";
    
    // If the specific theme isn't found, find any theme matching the dark/light mode
    if (!themeManager.GetAvailableThemeNames().Contains(themeName))
    {
        // Find first theme matching the requested mode
        var theme = themeManager.AvailableThemes.FirstOrDefault(t => t.IsDarkTheme == darkMode);
        if (theme != null)
        {
            themeName = theme.Name;
        }
        else
        {
            // If no matching theme found, just use the current theme
            return;
        }
    }
    
    // Apply the theme
    themeManager.ApplyTheme(themeName);
    
    // isDarkMode is set inside ApplyCurrentTheme, which is called by the ThemeChanged event
    }

    // Add these methods to support the Themes menu
private void AddThemesMenu()
{
    // Add a Themes submenu to the Terminal menu
    ToolStripMenuItem themesMenu = new ToolStripMenuItem("Themes");
    
    // Add menu items for each available theme
    foreach (string themeName in themeManager.GetAvailableThemeNames())
    {
        ToolStripMenuItem themeItem = new ToolStripMenuItem(themeName);
        themeItem.Click += (s, e) => themeManager.ApplyTheme(themeName);
        themesMenu.DropDownItems.Add(themeItem);
    }
    
    // Add import/export options
    themesMenu.DropDownItems.Add(new ToolStripSeparator());
    
    ToolStripMenuItem importItem = new ToolStripMenuItem("Import Theme...");
    importItem.Click += ImportTheme_Click;
    themesMenu.DropDownItems.Add(importItem);
    
    ToolStripMenuItem exportItem = new ToolStripMenuItem("Export Current Theme...");
    exportItem.Click += ExportTheme_Click;
    themesMenu.DropDownItems.Add(exportItem);
    
    // Find Terminal menu and add Themes submenu
    if (this.MainMenuStrip != null)
    {
        foreach (ToolStripItem item in this.MainMenuStrip.Items)
        {
            if (item is ToolStripMenuItem menuItem && menuItem.Text == "Terminal")
            {
                menuItem.DropDownItems.Add(new ToolStripSeparator());
                menuItem.DropDownItems.Add(themesMenu);
                break;
            }
        }
    }
}

private void ImportTheme_Click(object sender, EventArgs e)
{
    using (OpenFileDialog dialog = new OpenFileDialog())
    {
        dialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
        dialog.Title = "Import Theme";
        
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var theme = themeManager.ImportTheme(dialog.FileName);
                MessageBox.Show($"Theme '{theme.Name}' imported successfully.", 
                    "Import Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Refresh the themes menu
                RefreshThemesMenu();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing theme: {ex.Message}", 
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

// Method to refresh the themes menu when themes are added/removed
private void RefreshThemesMenu()
{
    // Find the Terminal menu
    ToolStripItem terminalMenu = this.MainMenuStrip.Items.Cast<ToolStripItem>()
        .FirstOrDefault(item => item is ToolStripMenuItem && item.Text == "Terminal");
    
    if (terminalMenu == null) return;
    
    // Find the Themes submenu
    ToolStripMenuItem themesMenu = ((ToolStripMenuItem)terminalMenu).DropDownItems.Cast<ToolStripItem>()
        .FirstOrDefault(item => item is ToolStripMenuItem && item.Text == "Themes") as ToolStripMenuItem;
    
    if (themesMenu == null) return;
    
    // Clear all theme items except for the built-in ones and management options
    List<ToolStripItem> itemsToKeep = new List<ToolStripItem>();
    
    // Keep built-in themes
    foreach (ToolStripItem item in themesMenu.DropDownItems)
    {
        if (item.Text == "Borland Classic" || item.Text == "Light Theme" || 
            item.Text == "Import Theme..." || item.Text == "Export Current Theme..." ||
            item is ToolStripSeparator)
        {
            itemsToKeep.Add(item);
        }
    }
    
    // Clear all items
    themesMenu.DropDownItems.Clear();
    
    // Add back the built-in themes
    foreach (var item in itemsToKeep.Where(i => i.Text == "Borland Classic" || i.Text == "Light Theme"))
    {
        themesMenu.DropDownItems.Add(item);
    }
    
    // Add separator if we have built-in themes
    if (themesMenu.DropDownItems.Count > 0)
    {
        themesMenu.DropDownItems.Add(new ToolStripSeparator());
    }
    
    // Add custom themes from theme manager
    var themeNames = themeManager.GetAvailableThemeNames()
        .Where(name => name != "Borland Classic" && name != "Light Theme");
    
    foreach (var themeName in themeNames)
    {
        ToolStripMenuItem themeItem = new ToolStripMenuItem(themeName);
        themeItem.Click += (s, e) => themeManager.ApplyTheme(themeName);
        themesMenu.DropDownItems.Add(themeItem);
    }
    
    // Add separator and management options
    themesMenu.DropDownItems.Add(new ToolStripSeparator());
    
    // Add back the management options
    foreach (var item in itemsToKeep.Where(i => i.Text == "Import Theme..." || i.Text == "Export Current Theme..."))
    {
        themesMenu.DropDownItems.Add(item);
    }
}


private void ExportTheme_Click(object sender, EventArgs e)
{
    using (SaveFileDialog dialog = new SaveFileDialog())
    {
        dialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
        dialog.Title = "Export Theme";
        dialog.FileName = themeManager.CurrentTheme.Name.Replace(" ", "_") + ".json";
        
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                themeManager.ExportTheme(themeManager.CurrentTheme.Name, dialog.FileName);
                MessageBox.Show($"Theme exported successfully to {dialog.FileName}.", 
                    "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting theme: {ex.Message}", 
                    "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}



    private void DarkModeCheckbox_CheckedChanged(object sender, EventArgs e)
    {
        ApplyTheme(darkModeCheckbox.Checked);
    }
    
    private async void ConnectButton_Click(object sender, EventArgs e)
    {
        try
        {
            if (terminal.IsConnected)
            {
                terminal.Disconnect();
            }
            else
            {
                string host = hostTextBox.Text.Trim();
                if (string.IsNullOrEmpty(host))
                {
                    MessageBox.Show("Please enter a hostname or IP address", "Connection Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int port;
                if (!int.TryParse(portTextBox.Text, out port) || port < 1 || port > 65535)
                {
                    MessageBox.Show("Please enter a valid port number (1-65535)", "Connection Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string username = usernameTextBox.Text.Trim();
                if (string.IsNullOrEmpty(username))
                {
                    MessageBox.Show("Please enter a username", "Connection Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                connectButton.Enabled = false;
                connectButton.Text = "CONNECTING...";
                UpdateStatus($"Connecting to {host}:{port}...");
                await Task.Delay(50); // Small delay to update UI
                await PlayConnectionSound();


                string password = passwordTextBox.Text;
                try
                {
                    await terminal.ConnectAsync(host, port, username, password);
                }
                catch (Exception connectEx)
                {
                    // Show detailed exception information
                    string errorMessage = $"Connection error: {connectEx.Message}";
                    if (connectEx.InnerException != null)
                    {
                        errorMessage += $"\n\nInner Exception: {connectEx.InnerException.Message}";
                    }
                    
                    MessageBox.Show(errorMessage, "Connection Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                    connectButton.Text = "CONNECT";
                    connectButton.Enabled = true;
                    UpdateStatus("Connection failed: " + connectEx.Message);
                }
            }
        }
        catch (Exception ex)
        {
            // Show detailed exception information
            string errorMessage = $"Error: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
            }
            
            MessageBox.Show(errorMessage, "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            connectButton.Text = "CONNECT";
            connectButton.Enabled = true;
            UpdateStatus("Operation failed: " + ex.Message);
        }
    }
    
    // Custom form border and title bar

    private void MainForm_Paint(object sender, PaintEventArgs e)
{
    // Get theme colors from current theme
    var theme = themeManager.CurrentTheme;
    if (theme == null) return;
    
    Color titleBarColor = Theme.HexToColor(theme.UI.Background);
    Color textColor = Theme.HexToColor(theme.UI.Text);
    Color borderColor = Theme.HexToColor(theme.UI.Border);
    
    // Draw custom title bar
    Rectangle titleRect = new Rectangle(0, 0, this.Width, 22);
    using (SolidBrush titleBrush = new SolidBrush(titleBarColor))
    {
        e.Graphics.FillRectangle(titleBrush, titleRect);
    }
    
    // Draw title text
    using (SolidBrush textBrush = new SolidBrush(textColor))
    {
        e.Graphics.DrawString(this.Text.ToUpper(), dosFont, textBrush, new Point(4, 3));
    }
    
    // Draw close button
    Rectangle closeRect = new Rectangle(this.Width - 22, 1, 20, 20);
    using (Pen borderPen = new Pen(borderColor))
    {
        e.Graphics.DrawRectangle(borderPen, closeRect);
        e.Graphics.DrawLine(borderPen, closeRect.Left + 4, closeRect.Top + 4, 
            closeRect.Right - 4, closeRect.Bottom - 4);
        e.Graphics.DrawLine(borderPen, closeRect.Right - 4, closeRect.Top + 4, 
            closeRect.Left + 4, closeRect.Bottom - 4);
    }
    
    // Draw form border
    Rectangle borderRect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
    using (Pen borderPen = new Pen(borderColor))
    {
        e.Graphics.DrawRectangle(borderPen, borderRect);
    }
}
// Allow dragging the window by the title bar
[DllImport("user32.dll")]
public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
[DllImport("user32.dll")]
public static extern bool ReleaseCapture();

private const int WM_NCLBUTTONDOWN = 0xA1;
private const int HT_CAPTION = 0x2;

private void MainForm_MouseDown(object sender, MouseEventArgs e)
{
    if (e.Button == MouseButtons.Left)
    {
        // Check if click is in the title bar area
        if (e.Y <= 22)
        {
            // If clicking close button
            if (e.X >= this.Width - 22 && e.X <= this.Width - 2 && e.Y >= 1 && e.Y <= 21)
            {
                this.Close();
                return;
            }
            
            // Otherwise allow dragging
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }
    }
}

private void UpdateStatus(string message)
{
    if (this.InvokeRequired)
    {
        this.BeginInvoke(new Action(() => UpdateStatus(message)));
        return;
    }

    // Find the status label in the panel's controls
    foreach (Control control in this.Controls)
    {
        if (control is TableLayoutPanel mainLayout)
        {
            foreach (Control layoutControl in mainLayout.Controls)
            {
                if (layoutControl is Panel panel && panel.BackColor == BorlandCyan)
                {
                    foreach (Control panelControl in panel.Controls)
                    {
                        if (panelControl is Label label && (string)label.Tag == "statusTextLabel")
                        {
                            label.Text = message;
                            return;
                        }
                    }
                    
                    // If no label found with the tag, try the first label
                    foreach (Control panelControl in panel.Controls)
                    {
                        if (panelControl is Label label)
                        {
                            label.Text = message;
                            return;
                        }
                    }
                }
            }
        }
    }
    
    // Fallback if we can't find the label
    statusLabel.Text = message;
}

protected override void OnFormClosed(FormClosedEventArgs e)
{
    if (terminal != null)
    {
        if (terminal.IsConnected)
        {
            terminal.Disconnect();
        }
        terminal.Dispose();
    }
    
    base.OnFormClosed(e);
}

// Custom renderer for DOS-style menus with theme support
// Update DOSMenuRenderer to support theme colors:
private class DOSMenuRenderer : ToolStripProfessionalRenderer
{
    public bool IsDarkMode { get; set; }
    private Theme currentTheme;
    
    public DOSMenuRenderer(bool isDarkMode = true)
    {
        this.IsDarkMode = isDarkMode;
    }
    
    public void UpdateThemeColors(Theme theme)
    {
        currentTheme = theme;
    }
    
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (currentTheme == null)
        {
            base.OnRenderMenuItemBackground(e);
            return;
        }
        
        // For main menu items
        if (e.Item.Owner is MenuStrip)
        {
            if (e.Item.Selected)
            {
                // Selected main menu items
                using (SolidBrush brush = new SolidBrush(Theme.HexToColor(currentTheme.UI.MenuHighlight)))
                {
                    e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                }
                e.Item.ForeColor = Theme.HexToColor(currentTheme.UI.MenuHighlightText);
            }
            else
            {
                // Non-selected main menu items
                using (SolidBrush brush = new SolidBrush(Theme.HexToColor(currentTheme.UI.MenuBackground)))
                {
                    e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                }
                e.Item.ForeColor = Theme.HexToColor(currentTheme.UI.MenuText);
            }
            return;
        }
        
        // For dropdown menu items
        if (e.Item.Selected)
        {
            // Selected dropdown items
            using (SolidBrush brush = new SolidBrush(Theme.HexToColor(currentTheme.UI.MenuHighlight)))
            {
                e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
            }
            e.Item.ForeColor = Theme.HexToColor(currentTheme.UI.MenuHighlightText);
        }
        else
        {
            // Non-selected dropdown items
            using (SolidBrush brush = new SolidBrush(Theme.HexToColor(currentTheme.UI.MenuBackground)))
            {
                e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
            }
            e.Item.ForeColor = Theme.HexToColor(currentTheme.UI.MenuText);
        }
    }
    
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (currentTheme == null || e.Item.Tag == null || !(e.Item.Tag is int) || (int)e.Item.Tag < 0)
        {
            base.OnRenderItemText(e);
            return;
        }
        
        // Get the hotkey position
        int hotkeyPos = (int)e.Item.Tag;
        if (hotkeyPos >= e.Text.Length)
        {
            base.OnRenderItemText(e);
            return;
        }
        
        // Get colors from theme
        Color textColor = e.Item.Selected ? 
            Theme.HexToColor(currentTheme.UI.MenuHighlightText) : 
            Theme.HexToColor(currentTheme.UI.MenuText);
            
        Color hotkeyColor = Theme.HexToColor(currentTheme.UI.HotkeyText); // Always use hotkey color
        
        // Split the text for rendering
        string prefix = e.Text.Substring(0, hotkeyPos);
        string hotkey = e.Text.Substring(hotkeyPos, 1);
        string suffix = e.Text.Substring(hotkeyPos + 1);
        
        float x = e.TextRectangle.X;
        float y = e.TextRectangle.Y + (e.TextRectangle.Height - e.TextFont.Height) / 2;
        
        using (StringFormat format = StringFormat.GenericTypographic)
        {
            using (SolidBrush textBrush = new SolidBrush(textColor))
            using (SolidBrush hotkeyBrush = new SolidBrush(hotkeyColor))
            {
                // Draw the prefix
                if (!string.IsNullOrEmpty(prefix))
                {
                    e.Graphics.DrawString(prefix, e.TextFont, textBrush, x, y, format);
                    x += e.Graphics.MeasureString(prefix, e.TextFont, e.TextRectangle.Width, format).Width;
                }
                
                // Draw the hotkey character
                e.Graphics.DrawString(hotkey, e.TextFont, hotkeyBrush, x, y, format);
                x += e.Graphics.MeasureString(hotkey, e.TextFont, e.TextRectangle.Width, format).Width;
                
                // Draw the suffix
                if (!string.IsNullOrEmpty(suffix))
                {
                    e.Graphics.DrawString(suffix, e.TextFont, textBrush, x, y, format);
                }
            }
        }
        
        // Indicate that we've handled rendering
        e.TextRectangle = Rectangle.Empty;
    }
}
}


 