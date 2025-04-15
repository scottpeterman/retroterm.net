using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using RetroTerm.NET.Controls;
using RetroTerm.NET.Forms;
using RetroTerm.NET.Models;
using RetroTerm.NET.Services;
using SshTerminalComponent;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Reflection;

using Microsoft.Win32;
// Win32 API declarations


namespace RetroTerm.NET
{
    
    public partial class MainForm : Form
    {
        // Managers
        private ThemeManager themeManager;
        private TabManager tabManager;
        private SettingsManager settingsManager;

        // UI Components
        private MenuStrip menuStrip;
        private TabControl tabControl;
        private Button closeButton;
        private Panel statusPanel;
        private Label statusLabel;
        private Font dosFont;


        // private Panel navigatorPanel;
        // private Panel navigationSidePanel; 
        private Panel navigatorPanel;
        private ToolStripMenuItem toggleNavigatorMenuItem;
        private TableLayoutPanel horizontalSplitLayout;

        private int savedNavigatorWidth = 200;

         // Color constants
        // Replace the static constants with properties
        private Color BorlandBlue => Theme.HexToColor(themeManager?.CurrentTheme?.UI.Background ?? "#0000AA");
        private Color BorlandCyan => Theme.HexToColor(themeManager?.CurrentTheme?.UI.ButtonBackground ?? "#00AAAA");
        private Color BorlandDarkBlue => Theme.HexToColor(themeManager?.CurrentTheme?.UI.MenuHighlight ?? "#000080");
                
        // Win32 API for dragging the window
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", SetLastError = true)]
private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        
        // Add this to your class's DllImport declarations
[DllImport("user32.dll")]
private static extern int GetSystemMetrics(int nIndex);

[DllImport("user32.dll")]
private static extern int SystemParametersInfo(int nAction, int nParam, ref int value, int ignore);

// Add these constants
private const int SM_CXVSCROLL = 2;           // Get current vertical scrollbar width

[DllImport("user32.dll")]
private static extern int ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

// Constants for scroll bar types
private const int SB_HORZ = 0;
private const int SB_VERT = 1;
private const int SB_BOTH = 3;
// Constants
private const int SPI_SETSCROLLWIDTH = 2114;  // Set scrollbar width
private const int SPI_SETVSCROLLWIDTH = 2122; // Set vertical scrollbar width

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        
        // Registry key for settings
        private const string RegistryKey = @"SOFTWARE\RetroTerm.NET";
        private Splitter navigatorSplitter;

        public MainForm()
        {
            // Application.EnableVisualStyles();
            // Application.SetCompatibleTextRenderingDefault(false);
            // Initialize DOS font
            InitializeDOSFont();

            // Set form style before initialization
            this.FormBorderStyle = FormBorderStyle.None;
            
            // Initialize theme manager
            string themesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RetroTerm.NET", "Themes");
                
            themeManager = new ThemeManager(themesDirectory);
            
            // Initialize settings manager
            settingsManager = new SettingsManager();
            
            // Initialize form components - this now contains all the UI setup
            InitializeComponent();
            // SetNarrowScrollbars();
            // HideScrollbarsRecursively(this);

    StyleTreeViewAdvScrollbars();

            // InitializePasswordManager();

            // Create tab manager
            tabManager = new TabManager(tabControl, themeManager);
            
            // Subscribe to the connection state changed event
            tabManager.ConnectionStateChanged += TabManager_ConnectionStateChanged;

            // Hook up events - do this after creating managers
            themeManager.ThemeChanged += ThemeManager_ThemeChanged;
            tabManager.TabAdded += TabManager_TabAdded;
            tabManager.TabRemoved += TabManager_TabRemoved;
            tabManager.TabSelected += TabManager_TabSelected;
            
            // Add menu items to menuStrip
            PopulateMenuItems();
            
            // Create initial tab
            // tabManager.CreateNewTab("New Terminal");
            
            // Load settings
            LoadSettings();
            
            // Apply current theme
            ApplyCurrentTheme();
        }
        
        // Method to set narrow scrollbars// Method to set narrow scrollbars



private void HideScrollbarsRecursively(Control control)
{
    foreach (Control childControl in control.Controls)
    {
        try
        {
            // Special handling for TreeView controls
            if (childControl is TreeView treeView || 
                childControl.GetType().Name.Contains("TreeView") ||
                childControl.Name.Contains("sessionTree"))
            {
                Console.WriteLine($"Processing TreeView control: {childControl.Name}");
                
                // Try multiple approaches to hide scrollbars
                ShowScrollBar(childControl.Handle, SB_BOTH, false);

                // If it's a standard TreeView
                if (childControl is TreeView standardTreeView)
                {
                    // Set ShowScrolls property if available
                    PropertyInfo showScrollsProperty = standardTreeView.GetType().GetProperty("ShowScrolls", 
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (showScrollsProperty != null)
                    {
                        showScrollsProperty.SetValue(standardTreeView, false);
                    }
                    
                    // Try to set scroll properties using reflection
                    PropertyInfo scrollableProperty = standardTreeView.GetType().GetProperty("Scrollable");
                    if (scrollableProperty != null)
                    {
                        scrollableProperty.SetValue(standardTreeView, false);
                    }
                }
                
                // For any type of TreeView (standard or custom)
                try {
                    // Try to access properties via reflection for third-party controls
                    PropertyInfo[] properties = childControl.GetType().GetProperties();
                    foreach (PropertyInfo prop in properties)
                    {
                        // Look for properties related to scrollbars
                        if (prop.Name.Contains("Scroll") || prop.Name.Contains("scroll"))
                        {
                            if (prop.PropertyType == typeof(bool))
                            {
                                Console.WriteLine($"Setting {prop.Name} to false");
                                prop.SetValue(childControl, false);
                            }
                            else if (prop.PropertyType == typeof(int))
                            {
                                Console.WriteLine($"Setting {prop.Name} to 0");
                                prop.SetValue(childControl, 0);
                            }
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error accessing properties: {ex.Message}");
                }
            }
            else
            {
                // For other control types, just use ShowScrollBar
                ShowScrollBar(childControl.Handle, SB_BOTH, false);
            }
            
            // Recursively process child controls
            if (childControl.Controls.Count > 0)
            {
                HideScrollbarsRecursively(childControl);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error hiding scrollbars for {childControl.Name}: {ex.Message}");
        }
    }
}

private void SetNarrowScrollbars()
{
    try
    {
        // Get current system scrollbar width as a reference
        int currentWidth = GetSystemMetrics(SM_CXVSCROLL);
        Console.WriteLine($"Current system scrollbar width: {currentWidth}px");
        
        // Set vertical scrollbar width to a smaller value (e.g., 8 pixels)
        // Note: Going below 8 can make scrollbars hard to use
        int width = 8;
        int result = SystemParametersInfo(SPI_SETVSCROLLWIDTH, width, ref width, 0);
        Console.WriteLine($"Set scrollbar width result: {result}");
        
        // Apply themed scrollbars to all controls recursively
        ApplyThemedScrollbarsRecursively(this);
        
        // Apply changes to the application
        Application.DoEvents();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error setting scrollbar width: {ex.Message}");
    }
}

[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

private void ApplyThemedScrollbarsRecursively(Control control)
{
    foreach (Control child in control.Controls)
    {
        try
        {
            // Apply themed scrollbars to scrollable controls
            if (child is ScrollableControl scrollable)
            {
                // Try to set modern theme for scrollbars
                SetWindowTheme(scrollable.Handle, "Explorer", null);
            }
            
            // Special handling for TreeView and similar controls
            if (child is TreeView || child.GetType().Name.Contains("TreeView"))
            {
                SetWindowTheme(child.Handle, "Explorer", null);
            }
            
            // Handle TabControl as well
            if (child is TabControl)
            {
                SetWindowTheme(child.Handle, "Explorer", null);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error styling control {child.Name}: {ex.Message}");
        }
        
        // Recurse through child controls
        if (child.Controls.Count > 0)
        {
            ApplyThemedScrollbarsRecursively(child);
        }
    }
}

private void StyleTreeViewAdvScrollbars()
{
    // Find all instances of TreeViewAdv in the form
    foreach (Control control in this.Controls.Find("sessionTree", true))
    {
        try
        {
            // Try different approaches based on the API
            
            // 1. Direct property if available
            var propertyInfo = control.GetType().GetProperty("ScrollBarWidth");
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(control, 8);
                Console.WriteLine("Set ScrollBarWidth property");
            }
            
            // 2. Custom drawing options if available
            var drawingOptionsProperty = control.GetType().GetProperty("DrawingOptions");
            if (drawingOptionsProperty != null)
            {
                var drawingOptions = drawingOptionsProperty.GetValue(control);
                var scrollbarWidthProperty = drawingOptions.GetType().GetProperty("ScrollBarWidth");
                if (scrollbarWidthProperty != null)
                {
                    scrollbarWidthProperty.SetValue(drawingOptions, 8);
                    Console.WriteLine("Set DrawingOptions.ScrollBarWidth property");
                }
            }
            
            // 3. Apply theme to control handle
            SetWindowTheme(control.Handle, "Explorer", null);
            
            // Force redraw
            control.Invalidate(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error styling TreeViewAdv: {ex.Message}");
        }
    }
}
        public void UpdateSplitters(Form form, Theme theme)
{
    // Find all splitters in the form
    foreach (Control control in form.Controls.Find("navigatorSplitter", true))
    {
        if (control is Splitter splitter)
        {
            splitter.BackColor = Theme.HexToColor(theme.UI.Border);
        }
    }
}
        private void ApplyThemeToContainer(Control container, Theme theme)
{
    foreach (Control control in container.Controls)
    {
        // Apply theme based on control type
        if (control is Button button)
        {
            button.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
            button.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
            if (button.FlatStyle == FlatStyle.Flat)
                button.FlatAppearance.BorderColor = Theme.HexToColor(theme.UI.Border);
        }
        else if (control is Panel panel)
        {
            panel.BackColor = Theme.HexToColor(theme.UI.Background);
        }
        else if (control is TableLayoutPanel tlPanel)
        {
            tlPanel.BackColor = Theme.HexToColor(theme.UI.Background);
        }
        
        // Recursively apply to child controls
        if (control.Controls.Count > 0)
            ApplyThemeToContainer(control, theme);
    }
}

private void TabManager_ConnectionStateChanged(object sender, SshConnectionEventArgs e)
{
    if (sender is TerminalTabPage tab)
    {
        if (e.IsConnected)
        {
            UpdateStatus($"Connected to {e.Host}:{e.Port}");
        }
        else
        {
            UpdateStatus("Disconnected");
        }
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
    this.Text = "RetroTerm.NET";
    
    // Calculate size based on screen dimensions
    Screen currentScreen = Screen.FromPoint(Cursor.Position);
int width = (int)(currentScreen.WorkingArea.Width * 0.8);
// Calculate 80% of screen height first, then reduce by one-third
int calculatedHeight = (int)(currentScreen.WorkingArea.Height * 0.8);
int height = (int)(calculatedHeight * 0.67); // Reduce by 1/3 (multiply by 2/3)

// Set the form size and position
this.Size = new Size(width, height);
this.StartPosition = FormStartPosition.CenterScreen;
    this.Font = dosFont;
    
    // Create the horizontal split layout for navigator and main content
    TableLayoutPanel horizontalSplitLayout = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Padding = new Padding(0),
        Margin = new Padding(0)
    };
    this.horizontalSplitLayout = horizontalSplitLayout;


    // Set column widths - navigator takes 200px, main content takes the rest
    horizontalSplitLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
    horizontalSplitLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
    

        // ==================
// SESSION NAVIGATOR AREA (Left Side)
// ==================
Panel navigatorPanel = new Panel
{
    Dock = DockStyle.Fill,
    BackColor = BorlandBlue,
    Padding = new Padding(1),
    Margin = new Padding(0),
    Width = 100
};
this.navigatorPanel = navigatorPanel;

SessionNavigator sessionNavigator = new SessionNavigator()
{
    Dock = DockStyle.Fill,
    Margin = new Padding(2),
    Font = dosFont
};

sessionNavigator.SetParentForm(this);

// Set the theme manager directly
sessionNavigator.SetThemeManager(themeManager);

// Hook up the event handler for connection requests
sessionNavigator.ConnectRequested += SessionNavigator_ConnectRequested;
// this.navigationSidePanel = navigatorPanel;


// Try to load the sessions file
string sessionsPath = GetSessionsFolderPath();


if (File.Exists(sessionsPath))
{
    sessionNavigator.SetSessionsFilePath(sessionsPath);
}
else
{
    // Create directory if it doesn't exist
    string sessionsDir = Path.GetDirectoryName(sessionsPath);
    if (!Directory.Exists(sessionsDir))
    {
        Directory.CreateDirectory(sessionsDir);
    }
    
    // No sessions file yet - you could create a default one here
    UpdateStatus("No sessions file found. Create one to see your sessions.");
}

// Add the session navigator to the navigator panel
navigatorPanel.Controls.Add(sessionNavigator);
    
    // ==================
    // MAIN CONTENT AREA (Right Side)
    // ==================
    Panel mainContentPanel = new Panel
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(0),
        Margin = new Padding(0)
    };
    
    // Create a splitter between navigator and main content
    Splitter navigatorSplitter = new Splitter
    {
        Dock = DockStyle.Left,
        Width = 4,
        MinSize = 100,
        MinExtra = 200,
        BackColor = Theme.HexToColor(themeManager?.CurrentTheme?.UI.Border ?? "#FFFFFF")
    };
    this.navigatorSplitter = navigatorSplitter;


    // Create the main layout for the right side content
    TableLayoutPanel mainLayout = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 4, // Menu, Toolbar, TabControl area, Function key bar
        Padding = new Padding(0),
        Margin = new Padding(0)
    };
    
    // Configure row heights - use font size as the basis for scaling
    float menuHeight = dosFont.Height + 8;
    float toolbarHeight = dosFont.Height + 14;
    float functionKeyHeight = dosFont.Height * 2 + 10;

    // Set up main layout row styles
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, menuHeight)); // Row 0: Menu strip height
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); // Row 1: Toolbar height - increased to 70px
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Row 2: Tab area gets all remaining space
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, functionKeyHeight)); // Row 3: Function key bar height
    
    // ==================
    // MENU AREA
    // ==================
    // Create a container for the menu to accommodate the close button
    Panel menuContainer = new Panel
    {
        Dock = DockStyle.Fill,
        BackColor = Color.Silver,
        Padding = new Padding(0),
        Margin = new Padding(0)
    };
    
    // Create menu strip
    menuStrip = new MenuStrip
    {
        Dock = DockStyle.Fill,
        BackColor = Color.Silver,
        ForeColor = Color.Black,
        Font = dosFont,
        Padding = new Padding(1, 2, 0, 2),
        RenderMode = ToolStripRenderMode.Professional
    };
    
    // Add the menu strip to its container
    menuContainer.Controls.Add(menuStrip);
    
    // Create close button within the menu container
    closeButton = new Button
    {
        Text = "[X]   ",
        Font = new Font(dosFont.FontFamily, 8, FontStyle.Bold),
        ForeColor = Color.White,
        BackColor = BorlandDarkBlue,
        FlatStyle = FlatStyle.Flat,
        Size = new Size(55, (int)menuHeight - 2),
        Anchor = AnchorStyles.Top | AnchorStyles.Right,
        TabStop = false,
        Margin = new Padding(2)
    };

    closeButton.FlatAppearance.BorderSize = 1;
    closeButton.FlatAppearance.BorderColor = Color.White;
    closeButton.Click += (s, e) => this.Close();

    // Add the close button to the menu container and position it
    menuContainer.Controls.Add(closeButton);
    menuContainer.Layout += (s, e) => {
        // Reposition the close button when the container is resized
        closeButton.Location = new Point(menuContainer.Width - closeButton.Width - 2, 1);
    };

    // Ensure the close button is on top of other controls
    menuContainer.Controls.SetChildIndex(closeButton, 0);

    // ==================
    // TOOLBAR AREA
    // ==================
    // Create a TableLayoutPanel for the main layout
    TableLayoutPanel toolbarContainer = new TableLayoutPanel 
    {     
        Dock = DockStyle.Fill,     
        BackColor = Theme.HexToColor(themeManager?.CurrentTheme?.UI.Background ?? "#0000AA"),
        Padding = new Padding(4),     
        Margin = new Padding(1),     
        RowCount = 1,     
        ColumnCount = 2 // Left side and right side
    };
    
    // Set column styles - first column takes all available space
    toolbarContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
    toolbarContainer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

    // Calculate button dimensions that fit within the toolbar
    int buttonHeight = 42; // More reasonable height
    int buttonWidth = 180; // More reasonable width
    int buttonSpacing = 8;
    int buttonFontSize = 8;

    // Create nested TableLayoutPanel for left buttons
    TableLayoutPanel leftButtonPanel = new TableLayoutPanel
    {
        Dock = DockStyle.Left,
        AutoSize = true,
        BackColor = Color.Transparent,
        Padding = new Padding(4),
        RowCount = 1,
        ColumnCount = 3, // Three buttons
        Margin = new Padding(0)
    };

    // Set all columns to auto-size
    leftButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
    leftButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
    leftButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

    // Create right button panel
    TableLayoutPanel rightButtonPanel = new TableLayoutPanel
    {
        Dock = DockStyle.Right,
        AutoSize = true,
        BackColor = Color.Transparent,
        Padding = new Padding(4),
        RowCount = 1,
        ColumnCount = 1,
        Margin = new Padding(0)
    };
    rightButtonPanel.Padding = new Padding(4, 4, 10, 4); // Extra padding on right

    rightButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

    // Create toolbar buttons with appropriate size and consistent styling
    Button connectButton = new Button
    {
        Text = "CONNECT",
        BackColor = BorlandCyan,
        ForeColor = Color.Black,
        FlatStyle = FlatStyle.Flat,
        Size = new Size(buttonWidth, buttonHeight),
        Font = new Font(dosFont.FontFamily, buttonFontSize, FontStyle.Bold),
        UseVisualStyleBackColor = false,
        Margin = new Padding(buttonSpacing)
    };
    connectButton.FlatAppearance.BorderColor = Color.White;
    connectButton.FlatAppearance.BorderSize = 1;
    connectButton.Click += ConnectButton_Click;

    Button directoryButton = new Button
    {
        Text = "DIRECTORY",
        BackColor = BorlandCyan,
        ForeColor = Color.Black,
        FlatStyle = FlatStyle.Flat,
        Size = new Size(buttonWidth + 20, buttonHeight),
        Font = new Font(dosFont.FontFamily, buttonFontSize, FontStyle.Bold),
        UseVisualStyleBackColor = false,
        Margin = new Padding(buttonSpacing)
    };
    directoryButton.FlatAppearance.BorderColor = Color.White;
    directoryButton.FlatAppearance.BorderSize = 1;
    directoryButton.Click += ShowConnectionDirectory_Click;

    Button disconnectButton = new Button
    {
        Text = "DISCONNECT",
        BackColor = BorlandCyan,
        ForeColor = Color.Black,
        FlatStyle = FlatStyle.Flat,
        Size = new Size(buttonWidth + 30, buttonHeight),
        Font = new Font(dosFont.FontFamily, buttonFontSize, FontStyle.Bold),
        UseVisualStyleBackColor = false,
        Margin = new Padding(buttonSpacing)
    };
    disconnectButton.FlatAppearance.BorderColor = Color.White;
    disconnectButton.FlatAppearance.BorderSize = 1;
    disconnectButton.Click += DisconnectCurrentTab_Click;
    disconnectButton.Click += (sender, e) => { themeManager.DebugControlHierarchy(this); };

    // Add buttons to their respective panels
    leftButtonPanel.Controls.Add(connectButton, 0, 0);
    leftButtonPanel.Controls.Add(directoryButton, 1, 0);
    rightButtonPanel.Controls.Add(disconnectButton, 0, 0);

    // Add button panels to the main toolbar container
    toolbarContainer.Controls.Add(leftButtonPanel, 0, 0);
    toolbarContainer.Controls.Add(rightButtonPanel, 1, 0);

    // Set vertical alignment to center
    leftButtonPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
    rightButtonPanel.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
    
    // ==================
    // TAB AREA
    // ==================
    // Create tab container panel with proper spacing
    Panel tabContainer = new Panel
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(4),
        Margin = new Padding(0),
        BackColor = Theme.HexToColor(themeManager?.CurrentTheme?.UI.Background ?? "#F0F0F0")
    };
    
    // Create the tab control with scaled properties
    tabControl = new ThemedTabControl
    {
        Dock = DockStyle.Fill,
        DrawMode = TabDrawMode.OwnerDrawFixed,
        Appearance = TabAppearance.FlatButtons,
        Padding = new Point((int)(dosFont.Size * 0.8), 6),
        Font = dosFont,
        ItemSize = new Size((int)(dosFont.Size * 8), (int)(dosFont.Size * 3.5)),
        BackColor = Theme.HexToColor(themeManager?.CurrentTheme?.UI.Background ?? "#000000"),
        SizeMode = TabSizeMode.Normal
    };
    
    // Create status panel with scalable dimensions
    int statusHeight = (int)(dosFont.Height * 1.8);
    statusPanel = new Panel
{
    Name = "statusPanel",
    Dock = DockStyle.Bottom,
    Height = statusHeight,
    BackColor = BorlandCyan,
    ForeColor = Color.Black,
    Padding = new Padding(5),
    Margin = new Padding(4)
};
    
    // Create status label with proper font scaling
    statusLabel = new Label
    {
        Name = "statusLabel",  // Explicitly set the name
        Dock = DockStyle.Fill,
        Text = "Ready",
        Font = dosFont,
        BackColor = BorlandCyan,
        ForeColor = Color.Black,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding((int)(dosFont.Size * 0.5), 0, 0, 0)
    };
    
    // Setup status panel paint handler with scaled borders
    // statusPanel.Paint += StatusPanel_Paint;
    
    statusPanel.Paint += (s, e) => {
    // Fill the entire panel with background color first
    e.Graphics.Clear(statusPanel.BackColor);
    
    // Skip drawing any borders or titles
    // This simplifies the panel to avoid any drawing artifacts
};
    // Add status label to status panel
    statusPanel.Controls.Add(statusLabel);
    
    // Add tab control and status panel to tab container
    tabContainer.Controls.Add(tabControl);
    tabContainer.Controls.Add(statusPanel);
    
    // ==================
    // FUNCTION KEY AREA
    // ==================
    // Create function key panel with scaled dimensions
    Panel functionKeyPanel = new Panel
    {
        Dock = DockStyle.Fill,
        BackColor = Theme.HexToColor(themeManager.CurrentTheme?.UI.MenuBackground ?? "#C0C0C0"),
        Padding = new Padding((int)(dosFont.Size * 0.4), 0, 
                              (int)(dosFont.Size * 0.3), (int)(dosFont.Size * 0.4))
    };
    
    // Create flow panel for function keys with proper spacing
    FlowLayoutPanel keyFlow = new FlowLayoutPanel
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        AutoScroll = true,
        BackColor = Color.Transparent,
        Padding = new Padding((int)(dosFont.Size * 0.3), 
                             (int)(dosFont.Size * 0.5), 
                             (int)(dosFont.Size * 0.3), 0)
    };
    
    // Add function key labels with proper scaling
    AddFunctionKeyLabels(keyFlow);
    
    // Create a much more visible resize grip
    Label resizeGrip = new Label
    {
        Text = "◢",
        Font = new Font("Arial", 18, FontStyle.Bold),
        Size = new Size(80, 30),  // Adjusted size
        AutoSize = false,
        TextAlign = ContentAlignment.BottomRight, // Align text to bottom-right
        ForeColor = Color.White,
        BorderStyle = BorderStyle.None,
        Cursor = Cursors.SizeNWSE,
        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        Name = "resizeGrip",
        Padding = new Padding(0, 0, 5, 0), // Add a bit of right padding to prevent text cutoff
        Margin = new Padding(0)
    };

    // Setup mouse handler for resizing
    resizeGrip.MouseDown += ResizeGrip_MouseDown;

    // Add controls to the function key panel in the correct order
    functionKeyPanel.Controls.Add(keyFlow);
    functionKeyPanel.Controls.Add(resizeGrip);

    // This ensures the label is positioned correctly in the bottom-right corner
    resizeGrip.Dock = DockStyle.Right; // Try using Dock instead of explicit positioning

    // If using Location instead of Dock, add this resize handler
    functionKeyPanel.Resize += (s, e) => {
        resizeGrip.Location = new Point(
            functionKeyPanel.ClientSize.Width - resizeGrip.Width,
            functionKeyPanel.ClientSize.Height - resizeGrip.Height);
    };

    resizeGrip.MouseEnter += (s, e) => {
        resizeGrip.Font = new Font(resizeGrip.Font.FontFamily, resizeGrip.Font.Size + 2, FontStyle.Bold);
        ToolTip toolTip = new ToolTip();
        toolTip.SetToolTip(resizeGrip, "Click and drag to resize window");
    };

    resizeGrip.MouseLeave += (s, e) => {
        resizeGrip.Font = new Font("Arial", 18, FontStyle.Bold);
        resizeGrip.Text = "◢";
    };

    // Ensure it's always on top
    resizeGrip.BringToFront();
    
    // ==================
    // FINALIZE LAYOUT
    // ==================
    
    // Add tab drawing event handler
    tabControl.DrawItem += TabControl_DrawItem;
    
    // Add all main sections to the main layout in correct order
    mainLayout.Controls.Add(menuContainer, 0, 0);     // Row 0: Menu
    mainLayout.Controls.Add(toolbarContainer, 0, 1);  // Row 1: Toolbar
    mainLayout.Controls.Add(tabContainer, 0, 2);      // Row 2: Tab area
    mainLayout.Controls.Add(functionKeyPanel, 0, 3);  // Row 3: Function keys
    
    // Add main layout to main content panel 
    mainContentPanel.Controls.Add(mainLayout);
    
    // Add splitter to content panel - needs to be added *after* the layout
    mainContentPanel.Controls.Add(this.navigatorSplitter);
    
    // Add panels to horizontal split layout
    horizontalSplitLayout.Controls.Add(navigatorPanel, 0, 0);
    horizontalSplitLayout.Controls.Add(mainContentPanel, 1, 0);
    
    // Add horizontal split layout to form
    this.Controls.Add(horizontalSplitLayout);
    
    // Make sure the close button is on top
    closeButton.BringToFront();
    
    // Also ensure the tabContainer background matches the theme
    tabContainer.BackColor = Theme.HexToColor(themeManager?.CurrentTheme?.UI.Background ?? "#F0F0F0");

    // Then call the FixTabPageBackgrounds method
    themeManager.FixTabPageBackgrounds(this);
    
    this.ResumeLayout(false);
}


private string GetSessionsFolderPath()
{
    // Get the app's base directory (working directory)
    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
    
    // Define the sessions folder
    string sessionsFolder = Path.Combine(baseDir, "sessions");
    
    // Create the folder if it doesn't exist
    if (!Directory.Exists(sessionsFolder))
    {
        Directory.CreateDirectory(sessionsFolder);
    }
    
    // Return the default sessions file path
    return Path.Combine(sessionsFolder, "default_sessions.yaml");
}

private void ToggleNavigatorPanel_Click(object sender, EventArgs e)
{
    // Debug output to help troubleshoot
    Console.WriteLine($"Toggle navigator - navigatorPanel: {navigatorPanel != null}, horizontalSplitLayout: {horizontalSplitLayout != null}");
    
    if (navigatorPanel != null && horizontalSplitLayout != null)
    {
        if (navigatorPanel.Visible)
        {
            // Save current width before hiding
            savedNavigatorWidth = navigatorPanel.Width;
            
            // Hide panel
            navigatorPanel.Visible = false;
            
            // Update column style to give 0 width to navigator
            horizontalSplitLayout.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, 0F);
            
            // Update menu item
            if (toggleNavigatorMenuItem != null)
            {
                // toggleNavigatorMenuItem.Text = "Hide Navigator";
                toggleNavigatorMenuItem.Checked = false;
            }
            
            UpdateStatus("Session navigator hidden");
        }
        else
        {
            // Show panel
            navigatorPanel.Visible = true;
            
            // Restore column style with saved width
            horizontalSplitLayout.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, savedNavigatorWidth);
            
            // Update menu item
            if (toggleNavigatorMenuItem != null)
            {
                toggleNavigatorMenuItem.Text = "Show Navigator";
                toggleNavigatorMenuItem.Checked = true;
            }
            
            UpdateStatus("Session navigator visible");
        }
    }
    else
    {
        // Log error if components are null
        Console.WriteLine("Toggle navigator panel failed - components not initialized");
        UpdateStatus("Error: Could not toggle navigator panel");
    }
}

private void SessionNavigator_ConnectRequested(object sender, SessionConnectionEventArgs e)
{
    // For new connections or if password is missing/empty
    if (e.IsNewConnection || !e.ConnectionData.ContainsKey("password") || string.IsNullOrEmpty(e.ConnectionData["password"].ToString()))
    {
        // Create a connection profile with the available data
        Models.ConnectionProfile profile = null;
        
        // Only create profile if we have some data (not a completely new connection)
        if (!e.IsNewConnection && e.ConnectionData.ContainsKey("host"))
        {
            profile = new Models.ConnectionProfile();
            
            // Set available properties
            if (e.ConnectionData.ContainsKey("host"))
                profile.Host = e.ConnectionData["host"].ToString();
                
            if (e.ConnectionData.ContainsKey("port"))
                profile.Port = Convert.ToInt32(e.ConnectionData["port"]);
                
            if (e.ConnectionData.ContainsKey("username"))
                profile.Username = e.ConnectionData["username"].ToString();
                
            if (e.ConnectionData.ContainsKey("password"))
                profile.EncryptedPassword = e.ConnectionData["password"].ToString();
                
            if (e.ConnectionData.ContainsKey("displayName"))
                profile.Name = e.ConnectionData["displayName"].ToString();
            else if (e.ConnectionData.ContainsKey("host"))
                profile.Name = e.ConnectionData["host"].ToString();
        }
        
        // Show dialog with pre-filled profile if available
        using (ConnectionDialog dialog = new ConnectionDialog(profile, themeManager))
        {
            dialog.TopMost = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // Continue with your existing connection logic...
                // Check if we need to create a new tab first
                if (tabManager.CurrentTab == null || tabManager.CurrentTab.IsConnected)
                {
                    // Create a new tab since the current tab is connected or there's no tab
                    var newTab = tabManager.CreateNewTab();
                    UpdateStatus($"Created new tab with ID: {newTab.TabId}");
                }
                
                // Get the current tab
                var tabToConnect = tabManager.CurrentTab;
                
                // Set connection parameters
                tabToConnect.Host = dialog.Host;
                tabToConnect.Port = dialog.Port;
                tabToConnect.Username = dialog.Username;
                tabToConnect.Password = dialog.Password;
                tabToConnect.ConnectionName = dialog.ConnectionName;
                
                // Log the connection attempt with tab ID
                Console.WriteLine($"Tab {tabToConnect.TabId}: Setting up connection to {dialog.Username}@{dialog.Host}:{dialog.Port}");
                
                // Save connection if requested
                if (dialog.SaveConnection && !string.IsNullOrWhiteSpace(dialog.ConnectionName))
                {
                    SaveConnectionProfile(
                        dialog.ConnectionName,
                        dialog.Host,
                        dialog.Port,
                        dialog.Username,
                        dialog.Password);
                }
                
                // Connect (this calls the async method)
                ConnectCurrentTab();
            }
        }
        return;
    }
    
    // The rest of your existing code for direct connections remains unchanged
    string host = e.ConnectionData["host"].ToString();
    int port = Convert.ToInt32(e.ConnectionData["port"]);
    string username = e.ConnectionData["username"].ToString();
    string password = e.ConnectionData["password"].ToString();
    string displayName = e.ConnectionData.ContainsKey("displayName") ? 
        e.ConnectionData["displayName"].ToString() : host;
    
    // Check if we need to create a new tab first
    if (tabManager.CurrentTab == null || tabManager.CurrentTab.IsConnected)
    {
        // Create a new tab since the current tab is connected or there's no tab
        var newTab = tabManager.CreateNewTab();
        UpdateStatus($"Created new tab with ID: {newTab.TabId}");
    }
    
    // Get the current tab
    var currentTab = tabManager.CurrentTab;
    
    // Set connection parameters
    currentTab.Host = host;
    currentTab.Port = port;
    currentTab.Username = username;
    currentTab.Password = password;
    currentTab.ConnectionName = displayName;
    
    // Log the connection attempt with tab ID
    Console.WriteLine($"Tab {currentTab.TabId}: Setting up connection from session navigator to {username}@{host}:{port}");
    
    // Connect (this calls the async method)
    ConnectCurrentTab();
}

private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
{
    // Check if the index is valid
    if (e.Index < 0 || e.Index >= tabControl.TabPages.Count)
        return;
        
    // Get the tab page
    TabPage tabPage = tabControl.TabPages[e.Index];
    
    // Get the tab rectangle
    Rectangle tabRect = tabControl.GetTabRect(e.Index);
    
    // Ensure we have a theme manager
    if (themeManager?.CurrentTheme == null)
        return;
        
    var theme = themeManager.CurrentTheme;
    
    // Determine colors based on selection state
    Color bgColor, textColor, borderColor;
    
    if ((e.State & DrawItemState.Selected) != 0)
    {
        // Active tab - use theme background
        bgColor = Theme.HexToColor(theme.UI.Background);
        textColor = Theme.HexToColor(theme.UI.Text);
        borderColor = Theme.HexToColor(theme.UI.Border);
    }
    else
    {
        // Inactive tab - use theme button background
        bgColor = Theme.HexToColor(theme.UI.ButtonBackground);
        textColor = Theme.HexToColor(theme.UI.ButtonText);
        borderColor = Theme.HexToColor(theme.UI.Border);
    }
    
    // Fill the background
    using (SolidBrush brush = new SolidBrush(bgColor))
    {
        e.Graphics.FillRectangle(brush, tabRect);
    }
    
    // Draw border
    using (Pen pen = new Pen(borderColor))
    {
        e.Graphics.DrawRectangle(pen, tabRect);
    }
    
    // Draw the text
    string tabText = tabPage.Text;
    
    // Calculate text layout
    StringFormat stringFormat = new StringFormat
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };
    
    // Draw the text
    using (SolidBrush brush = new SolidBrush(textColor))
    {
        e.Graphics.DrawString(tabText, tabPage.Font, brush, tabRect, stringFormat);
    }
    
    // Draw close button "X" for all tabs except the "+" tab
    if (tabPage.Tag == null || tabPage.Tag.ToString() != "new-tab-button")
    {
        // Calculate close button rectangle
        Rectangle closeRect = new Rectangle(
            tabRect.Right - 15, tabRect.Top + 5, 10, 10);
        
        // Draw the "X"
        using (Pen pen = new Pen(borderColor))
        {
            e.Graphics.DrawLine(pen, closeRect.Left + 2, closeRect.Top + 2, 
                closeRect.Right - 2, closeRect.Bottom - 2);
            e.Graphics.DrawLine(pen, closeRect.Right - 2, closeRect.Top + 2, 
                closeRect.Left + 2, closeRect.Bottom - 2);
        }
    }
}

        // ==================
        // HELPER METHODS
        // ==================


private void FixAllTabBackgrounds()
{
    if (themeManager?.CurrentTheme == null || tabControl == null) return;
    
    Color bgColor = Theme.HexToColor(themeManager.CurrentTheme.UI.Background);
    
    // Set all tab page backgrounds to the theme background color
    foreach (TabPage page in tabControl.TabPages)
    {
        page.BackColor = bgColor;
        
        // Also apply to parent panel (if it exists)
        if (page.Parent is Panel panel)
        {
            panel.BackColor = bgColor;
        }
    }
    
    // Also handle tab page's containing panel
    Control[] tabContainers = this.Controls.Find("tabContainer", true);
    if (tabContainers.Length > 0 && tabContainers[0] is Panel tabContainer)
    {
        tabContainer.BackColor = bgColor;
    }
    
    // Force repaint
    tabControl.Invalidate(true);
}


private void AddFunctionKeyLabels(FlowLayoutPanel keyFlow)
{
    // Function key definitions - add F3 for Sessions
    string[] keys = { "F1", "F2", "F3", "F4", "F9", "F10" };
    string[] descriptions = { "Help", "Save", "Sessions", "Directory", "Quit", "Menu" };
    
    for (int i = 0; i < keys.Length; i++)
    {
        Label keyLabel = new Label
        {
            AutoSize = true,
            Margin = new Padding((int)(dosFont.Size * 0.7), 
                               (int)(dosFont.Size * 0.25), 
                               (int)(dosFont.Size * 1.2), 
                               (int)(dosFont.Size * 0.25)),
            Text = $"{keys[i]}-{descriptions[i]}",
            Font = dosFont,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        
        // Custom paint handler for function key labels
        keyLabel.Paint += FunctionKey_Paint;
        
        // Add click handlers for all function keys
        switch (keys[i])
        {
            case "F1":
                keyLabel.Click += ShowAboutDialog_Click;
                break;
            case "F2":
                // Add handler for Save functionality
                keyLabel.Click += (s, e) => {
                    UpdateStatus("Save function triggered");
                    // Call appropriate method
                };
                break;
            case "F3":
                // Add handler for Sessions toggle
                keyLabel.Click += ToggleNavigatorPanel_Click;
                break;
            case "F4":
                keyLabel.Click += ShowConnectionDirectory_Click;
                break;
            case "F9":
                keyLabel.Click += HandleQuit_Click;
                break;
            case "F10":
                keyLabel.Click += (s, e) => {
                    menuStrip.Focus();
                    if (menuStrip.Items.Count > 0)
                    {
                        ((ToolStripMenuItem)menuStrip.Items[0]).ShowDropDown();
                    }
                    UpdateStatus("Menu activated");
                };
                break;
        }
        
        keyFlow.Controls.Add(keyLabel);
    }
}

        
        private Button CreateToolbarButton(string text, EventHandler clickHandler)
        {
            // Calculate button dimensions based on font size
            int buttonWidth = (int)(dosFont.Size * 8);
            int buttonHeight = (int)(dosFont.Size * 1.6);
            int buttonMargin = (int)(dosFont.Size * 0.4);
            
            Button button = new Button
            {
                Text = text,
                BackColor = BorlandCyan,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(buttonWidth, buttonHeight),
                Margin = new Padding(buttonMargin, 0, buttonMargin, 0),
                Font = new Font(dosFont.FontFamily, dosFont.Size * 0.9f, FontStyle.Bold)
            };
            
            button.FlatAppearance.BorderColor = Color.Black;
            button.FlatAppearance.BorderSize = 1;
            
            if (clickHandler != null)
            {
                button.Click += clickHandler;
            }
            
            return button;
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
        
        private void PopulateMenuItems()
        {
            // Create menu renderer
            menuStrip.Renderer = new RetroMenuRenderer(themeManager);
            
            // File menu
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add(CreateMenuItem("New Tab", 'N', NewTab_Click));
            fileMenu.DropDownItems.Add(CreateMenuItem("Close Tab", 'C', CloseTab_Click));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(CreateMenuItem("Connection Directory", 'D', ShowConnectionDirectory_Click));
            
             fileMenu.DropDownItems.Add(CreateMenuItem("Edit Sessions", 'E', EditSessions_Click));
    
    fileMenu.DropDownItems.Add(CreateMenuItem("Connection Directory", 'D', ShowConnectionDirectory_Click));
    fileMenu.DropDownItems.Add(new ToolStripSeparator());
    fileMenu.DropDownItems.Add(CreateMenuItem("Exit", 'x', (s, e) => this.Close()));

            // Terminal menu
            ToolStripMenuItem terminalMenu = new ToolStripMenuItem("Terminal");
            terminalMenu.DropDownItems.Add(CreateMenuItem("Connect", 'C', ConnectButton_Click));
            terminalMenu.DropDownItems.Add(CreateMenuItem("Disconnect", 'D', DisconnectCurrentTab_Click));
            terminalMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // Modem sound toggle
            ToolStripMenuItem soundMenuItem = CreateMenuItem("Modem Sound", 'M', ToggleModemSound);
            soundMenuItem.Checked = settingsManager.EnableModemSound;
            terminalMenu.DropDownItems.Add(soundMenuItem);
            
            // Themes submenu
            ToolStripMenuItem themesMenu = new ToolStripMenuItem("Themes");
            
            // Add built-in themes
            foreach (var themeName in themeManager.GetAvailableThemeNames())
            {
                ToolStripMenuItem themeItem = new ToolStripMenuItem(themeName);
                themeItem.Click += (s, e) => themeManager.ApplyTheme(themeName);
                themesMenu.DropDownItems.Add(themeItem);
            }
            
            // Add theme management options
            themesMenu.DropDownItems.Add(new ToolStripSeparator());
            themesMenu.DropDownItems.Add(CreateMenuItem("Import Theme...", 'I', ImportTheme_Click));
            themesMenu.DropDownItems.Add(CreateMenuItem("Export Current Theme...", 'E', ExportCurrentTheme_Click));
            themesMenu.DropDownItems.Add(new ToolStripSeparator());
            themesMenu.DropDownItems.Add(CreateMenuItem("Reset to Default Themes", 'R', ResetThemes_Click));

            terminalMenu.DropDownItems.Add(new ToolStripSeparator());
            terminalMenu.DropDownItems.Add(themesMenu);
            
            // View menu
          
            ToolStripMenuItem viewMenu = new ToolStripMenuItem("View");
            viewMenu.DropDownItems.Add(CreateMenuItem("Maximize", 'M', MaximizeWindow_Click));
            viewMenu.DropDownItems.Add(CreateMenuItem("Restore", 'R', RestoreWindow_Click));
            viewMenu.DropDownItems.Add(new ToolStripSeparator());

            // Toggle navigator panel option
            toggleNavigatorMenuItem = CreateMenuItem("Show Navigator", 'N', ToggleNavigatorPanel_Click);
            toggleNavigatorMenuItem.Checked = true; // Initially checked since navigator is visible by default
            viewMenu.DropDownItems.Add(toggleNavigatorMenuItem);

            // Help menu
            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help");
            helpMenu.DropDownItems.Add(CreateMenuItem("About", 'A', ShowAboutDialog_Click));
            
            // Add menus to strip
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(terminalMenu);
            menuStrip.Items.Add(viewMenu);
            menuStrip.Items.Add(helpMenu);
            
            // Make the menu draggable for moving the form
            menuStrip.MouseDown += MenuStrip_MouseDown;
        }
                private void DisconnectCurrentTab()
        {
            tabManager.DisconnectTab();
            UpdateStatus("Disconnected");
        }
        // ==================
        // EVENT HANDLERS
        // ==================
        
        private void ResetThemes_Click(object sender, EventArgs e)
{
    ResetThemesToDefaults();
}


// Add this implementation method with your other methods
private void ResetThemesToDefaults()
{
    // Confirm with the user before resetting
    DialogResult result = MessageBox.Show(
        "This will remove all custom themes and restore only the built-in themes.\nAre you sure you want to continue?",
        "Reset Themes",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning,
        MessageBoxDefaultButton.Button2);
        
    if (result == DialogResult.Yes)
    {
        try
        {
            // Save the current theme name
            string currentThemeName = themeManager.CurrentThemeName;
            
            // Call our reset method from ThemeManager
            themeManager.ResetToBuiltInThemes();
            
            // Reload the themes menu
            UpdateThemesMenu();
            
            // Show confirmation
            UpdateStatus("Themes reset to defaults successfully");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error resetting themes: {ex.Message}", 
                "Reset Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("Failed to reset themes");
        }
    }
}

// Add this method to refresh the themes menu after resetting
private void UpdateThemesMenu()
{
    // Find the Themes menu
    ToolStripMenuItem themesMenu = null;
    foreach (ToolStripItem item in menuStrip.Items)
    {
        if (item is ToolStripMenuItem menuItem)
        {
            foreach (ToolStripItem subItem in menuItem.DropDownItems)
            {
                if (subItem is ToolStripMenuItem subMenuItem && subMenuItem.Text == "Themes")
                {
                    themesMenu = subMenuItem;
                    break;
                }
            }
            if (themesMenu != null) break;
        }
    }
    
    if (themesMenu != null)
    {
        // Remove all existing theme menu items, keeping only the last 3 items 
        // (separator, Import Theme, Export Current Theme, separator, Reset Themes)
        int itemsToKeep = 5;
        while (themesMenu.DropDownItems.Count > itemsToKeep)
        {
            themesMenu.DropDownItems.RemoveAt(0);
        }
        
        // Re-add all available themes at the top of the menu
        int index = 0;
        foreach (var themeName in themeManager.GetAvailableThemeNames())
        {
            ToolStripMenuItem themeItem = new ToolStripMenuItem(themeName);
            themeItem.Click += (s, e) => themeManager.ApplyTheme(themeName);
            themesMenu.DropDownItems.Insert(index++, themeItem);
        }
        
        // Add separator after theme items if not already there
        if (index > 0 && !(themesMenu.DropDownItems[index] is ToolStripSeparator))
        {
            themesMenu.DropDownItems.Insert(index, new ToolStripSeparator());
        }
    }
}
        private void ConnectButton_Click(object sender, EventArgs e)
        {
            ShowConnectDialog();
        }
        private async void ConnectCurrentTab()
{
    try
    {
        var currentTab = tabManager.CurrentTab;
        if (currentTab != null)
        {
            UpdateStatus($"Connecting ... ");
            await tabManager.ConnectTabAsync();
        }
        else
        {
            UpdateStatus("No active tab to connect");
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Connection failed: {ex.Message}", 
            "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        UpdateStatus($"Connection failed: {ex.Message}");
    }
}

private void EditSessions_Click(object sender, EventArgs e)
{
    // Get the path to the sessions file
    string sessionsFilePath = GetSessionsFolderPath();
    
    // Check if file doesn't exist, create a default one
    if (!File.Exists(sessionsFilePath))
    {
        CreateDefaultSessionsFile(sessionsFilePath);
    }
    
    // Show the session editor dialog
    using (var editor = new SessionEditorDialog(sessionsFilePath, themeManager))
    {
        if (editor.ShowDialog() == DialogResult.OK)
        {
            // Reload sessions in navigator if it exists
            UpdateStatus("Sessions file updated successfully");
            
            // Find and refresh the session navigator
            foreach (Control control in navigatorPanel.Controls)
            {
                if (control is SessionNavigator navigator)
                {
                    navigator.LoadSessions();
                    break;
                }
            }
        }
    }
}
private void CreateDefaultSessionsFile(string filePath)
{
    // Create a default folder and session structure
    var defaultSessions = new List<FolderData>
    {
        new FolderData
        {
            FolderName = "Default",
            Sessions = new List<SessionData>
            {
                new SessionData
                {
                    DisplayName = "Local Connection",
                    Host = "localhost",
                    Port = 22,
                    DeviceType = "Linux",
                    Model = "",
                    SerialNumber = "",
                    SoftwareVersion = "",
                    Vendor = "",
                    CredsId = "",
                    Username = "",
                    Password = ""
                }
            }
        }
    };
    
    // Serialize to YAML and save
    var serializer = new SerializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)  // Use the same convention as your deserializer
        .Build();
    
    string yaml = serializer.Serialize(defaultSessions);
    File.WriteAllText(filePath, yaml);
}

private void ShowConnectDialog()
{
    using (ConnectionDialog dialog = new ConnectionDialog(themeManager))
    {
        dialog.TopMost = true;
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            // Check if we need to create a new tab first
            if (tabManager.CurrentTab == null || tabManager.CurrentTab.IsConnected)
            {
                // Create a new tab since the current tab is connected or there's no tab
                var newTab = tabManager.CreateNewTab();
                UpdateStatus($"Created new tab with ID: {newTab.TabId}");
            }
            
            // Get the current tab
            var currentTab = tabManager.CurrentTab;
            
            // Set connection parameters
            currentTab.Host = dialog.Host;
            currentTab.Port = dialog.Port;
            currentTab.Username = dialog.Username;
            currentTab.Password = dialog.Password;
            currentTab.ConnectionName = dialog.ConnectionName;
            
            // Log the connection attempt with tab ID
            Console.WriteLine($"Tab {currentTab.TabId}: Setting up connection to {dialog.Username}@{dialog.Host}:{dialog.Port}");
            
            // Save connection if requested
            if (dialog.SaveConnection && !string.IsNullOrWhiteSpace(dialog.ConnectionName))
            {
                SaveConnectionProfile(
                    dialog.ConnectionName,
                    dialog.Host,
                    dialog.Port,
                    dialog.Username,
                    dialog.Password);
            }
            
            // Connect (this calls the async method)
            ConnectCurrentTab();
        }
    }
}

        private void ShowConnectionDirectory_Click(object sender, EventArgs e)
        {
            ShowConnectionDirectory();
        }
        
        private void NewTab_Click(object sender, EventArgs e)
        {
            tabManager.CreateNewTab();
        }
        
        private void CloseTab_Click(object sender, EventArgs e)
        {
            tabManager.CloseTab();
        }
        
        private void DisconnectCurrentTab_Click(object sender, EventArgs e)
        {
            DisconnectCurrentTab();
        }
        
        private void ImportTheme_Click(object sender, EventArgs e)
        {
            ImportTheme();
        }
        
        private void ExportCurrentTheme_Click(object sender, EventArgs e)
        {
            ExportCurrentTheme();
        }
        
        private void MaximizeWindow_Click(object sender, EventArgs e)
        {
            MaximizeWindow();
        }
        
        private void RestoreWindow_Click(object sender, EventArgs e)
        {
            RestoreWindow();
        }
        
        private void ShowAboutDialog_Click(object sender, EventArgs e)
        {
            ShowAboutDialog();
        }
        
        private void HandleQuit_Click(object sender, EventArgs e)
        {
            HandleQuit();
        }


private void FunctionKey_Paint(object sender, PaintEventArgs e)
{
    Label keyLabel = (Label)sender;
    string key = keyLabel.Text.Split('-')[0];
    string desc = keyLabel.Text.Substring(key.Length + 1);
    
    // Get theme colors (or use defaults if theme is not available)
    Color backgroundColor = Color.Silver;
    Color keyColor = Color.Red;
    Color descColor = Color.Black;
    
    // Use theme colors if available
    if (themeManager?.CurrentTheme != null)
    {
        backgroundColor = Theme.HexToColor(themeManager.CurrentTheme.UI.MenuBackground);
        keyColor = Theme.HexToColor(themeManager.CurrentTheme.UI.FunctionKeyText);
        descColor = Theme.HexToColor(themeManager.CurrentTheme.UI.FunctionKeyDescriptionText);
    }
    
    // Clear background
    e.Graphics.Clear(backgroundColor);
    
    // Draw key with themed color
    using (Brush keyBrush = new SolidBrush(keyColor))
    {
        e.Graphics.DrawString(key, keyLabel.Font, keyBrush, 0, 0);
    }
    
    // Get width for positioning
    SizeF keySize = e.Graphics.MeasureString(key, keyLabel.Font);
    
    // Draw description with themed color
    using (Brush descBrush = new SolidBrush(descColor))
    {
        e.Graphics.DrawString($"-{desc}", keyLabel.Font, descBrush, keySize.Width, 0);
    }
}

// private void StatusPanel_Paint(object sender, PaintEventArgs e)
// {
//     // Get theme colors
//     Color bgColor = themeManager?.CurrentTheme != null ? 
//         Theme.HexToColor(themeManager.CurrentTheme.UI.StatusBarBackground) : 
//         BorlandCyan;
        
//     Color textColor = themeManager?.CurrentTheme != null ? 
//         Theme.HexToColor(themeManager.CurrentTheme.UI.StatusBarText) : 
//         Color.Black;
    
//     // Clear the background
//     e.Graphics.Clear(bgColor);
    
//     // Draw title if desired
//     string title = " Status ";
//     int titleOffset = (int)(dosFont.Size * 0.8);
//     using (Font titleFont = new Font(statusLabel.Font.FontFamily, 
//         statusLabel.Font.Size, FontStyle.Bold))
//     {
//         e.Graphics.DrawString(title, titleFont, new SolidBrush(textColor), titleOffset, 0);
//     }
// }
private void StatusPanel_Paint(object sender, PaintEventArgs e)
{
    // Get theme colors
    Color bgColor = themeManager?.CurrentTheme != null ? 
        Theme.HexToColor(themeManager.CurrentTheme.UI.StatusBarBackground) : 
        BorlandCyan;
    
    Color textColor = themeManager?.CurrentTheme != null ? 
        Theme.HexToColor(themeManager.CurrentTheme.UI.StatusBarText) : 
        Color.Black;
    
    // Clear the background with a fully opaque fill
    e.Graphics.Clear(bgColor);
    
    // Draw title if you still want it (remove these lines if you don't want any title)
    string title = " Status ";
    int titleOffset = (int)(dosFont.Size * 0.8);
    using (Font titleFont = new Font(statusLabel.Font.FontFamily, 
        statusLabel.Font.Size, FontStyle.Bold))
    {
        e.Graphics.DrawString(title, titleFont, new SolidBrush(textColor), titleOffset, 0);
    }
}
private void ResizeGrip_Paint(object sender, PaintEventArgs e)
{
    Panel grip = (Panel)sender;
    
    // Use a bright color regardless of theme
    Color lineColor = Color.White;
    Color shadowColor = Color.Black;
    
    // Increase line width for better visibility
    int lineWidth = 3;
    
    // Clear background to a distinctive color
    e.Graphics.Clear(Color.Turquoise);
    
    // Add a strong border
    using (Pen borderPen = new Pen(Color.White, 2))
    {
        e.Graphics.DrawRectangle(borderPen, 0, 0, grip.Width - 1, grip.Height - 1);
    }
    
    // Draw more prominent diagonal lines
    using (Pen shadowPen = new Pen(shadowColor, lineWidth))
    using (Pen linePen = new Pen(lineColor, lineWidth))
    {
        for (int i = 1; i <= 3; i++)
        {
            int offset = i * (grip.Width / 4);
            
            // Shadow line
            e.Graphics.DrawLine(shadowPen, 
                grip.Width - offset + 1, grip.Height, 
                grip.Width, grip.Height - offset + 1);
            
            // Main line
            e.Graphics.DrawLine(linePen, 
                grip.Width - offset, grip.Height - 1, 
                grip.Width - 1, grip.Height - offset);
        }
    }
    
    // Add a larger corner indicator
    using (SolidBrush cornerBrush = new SolidBrush(Color.White))
    {
        e.Graphics.FillRectangle(cornerBrush, 
            grip.Width - 10, grip.Height - 10, 10, 10);
    }
}

        private void ResizeGrip_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                const int WM_SYSCOMMAND = 0x112;
                const int SC_SIZE = 0xF000;
                const int WMSZ_BOTTOMRIGHT = 8;
                
                ReleaseCapture();
                SendMessage(this.Handle, WM_SYSCOMMAND, SC_SIZE + WMSZ_BOTTOMRIGHT, 0);
            }
        }
        
        private void MenuStrip_MouseDown(object sender, MouseEventArgs e)
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
        }

private void ShowConnectionDirectory()
{
    using (ConnectionDirectoryForm directoryForm = new ConnectionDirectoryForm(themeManager))
    {
        if (directoryForm.ShowDialog() == DialogResult.OK && directoryForm.SelectedProfile != null)
        {
            // Check if we need to create a new tab first
            if (tabManager.CurrentTab == null || tabManager.CurrentTab.IsConnected)
            {
                // Create a new tab since the current tab is connected or there's no tab
                var newTab = tabManager.CreateNewTab();
                UpdateStatus($"Created new tab with ID: {newTab.TabId}");
            }
            
            // Get the current tab
            var currentTab = tabManager.CurrentTab;
            
            // Set connection parameters
            currentTab.Host = directoryForm.SelectedProfile.Host;
            currentTab.Port = directoryForm.SelectedProfile.Port;
            currentTab.Username = directoryForm.SelectedProfile.Username;
            currentTab.Password = directoryForm.SelectedProfile.EncryptedPassword;
            currentTab.ConnectionName = directoryForm.SelectedProfile.Name;
            
            // Log the connection setup with tab ID
            Console.WriteLine($"Tab {currentTab.TabId}: Setting up connection from directory: {directoryForm.SelectedProfile.Name}");
            
            // Connect if requested (this calls the async method)
            if (directoryForm.ConnectImmediately)
            {
                ConnectCurrentTab();
            }
        }
    }
}
        
        private void ToggleModemSound(object sender, EventArgs e)
        {
            settingsManager.EnableModemSound = !settingsManager.EnableModemSound;
            tabManager.EnableModemSound = settingsManager.EnableModemSound;
            
            // Update the menu item checked state
            if (sender is ToolStripMenuItem menuItem)
            {
                menuItem.Checked = settingsManager.EnableModemSound;
            }
            
            // Save the setting
            SaveSettings();
            
            // Update status
            UpdateStatus(settingsManager.EnableModemSound ? 
                "Modem sound enabled" : "Modem sound disabled");
        }
        
        private void SaveConnectionProfile(string name, string host, int port, string username, string password)
        {
            // Create profile
            Models.ConnectionProfile profile = new Models.ConnectionProfile
            {
                Name = name,
                Host = host,
                Port = port,
                Username = username,
                EncryptedPassword = password  // Note: In a real app, you should encrypt this
            };
            
            // Save profile (not implemented yet)
            // ConnectionDirectoryService.SaveProfile(profile);
            
            UpdateStatus($"Saved connection profile: {name}");
        }
        
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
                        var theme = themeManager.ImportTheme(dialog.FileName);
                        MessageBox.Show($"Theme '{theme.Name}' imported successfully.\nRestart RetroTerm.NET to use newly imported themes.", 
                            "Import Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            
                        UpdateStatus($"Imported theme: {theme.Name}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error importing theme: {ex.Message}", 
                            "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            
                        UpdateStatus("Theme import failed");
                    }
                }
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
                        MessageBox.Show($"Theme exported successfully to {dialog.FileName}", 
                            "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            
                        UpdateStatus($"Exported theme: {themeManager.CurrentTheme.Name}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting theme: {ex.Message}", 
                            "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            
                        UpdateStatus("Theme export failed");
                    }
                }
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
        
private void ShowAboutDialog()
{
    // Create a form with retro styling
    Form aboutBox = new Form
    {
        Text = "About RetroTerm.NET",
        FormBorderStyle = FormBorderStyle.Fixed3D,
        MaximizeBox = false,
        MinimizeBox = false,
        Size = new Size(700, 550), // Increased height to accommodate folder links
        StartPosition = FormStartPosition.CenterParent
    };
    
    // Get theme colors
    Color bgColor = BorlandBlue;
    Color textColor = Theme.HexToColor(themeManager?.CurrentTheme?.UI.Text ?? "#FFFFFF");
    Color buttonColor = BorlandCyan;
    Color buttonTextColor = Theme.HexToColor(themeManager?.CurrentTheme?.UI.ButtonText ?? "#000000");
    Color accentColor = Theme.HexToColor(themeManager?.CurrentTheme?.UI.InputText ?? "#FFFF00");
    
    aboutBox.BackColor = bgColor;
    aboutBox.ForeColor = textColor;
    
    // Create the main layout with improved spacing
    TableLayoutPanel mainLayout = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 6, // Added an extra row for folder links
        Padding = new Padding(25, 20, 25, 25),
        Margin = new Padding(0),
        BackColor = bgColor
    };
    
    // Set row styles with better distribution
    mainLayout.RowStyles.Clear();
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F)); // Title
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));  // Content description
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F)); // Author info
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));  // Folder links (new row)
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F)); // Button
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));  // Motto label
    
    // Title label
    Label titleLabel = new Label
    {
        Text = "RetroTerm.NET™",
        Font = new Font(dosFont.FontFamily, 22, FontStyle.Bold),
        ForeColor = accentColor,
        TextAlign = ContentAlignment.MiddleCenter,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 5, 0, 5),
        BackColor = bgColor,
        AutoSize = false
    };
    
    // Description text
    Label descriptionLabel = new Label
    {
        Text = "A nostalgic terminal emulator\nwith a retro DOS-inspired interface.\n\nConnect to multiple SSH servers with\na tabbed interface and customizable\ncolor themes.",
        Font = new Font(dosFont.FontFamily, 10),
        ForeColor = textColor,
        TextAlign = ContentAlignment.MiddleCenter,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 10, 0, 10),
        BackColor = bgColor
    };
    
    // Author container
    TableLayoutPanel authorContainer = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        BackColor = bgColor,
        RowCount = 2,
        ColumnCount = 1,
        Margin = new Padding(0, 10, 0, 10)
    };
    
    authorContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
    authorContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
    
    // GitHub link
    Label authorLabel = new Label
    {
        Text = "github.com/scottpeterman",
        Font = new Font(dosFont.FontFamily, 9),
        ForeColor = accentColor,
        TextAlign = ContentAlignment.BottomCenter,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 0, 0, 5),
        BackColor = bgColor,
        Cursor = Cursors.Hand
    };
    
    // Copyright info
    Label copyrightLabel = new Label
    {
        Text = "Scott Peterman © 2025",
        Font = new Font(dosFont.FontFamily, 10),
        ForeColor = accentColor,
        TextAlign = ContentAlignment.TopCenter,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 5, 0, 0),
        BackColor = bgColor
    };
    
    authorContainer.Controls.Add(authorLabel, 0, 0);
    authorContainer.Controls.Add(copyrightLabel, 0, 1);
    
    // Handle GitHub link click
    authorLabel.Click += (s, e) => {
        try {
            System.Diagnostics.Process.Start("https://github.com/scottpeterman");
        }
        catch (Exception ex) {
            MessageBox.Show($"Could not open the link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    };
    
    TableLayoutPanel folderLinksContainer = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        BackColor = bgColor,
        RowCount = 2,
        ColumnCount = 1,
        Margin = new Padding(0, 0, 0, 0)
    };
    
    folderLinksContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
    folderLinksContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
    
    // Themes folder link
    Label themesFolderLink = new Label
    {
        Text = "Themes Folder",
        Font = new Font(dosFont.FontFamily, 9, FontStyle.Underline),
        ForeColor = accentColor,
        TextAlign = ContentAlignment.BottomCenter,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 0, 0, 5),
        BackColor = bgColor,
        Cursor = Cursors.Hand
    };
    
    // Sessions folder link
    Label sessionsFolderLink = new Label
    {
        Text = "Sessions Folder",
        Font = new Font(dosFont.FontFamily, 9, FontStyle.Underline),
        ForeColor = accentColor,
        TextAlign = ContentAlignment.TopCenter,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 5, 0, 0),
        BackColor = bgColor,
        Cursor = Cursors.Hand
    };
    
    folderLinksContainer.Controls.Add(themesFolderLink, 0, 0);
    folderLinksContainer.Controls.Add(sessionsFolderLink, 0, 1);
    
    // Themes folder click handler
    themesFolderLink.Click += (s, e) => {
        try {
            string themesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RetroTerm.NET", "Themes");
                
            // Create the folder if it doesn't exist
            if (!Directory.Exists(themesPath))
                Directory.CreateDirectory(themesPath);
                
            // Open the folder in File Explorer
            System.Diagnostics.Process.Start("explorer.exe", themesPath);
        }
        catch (Exception ex) {
            MessageBox.Show($"Could not open themes folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    };
    
    // Sessions folder click handler
    sessionsFolderLink.Click += (s, e) => {
        try {
            string sessionsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "sessions");
                
            // Create the folder if it doesn't exist
            if (!Directory.Exists(sessionsPath))
                Directory.CreateDirectory(sessionsPath);
                
            // Open the folder in File Explorer
            System.Diagnostics.Process.Start("explorer.exe", sessionsPath);
        }
        catch (Exception ex) {
            MessageBox.Show($"Could not open sessions folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    };
    
    // Button panel
    Panel buttonPanel = new Panel
    {
        Dock = DockStyle.Fill,
        BackColor = bgColor,
        Margin = new Padding(0, 0, 0, 0)
    };
    
    // OK button
    Button okButton = new Button
    {
        Text = "OK",
        BackColor = buttonColor,
        ForeColor = buttonTextColor,
        FlatStyle = FlatStyle.Flat,
        Font = new Font(dosFont.FontFamily, 10, FontStyle.Bold),
        
        Size = new Size(120, 50),
        Location = new Point((buttonPanel.Width - 120) / 2, 15),
        DialogResult = DialogResult.OK,
        UseVisualStyleBackColor = false,
        AutoSize = false
    };
    
    okButton.FlatAppearance.BorderSize = 1;
    okButton.FlatAppearance.BorderColor = textColor;
    
    buttonPanel.Controls.Add(okButton);
    
    buttonPanel.Layout += (s, e) => {
        okButton.Location = new Point((buttonPanel.Width - okButton.Width) / 2, 15);
    };
    
    // Motto label (uncommented)
    Label mottoLabel = new Label
    {
        Text = "CLI's Never Die, They Just Change Their Prompt",
        Font = new Font(dosFont.FontFamily, 9, FontStyle.Italic),
        ForeColor = accentColor,
        TextAlign = ContentAlignment.TopCenter,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 0, 0, 5),
        BackColor = bgColor
    };
    
    // Add all controls to main layout
    mainLayout.Controls.Add(titleLabel, 0, 0);
    mainLayout.Controls.Add(descriptionLabel, 0, 1);
    mainLayout.Controls.Add(authorContainer, 0, 2);
    mainLayout.Controls.Add(folderLinksContainer, 0, 3); // Add the folder links container
    mainLayout.Controls.Add(buttonPanel, 0, 4);
    mainLayout.Controls.Add(mottoLabel, 0, 5); // Using the motto now
    
    aboutBox.Controls.Add(mainLayout);
    
    // Add border
    aboutBox.Paint += (s, e) => {
        // Draw double border around form
        Color borderColor = textColor;
        Rectangle rect = new Rectangle(0, 0, aboutBox.Width - 1, aboutBox.Height - 1);
        e.Graphics.DrawRectangle(new Pen(borderColor, 2), rect);
        
        // Inner border
        Rectangle innerRect = new Rectangle(4, 4, aboutBox.Width - 9, aboutBox.Height - 9);
        e.Graphics.DrawRectangle(new Pen(borderColor), innerRect);
    };
    
    // Make form draggable
    aboutBox.MouseDown += (s, e) => {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(aboutBox.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }
    };
    
    // Add close button in upper right corner
    Button closeButton = new Button
    {
        Text = "×",
        TextAlign = ContentAlignment.MiddleCenter,
        FlatStyle = FlatStyle.Flat,
        Size = new Size(26, 26),
        Location = new Point(aboutBox.Width - 32, 6),
        ForeColor = textColor,
        BackColor = bgColor,
        Font = new Font("Arial", 12, FontStyle.Bold),
        Cursor = Cursors.Hand
    };
    
    closeButton.FlatAppearance.BorderSize = 0;
    closeButton.Click += (s, e) => aboutBox.Close();
    
    aboutBox.Controls.Add(closeButton);
    
    // Apply theme
    if (themeManager?.CurrentTheme != null)
    {
        ApplyThemeToContainer(aboutBox, themeManager.CurrentTheme);
    }
    
    // Show dialog
    aboutBox.ShowDialog(this);
}
        private void HandleQuit()
        {
            // Check for connected tabs
            bool hasConnectedTabs = false;
            foreach (TabPage page in tabControl.TabPages)
            {
                if (page is TerminalTabPage terminalTab && terminalTab.IsConnected)
                {
                    hasConnectedTabs = true;
                    break;
                }
            }
            
            // Confirm exit if connected
            if (hasConnectedTabs)
            {
                DialogResult result = MessageBox.Show(
                    "You have active connections. Are you sure you want to quit?",
                    "Confirm Exit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                    
                if (result != DialogResult.Yes)
                {
                    return;
                }
            }
            
            // Save settings and exit
            SaveSettings();
            this.Close();
        }
        
        private void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKey))
                {
                    if (key != null)
                    {
                        // Load application settings
                        settingsManager.LastThemeName = key.GetValue("LastThemeName", "Borland Classic")?.ToString() ?? "Borland Classic";
                        settingsManager.EnableModemSound = Convert.ToInt32(key.GetValue("EnableModemSound", 0) ?? 0) == 1;
                        settingsManager.FontSize = Convert.ToInt32(key.GetValue("FontSize", 10) ?? 10);
                        
                        // Load window settings
                        settingsManager.WindowWidth = Convert.ToInt32(key.GetValue("WindowWidth", this.Width) ?? this.Width);
                        settingsManager.WindowHeight = Convert.ToInt32(key.GetValue("WindowHeight", this.Height) ?? this.Height);
                        settingsManager.IsMaximized = Convert.ToInt32(key.GetValue("IsMaximized", 0) ?? 0) == 1;
                        
                        // Apply window settings
                        if (settingsManager.WindowWidth > 0 && settingsManager.WindowHeight > 0)
                        {
                            this.Width = settingsManager.WindowWidth;
                            this.Height = settingsManager.WindowHeight;
                        }
                        
                        if (settingsManager.IsMaximized)
                        {
                            this.WindowState = FormWindowState.Maximized;
                        }
                        
                        // Apply other settings
                        tabManager.EnableModemSound = settingsManager.EnableModemSound;
                        
                        // Apply theme
                        if (!string.IsNullOrEmpty(settingsManager.LastThemeName))
                        {
                            themeManager.ApplyTheme(settingsManager.LastThemeName);
                        }
                        
                        UpdateStatus("Settings loaded");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                // If settings failed to load, use defaults
                UpdateStatus("Using default settings");
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                // Update settings from current state
                settingsManager.LastThemeName = themeManager.CurrentTheme?.Name ?? "Borland Classic";
                
                if (this.WindowState == FormWindowState.Normal)
                {
                    settingsManager.WindowWidth = this.Width;
                    settingsManager.WindowHeight = this.Height;
                    settingsManager.IsMaximized = false;
                }
                else if (this.WindowState == FormWindowState.Maximized)
                {
                    settingsManager.IsMaximized = true;
                }
                
                // Save to registry
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKey))
                {
                    if (key != null)
                    {
                        key.SetValue("LastThemeName", settingsManager.LastThemeName);
                        key.SetValue("EnableModemSound", settingsManager.EnableModemSound ? 1 : 0);
                        key.SetValue("FontSize", settingsManager.FontSize);
                        key.SetValue("WindowWidth", settingsManager.WindowWidth);
                        key.SetValue("WindowHeight", settingsManager.WindowHeight);
                        key.SetValue("IsMaximized", settingsManager.IsMaximized ? 1 : 0);
                        
                        UpdateStatus("Settings saved");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
                UpdateStatus("Failed to save settings");
            }
        }
        
        private void UpdateStatus(string message)
{
    if (statusLabel != null && statusPanel != null)
    {
        // Force a clean redraw
        statusPanel.SuspendLayout();
        
        // Clear the panel's background and force redraw
        using (Graphics g = statusPanel.CreateGraphics())
        {
            g.Clear(statusPanel.BackColor);
        }
        
        // Set new text
        statusLabel.Text = message;
        
        // Resume layout and force immediate update
        statusPanel.ResumeLayout(true);
        statusPanel.Refresh();
    }
}
       
       
private void ApplyCurrentTheme()
{
    // Get current theme
    var theme = themeManager.CurrentTheme;
    if (theme == null) return;
    
    // Apply theme to form elements
    this.BackColor = Theme.HexToColor(theme.UI.Background);
    this.ForeColor = Theme.HexToColor(theme.UI.Text);
    
    // Update menu container and menu strip
    Control[] menuContainers = this.Controls.Find("menuContainer", true);
    if (menuContainers.Length > 0 && menuContainers[0] is Panel menuContainer)
    {
        menuContainer.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
    }
    
    if (menuStrip != null)
    {
        menuStrip.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
        menuStrip.ForeColor = Theme.HexToColor(theme.UI.MenuText);
        
        // Update menu items
        foreach (ToolStripItem item in menuStrip.Items)
        {
            if (item is ToolStripMenuItem menuItem)
            {
                menuItem.ForeColor = Theme.HexToColor(theme.UI.MenuText);
                menuItem.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
            }
        }
    }
    
    Panel tabContainer = new Panel
{
    Dock = DockStyle.Fill,
    Padding = new Padding(4),
    Margin = new Padding(0),
    BackColor = Theme.HexToColor(themeManager.CurrentTheme?.UI.Background ?? "#F0F0F0")
};
    // Update toolbar
    Control[] toolbarContainers = this.Controls.Find("toolbarContainer", true);
    if (toolbarContainers.Length > 0 && toolbarContainers[0] is TableLayoutPanel toolbarContainer)
    {
        toolbarContainer.BackColor = Theme.HexToColor(theme.UI.Background);
        
        // Update toolbar buttons
        foreach (Control control in toolbarContainer.Controls)
        {
            if (control is TableLayoutPanel buttonPanel)
            {
                foreach (Control button in buttonPanel.Controls)
                {
                    if (button is Button toolButton)
                    {
                        toolButton.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
                        toolButton.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
                        toolButton.FlatAppearance.BorderColor = Theme.HexToColor(theme.UI.Border);
                    }
                }
            }
        }
    }
    
    // Update status panel and label
    if (statusPanel != null)
    {
        statusPanel.BackColor = Theme.HexToColor(theme.UI.StatusBarBackground);
        statusPanel.ForeColor = Theme.HexToColor(theme.UI.StatusBarText);
        
        
        if (statusLabel != null)
        {
            statusLabel.BackColor = Theme.HexToColor(theme.UI.StatusBarBackground);
            statusLabel.ForeColor = Theme.HexToColor(theme.UI.StatusBarText);
        }
    }
    
    // Update function key area
    Control[] functionKeyPanels = this.Controls.Find("functionKeyPanel", true);
    if (functionKeyPanels.Length > 0 && functionKeyPanels[0] is Panel functionKeyPanel)
    {
        functionKeyPanel.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
        
        // Update all controls within the function key panel
        foreach (Control control in functionKeyPanel.Controls)
        {
            if (control is FlowLayoutPanel flowPanel)
            {
                flowPanel.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                
                // Update each function key label
                foreach (Control keyControl in flowPanel.Controls)
                {
                    if (keyControl is Label keyLabel)
                    {
                        keyLabel.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                        keyLabel.Invalidate(); // Force repaint with the new theme
                    }
                }
            }
            else if (control is Button button)
            {
                button.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
                button.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
                button.FlatAppearance.BorderColor = Theme.HexToColor(theme.UI.Border);
            }
        }
    }
    
    // Update close button
    if (closeButton != null)
    {
        closeButton.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
        closeButton.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
        closeButton.FlatAppearance.BorderColor = Theme.HexToColor(theme.UI.Border);
    }
    
    // Apply theme to tab manager (which handles terminal tabs)
    tabManager.ApplyThemeToAllTabs();
    
    // Force redraw
    this.Invalidate(true);
}

private void DebugUI()
{
    // themeManager.DebugControlHierarchy(this);
}


// Update InitializePasswordManager in MainForm.cs

private void InitializePasswordManager()
{
    try
    {
        // Initialize the password manager
        bool initializationResult = false;
        bool resetRequested = false;
        
        do {
            // Attempt to initialize
            initializationResult = PasswordManager.Initialize(this);
            
            // If initialization failed, it might be due to wrong password or user cancelation
            if (!initializationResult)
            {
                // If the initialization failed, check if the user wants to reset
                DialogResult result = MessageBox.Show(
                    "Authentication failed or was canceled.\n\n" +
                    "Would you like to reset your password?\n" +
                    "(Note: This will require re-entering all your stored passwords)",
                    "Authentication Failed",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
                    
                if (result == DialogResult.Yes)
                {
                    // User wants to reset
                    resetRequested = true;
                    initializationResult = PasswordManager.ResetEncryption(this);
                }
                else // User chose No or Cancel
                {
                    // Display message about shutting down
                    MessageBox.Show(
                        "Password authentication is required to use encrypted storage features.\n" +
                        "The application will now exit.",
                        "Authentication Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    
                    // Force application shutdown
                    this.BeginInvoke(new Action(() => {
                        this.Close();
                        Environment.Exit(0);
                    }));
                    return; // Exit the method to prevent further processing
                }
            }
        } while (!initializationResult && resetRequested);
        
        // The rest of your method remains the same...
    }
    catch (Exception ex)
    {
        // Exception handling remains the same...
    }
}


protected override void OnLoad(EventArgs e)
{
    base.OnLoad(e);
    
    // Force a specific height
    int targetHeight = 600; // Adjust this value as needed
    int targetWidth = 1000; // Adjust this value as needed
    this.Height = targetHeight;
    this.Width = targetWidth;
    
    // Rest of your existing OnLoad code
    this.BeginInvoke(new Action(() => {
        themeManager.ApplyThemeToForm(this);
        themeManager.FixTabPageBackgrounds(this);
        FixAllTabBackgrounds();
        this.Invalidate(true);
    }));
}

private void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e)
{
    // Apply the new theme to the entire form
    themeManager.ApplyThemeToForm(this);
    themeManager.UpdateSplitter(navigatorSplitter);

    
    // Specifically fix TabPage backgrounds
    themeManager.FixTabPageBackgrounds(this);
    
    // Apply theme to tab manager (which handles terminal tabs)
    tabManager.ApplyThemeToAllTabs();
    
    
        FixAllTabBackgrounds();

        if (statusPanel != null)
    {
        statusPanel.BackColor = Theme.HexToColor(themeManager.CurrentTheme.UI.StatusBarBackground);
        statusPanel.ForeColor = Theme.HexToColor(themeManager.CurrentTheme.UI.StatusBarText);
        statusPanel.Invalidate(true);
    }
    
    if (statusLabel != null)
    {
        statusLabel.BackColor = Theme.HexToColor(themeManager.CurrentTheme.UI.StatusBarBackground);
        statusLabel.ForeColor = Theme.HexToColor(themeManager.CurrentTheme.UI.StatusBarText);
        statusLabel.Invalidate(true);
    }

    // Force redraw
    this.Invalidate(true);
}

        private void TabManager_TabAdded(object sender, TabEventArgs e)
        {
            // Update UI for new tab
            UpdateStatus($"New tab created: {e.Tab.Text}");
        }
        
        private void TabManager_TabRemoved(object sender, TabEventArgs e)
        {
            // Update UI for removed tab
            UpdateStatus($"Tab closed: {e.Tab.Text}");
        }
        
        private void TabManager_TabSelected(object sender, TabEventArgs e)
        {
            // Update UI for selected tab
            UpdateStatus(e.Tab.IsConnected ? 
                $"Connected to {e.Tab.Host}:{e.Tab.Port}" : "Not connected");
        }

protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
{
    // Handle function keys
    switch (keyData)
    {
        case Keys.F1:
            ShowAboutDialog();
            return true;
            
        case Keys.F2:
            // Handle Save functionality
            UpdateStatus("Save function triggered");
            return true;
            
        case Keys.F3:
            // Toggle sessions navigator
            ToggleNavigatorPanel_Click(null, EventArgs.Empty);
            return true;
            
        case Keys.F4:
            ShowConnectionDirectory();
            return true;
        
        case Keys.F9:
            HandleQuit();
            return true;
            
        case Keys.F10:
            // Handle Menu functionality
            menuStrip.Focus();
            if (menuStrip.Items.Count > 0)
            {
                ((ToolStripMenuItem)menuStrip.Items[0]).ShowDropDown();
            }
            UpdateStatus("Menu activated");
            return true;
        
        case Keys.Control | Keys.T:
            NewTab_Click(null, EventArgs.Empty);
            return true;
        
        case Keys.Control | Keys.W:
            CloseTab_Click(null, EventArgs.Empty);
            return true;
    }
    
    return base.ProcessCmdKey(ref msg, keyData);
}

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Save settings before closing
            SaveSettings();
            
            base.OnFormClosing(e);
        }
    }
}
    
    // Custom renderer class for retro DOS style menus
public class RetroMenuRenderer : ToolStripProfessionalRenderer
{
    private readonly ThemeManager themeManager;
    
    public RetroMenuRenderer(ThemeManager themeManager)
    {
        this.themeManager = themeManager;
    }
    
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        // Get theme colors or use fallback colors if theme is not available
        Color selectedBgColor = Color.FromArgb(0, 0, 170); // Fallback to Borland Blue
        Color selectedFgColor = Color.White;
        Color normalBgColor = Color.Silver;
        Color normalFgColor = Color.Black;
        
        // Use theme colors if available
        if (themeManager?.CurrentTheme != null)
        {
            var theme = themeManager.CurrentTheme;
            selectedBgColor = Theme.HexToColor(theme.UI.MenuHighlight);
            selectedFgColor = Theme.HexToColor(theme.UI.MenuHighlightText);
            normalBgColor = Theme.HexToColor(theme.UI.MenuBackground);
            normalFgColor = Theme.HexToColor(theme.UI.MenuText);
        }
        
        // For main menu items
        if (e.Item.Owner is MenuStrip)
        {
            if (e.Item.Selected)
            {
                // Selected main menu items
                using (SolidBrush brush = new SolidBrush(selectedBgColor))
                {
                    e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                }
                e.Item.ForeColor = selectedFgColor;
            }
            else
            {
                // Non-selected main menu items
                using (SolidBrush brush = new SolidBrush(normalBgColor))
                {
                    e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                }
                e.Item.ForeColor = normalFgColor;
            }
            return;
        }
        
        // For dropdown menu items
        if (e.Item.Selected)
        {
            // Selected dropdown items
            using (SolidBrush brush = new SolidBrush(selectedBgColor))
            {
                e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
            }
            e.Item.ForeColor = selectedFgColor;
        }
        else
        {
            // Non-selected dropdown items
            using (SolidBrush brush = new SolidBrush(normalBgColor))
            {
                e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
            }
            e.Item.ForeColor = normalFgColor;
        }
    }
    
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (e.Item.Tag == null || !(e.Item.Tag is int) || (int)e.Item.Tag < 0)
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
        
        // Determine colors based on the current theme
        Color textColor = e.Item.Selected ? Color.White : Color.Black;
        Color hotkeyColor = Color.Red; // Default for hotkey
        
        // Use theme colors if available
        if (themeManager?.CurrentTheme != null)
        {
            var theme = themeManager.CurrentTheme;
            textColor = e.Item.Selected ? 
                Theme.HexToColor(theme.UI.MenuHighlightText) : 
                Theme.HexToColor(theme.UI.MenuText);
            hotkeyColor = Theme.HexToColor(theme.UI.HotkeyText);
        }
        
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
                    x += e.Graphics.MeasureString(prefix, e.TextFont, 
                        e.TextRectangle.Width, format).Width;
                }
                
                // Draw the hotkey character
                e.Graphics.DrawString(hotkey, e.TextFont, hotkeyBrush, x, y, format);
                x += e.Graphics.MeasureString(hotkey, e.TextFont, 
                    e.TextRectangle.Width, format).Width;
                
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