using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Collections.Generic;  
using System.Text.Json;
using System.Text.Json.Serialization;
namespace SshTerminalComponent
{
    /// <summary>
    /// A reusable SSH terminal control for .NET applications
    /// </summary>
    public class SshTerminalControl : UserControl, IDisposable
    {
        public event EventHandler TerminalReady;
        public bool WebViewInitialized => webViewInitialized?.Task.IsCompleted ?? false;

        public string TabId { get; private set; }


        private WebView2 webView;
        private SshConnectionService sshService;
        private uint currentCols;
        private uint currentRows;
        private bool disposed = false;
        private TaskCompletionSource<bool> webViewInitialized;
        // sshService.TerminalError += (sender, e) => OnTerminalError(e);
        // sshService.TerminalResized += (sender, e) => OnTerminalResized(e);

        public void SshService_ConnectionStateChanged(object sender, SshConnectionEventArgs e)
{
    // Forward the event to the control's subscribers
    OnConnectionStateChanged(e);
    
    // If disconnected, update the UI
    if (!e.IsConnected && webView?.CoreWebView2 != null)
    {
        webView.CoreWebView2.ExecuteScriptAsync("handleDisconnect()").ConfigureAwait(false);
    }
}
        
        /// <summary>
        /// Gets or sets the terminal settings
        /// </summary>
        public TerminalSettings TerminalSettings { get; private set; }
        
        /// <summary>
        /// Gets whether the terminal is connected to an SSH server
        /// </summary>
        public bool IsConnected => sshService?.IsConnected ?? false;
        
        /// <summary>
        /// Event raised when the connection state changes
        /// </summary>
        public event EventHandler<SshConnectionEventArgs> ConnectionStateChanged;
        
        /// <summary>
        /// Event raised when a terminal error occurs
        /// </summary>
        public event EventHandler<SshTerminalErrorEventArgs> TerminalError;
        
        /// <summary>
        /// Event raised when the terminal is resized
        /// </summary>
        public event EventHandler<TerminalResizeEventArgs> TerminalResized;
        
        /// <summary>
        /// Gets or sets the current terminal theme colors
        /// </summary>
        public TerminalThemeColors ThemeColors { get; private set; }


        /// <summary>
        /// Attempts to load theme.json from various locations
        /// </summary>
        /// <returns>True if theme was loaded successfully</returns>
        public bool TryLoadThemeJson()
        {
            // Try to find theme.json in several locations
            var possibleLocations = new List<string>();

            // First look in the terminal directory
            string terminalDir = null;
            try
            {
                terminalDir = GetTerminalDirectory();
                possibleLocations.Add(Path.Combine(terminalDir, "theme.json"));
            }
            catch
            {
                // If terminal directory can't be determined yet, try other locations
            }

            // Look in the assembly directory
            string assemblyLocation = Path.GetDirectoryName(typeof(SshTerminalControl).Assembly.Location);
            possibleLocations.Add(Path.Combine(assemblyLocation, "theme.json"));

            // Look in the application's base directory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            possibleLocations.Add(Path.Combine(baseDir, "theme.json"));

            // Look in the current directory
            string currentDir = Directory.GetCurrentDirectory();
            possibleLocations.Add(Path.Combine(currentDir, "theme.json"));

            // Try each location
            foreach (string themePath in possibleLocations)
            {
                if (File.Exists(themePath))
                {
                    LogDebug($"Found theme.json at: {themePath}");
                    if (LoadThemeFromFile(themePath))
                    {
                        return true;
                    }
                }
            }

            LogDebug("Could not find or load theme.json");
            return false;
        }

        /// <summary>
        /// Loads theme from the specified JSON file
        /// </summary>
        /// <param name="themePath">Path to the theme.json file</param>
        /// <returns>True if theme was loaded successfully</returns>
        public bool LoadThemeFromFile(string themePath)
        {
            try
            {
                string themeJson = File.ReadAllText(themePath);
                ThemeColors = JsonSerializer.Deserialize<TerminalThemeColors>(themeJson);
                LogDebug($"Successfully loaded theme from {themePath}");
                return true;
            }
            catch (Exception ex)
            {
                LogDebug($"Error loading theme from {themePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
/// Executes a JavaScript script in the terminal
/// </summary>
public async Task<string> ExecuteScriptAsync(string script)
{
    if (webView?.CoreWebView2 == null) 
        throw new InvalidOperationException("WebView not initialized");
        
    // Wait for WebView to be initialized
    await webViewInitialized.Task;
    
    // Execute the script
    return await webView.CoreWebView2.ExecuteScriptAsync(script);
}

        /// <summary>
        /// Applies the current theme colors to the terminal
        /// </summary>
        public async Task ApplyThemeColorsAsync()
        {
            if (webView?.CoreWebView2 == null) return;

            // Wait for WebView to be initialized
            await webViewInitialized.Task;

            // Convert the theme to a dictionary
            var themeDictionary = ThemeColors.ToDictionary();

            // Convert the theme to JSON
            string themeJson = JsonSerializer.Serialize(themeDictionary);

            // Apply theme to the terminal
            await webView.CoreWebView2.ExecuteScriptAsync($"applyTerminalTheme({themeJson})");
        }



        /// <summary>
        /// Initializes a new instance of the SshTerminalControl class
        /// </summary>
        /// 
        /// <summary>
/// Initializes a new instance of the SshTerminalControl class
/// </summary>
public SshTerminalControl(string tabId = null)
{
    // Initialize Tab ID (generate one if not provided)
    TabId = tabId ?? Guid.NewGuid().ToString();
    LogDebug($"Creating terminal control with Tab ID: {TabId}");
    
    // Initialize components
    InitializeComponent();
    AttachWebViewEvents();
    
    // Create settings
    TerminalSettings = new TerminalSettings();
    TerminalSettings.SettingsChanged += TerminalSettings_SettingsChanged;
    
    // Create SSH service with Tab ID
    sshService = new SshConnectionService(TabId);
    sshService.DataReceived += SshService_DataReceived;
    sshService.ConnectionStateChanged += SshService_ConnectionStateChanged;

    // Initialize WebView2
    webViewInitialized = new TaskCompletionSource<bool>();
    InitializeWebView();
}
/// <summary>
/// Sets specific terminal colors and applies them asynchronously
/// </summary>
/// <param name="background">Background color in hex format (#RRGGBB)</param>
/// <param name="foreground">Foreground color in hex format (#RRGGBB)</param>
/// <param name="cursor">Cursor color in hex format (#RRGGBB)</param>
/// <param name="scrollbarBackground">Scrollbar background color in hex format (#RRGGBB)</param>
/// <param name="scrollbarThumb">Scrollbar thumb color in hex format (#RRGGBB)</param>
/// <returns>A task representing the asynchronous operation</returns>
public async Task SetTerminalColorsAsync(string background, string foreground, string cursor, 
                                         string scrollbarBackground, string scrollbarThumb)
{
    // Update the theme colors
    ThemeColors.Background = background;
    ThemeColors.Foreground = foreground;
    ThemeColors.Cursor = cursor;
    ThemeColors.ScrollbarBackground = scrollbarBackground;
    ThemeColors.ScrollbarThumb = scrollbarThumb;
    
    // Log the color change
    LogDebug($"Setting terminal colors - Background: {background}, Foreground: {foreground}, Cursor: {cursor}");
    
    // Use the existing method to apply the updated theme colors
    await ApplyThemeColorsAsync();
}

private async void InitializeWebView()
{
    try
    {
        LogDebug($"Starting WebView2 initialization for tab {TabId}");

        // Ensure ThemeColors is initialized first
        if (ThemeColors == null)
        {
            ThemeColors = new TerminalThemeColors();
            LogDebug("Created default ThemeColors during WebView initialization");
        }

        // Create a user data folder in a non-restricted location
        string userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SshTerminalComponentData");

        // Ensure directory exists
        if (!Directory.Exists(userDataFolder))
        {
            Directory.CreateDirectory(userDataFolder);
            LogDebug($"Created user data folder: {userDataFolder}");
        }

        // Initialize WebView2 with custom user data folder
        var options = new CoreWebView2EnvironmentOptions();
        LogDebug("Creating CoreWebView2Environment");
        var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
        LogDebug("Ensuring CoreWebView2");
        await webView.EnsureCoreWebView2Async(environment);

        // Configure WebView2
        LogDebug("Configuring CoreWebView2");
        webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        // Configure WebView2 permissions
        webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
        webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
        webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
        webView.CoreWebView2.Settings.IsScriptEnabled = true;

        // Inject the tab ID BEFORE loading terminal content
        LogDebug($"Injecting Tab ID {TabId} into WebView2");
        await webView.CoreWebView2.ExecuteScriptAsync($"window.tabId = '{TabId}';");
        
        // Now add an event handler to make sure our tab ID persists across page loads
        webView.CoreWebView2.DOMContentLoaded += async (sender, e) => {
            LogDebug($"DOM loaded - re-injecting Tab ID {TabId}");
            await webView.CoreWebView2.ExecuteScriptAsync($"window.tabId = '{TabId}';");
        };

        // Try to load theme.json before loading the terminal
        LogDebug("Attempting to load theme.json");
        TryLoadThemeJson();

        // Load terminal HTML
        LogDebug("Loading terminal from resources");
        await LoadTerminalFromResources();

        // Signal that WebView is initialized BEFORE applying settings
        LogDebug("WebView initialization complete");
        webViewInitialized.TrySetResult(true);

        // Apply settings after marking initialization complete
        LogDebug("Applying terminal settings");
        await ApplyTerminalSettingsInternal(); // Use a different method that doesn't wait for initialization
        LogDebug("Applying theme colors");
        await ApplyThemeColorsInternal(); // Use a different method that doesn't wait for initialization
    }
    catch (Exception ex)
    {
        LogDebug($"WebView initialization error for tab {TabId}: {ex.GetType().Name}: {ex.Message}");
        webViewInitialized.TrySetException(ex);
        OnTerminalError(new SshTerminalErrorEventArgs("Error initializing WebView2", ex));
    }
}

public async Task SetupCustomContextMenu()
{
    if (webView?.CoreWebView2 == null) return;
    
    // Wait for WebView to be initialized
    await webViewInitialized.Task;
    
    // Call the JavaScript function to set up the custom context menu
    await webView.CoreWebView2.ExecuteScriptAsync("setupCustomContextMenu()");
}

// Add these helper methods that don't wait for webViewInitialized
private async Task ApplyTerminalSettingsInternal()
{
    if (webView?.CoreWebView2 == null) return;
    
    // Ensure ThemeColors is initialized
    if (ThemeColors == null)
    {
        ThemeColors = new TerminalThemeColors();
        LogDebug("Created default ThemeColors in ApplyTerminalSettingsInternal");
    }

    // Create a settings object for JavaScript (same as in ApplyTerminalSettings)
    string settingsJson = System.Text.Json.JsonSerializer.Serialize(new
    {
        fontSize = TerminalSettings.FontSize,
        fontFamily = TerminalSettings.FontFamily,
        cursorBlink = TerminalSettings.CursorBlink,
        scrollback = TerminalSettings.ScrollbackSize,
        isDarkTheme = TerminalSettings.Theme == TerminalTheme.Dark,
        colors = new
        {
            background = ThemeColors.Background,
            foreground = ThemeColors.Foreground,
            cursor = ThemeColors.Cursor,
            scrollbarBackground = ThemeColors.ScrollbarBackground,
            scrollbarThumb = ThemeColors.ScrollbarThumb
        }
    });

    // Apply settings to the terminal
    LogDebug($"Sending terminal settings to WebView: {settingsJson}");
    await webView.CoreWebView2.ExecuteScriptAsync($"applyTerminalSettings({settingsJson})");
}

private async Task ApplyThemeColorsInternal()
{
    if (webView?.CoreWebView2 == null) return;
    
    // Ensure ThemeColors is initialized
    if (ThemeColors == null)
    {
        ThemeColors = new TerminalThemeColors();
        LogDebug("Created default ThemeColors in ApplyThemeColorsInternal");
    }

    // Convert the theme to a dictionary
    var themeDictionary = ThemeColors.ToDictionary();

    // Convert the theme to JSON
    string themeJson = JsonSerializer.Serialize(themeDictionary);

    // Apply theme to the terminal
    await webView.CoreWebView2.ExecuteScriptAsync($"applyTerminalTheme({themeJson})");
}

        private void AttachWebViewEvents()
{
    if (webView == null) return;
    
    // Handle navigation completion to detect when terminal.html is fully loaded
    webView.CoreWebView2InitializationCompleted += (sender, e) => 
    {
        LogDebug($"CoreWebView2 initialization completed. Success: {e.IsSuccess}");
        if (!e.IsSuccess)
        {
            LogDebug($"CoreWebView2 initialization failed: {e.InitializationException?.Message}");
            webViewInitialized.TrySetException(e.InitializationException ?? new Exception("WebView2 initialization failed"));
        }
    };
    
    webView.NavigationCompleted += (sender, e) => 
    {
        LogDebug($"Navigation completed. Success: {e.IsSuccess}, WebError status: {e.WebErrorStatus}");
        if (!e.IsSuccess)
        {
            LogDebug($"Navigation failed with error: {e.WebErrorStatus}");
        }
    };
}


private void LogDebug(string message)
{   return;
    try
    {
        // Include tab ID in log message if not already present
        if (!message.Contains($"Tab {TabId}:") && !string.IsNullOrEmpty(TabId))
        {
            message = $"Tab {TabId}: {message}";
        }
        
        // Log to console
        Console.WriteLine($"[SshTerminal] {DateTime.Now:HH:mm:ss.fff}: {message}");
        
        // Optionally log to a file
        string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SshTerminalComponentData", 
            "debug.log");
            
        File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {message}{Environment.NewLine}");
    }
    catch
    {
        // Ignore logging errors
    }
}
        private void InitializeComponent()
        {
            this.webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            
            this.Controls.Add(this.webView);
        }
        
        private async Task LoadTerminalFromResources()
        {
             try
    {
        // Get the terminal files path
        string terminalDir = GetTerminalDirectory();
        LogDebug($"Terminal directory found at: {terminalDir}");
        
        // Verify terminal.html exists
        string terminalHtmlPath = Path.Combine(terminalDir, "terminal.html");
        if (!File.Exists(terminalHtmlPath))
        {
            LogDebug($"Terminal HTML file not found at: {terminalHtmlPath}");
            throw new FileNotFoundException($"Terminal HTML file not found at: {terminalHtmlPath}");
        }
        
        LogDebug("Setting virtual host mapping for terminal directory");
        // Create virtual host mapping for the terminal folder
        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "terminalapp", terminalDir, CoreWebView2HostResourceAccessKind.Allow);
        
        // Navigate to the HTML file using the virtual host
        LogDebug("Navigating to terminal.html");
        webView.CoreWebView2.Navigate("https://terminalapp/terminal.html");
    }
    catch (Exception ex)
    {
        LogDebug($"Error loading terminal resources: {ex.Message}");
        OnTerminalError(new SshTerminalErrorEventArgs("Error loading terminal resources", ex));
        throw;
    }
}

private string GetTerminalDirectory()
{
    // List possible terminal directory locations
    var possibleLocations = new List<string>();
    
    // First try to find terminal folder next to the assembly
    string assemblyLocation = Path.GetDirectoryName(typeof(SshTerminalControl).Assembly.Location);
    string terminalDir = Path.Combine(assemblyLocation, "terminal");
    possibleLocations.Add(terminalDir);
    
    if (Directory.Exists(terminalDir))
    {
        LogDebug($"Terminal directory found at: {terminalDir}");
        return terminalDir;
    }
    
    // If not found, try to find it in the application's base directory
    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
    terminalDir = Path.Combine(baseDir, "terminal");
    possibleLocations.Add(terminalDir);
    
    if (Directory.Exists(terminalDir))
    {
        LogDebug($"Terminal directory found at: {terminalDir}");
        return terminalDir;
    }
    
    // Try other possible locations
    string currentDir = Directory.GetCurrentDirectory();
    terminalDir = Path.Combine(currentDir, "terminal");
    possibleLocations.Add(terminalDir);
    
    if (Directory.Exists(terminalDir))
    {
        LogDebug($"Terminal directory found at: {terminalDir}");
        return terminalDir;
    }
    
    // Log all checked locations
    LogDebug($"Terminal directory not found in any of the following locations:");
    foreach (var location in possibleLocations)
    {
        LogDebug($" - {location}");
    }
    
    // If still not found, throw an exception
    throw new DirectoryNotFoundException(
        "Terminal directory not found. Make sure the 'terminal' folder is included in the output directory and contains terminal.html. Locations checked: " + 
        string.Join(", ", possibleLocations));
        }
        
private async Task ApplyTerminalSettings()
{
    if (webView?.CoreWebView2 == null) return;

    // Wait for WebView to be initialized
    await webViewInitialized.Task;

    // Create a settings object for JavaScript
    string settingsJson = System.Text.Json.JsonSerializer.Serialize(new
    {
        fontSize = TerminalSettings.FontSize,
        fontFamily = TerminalSettings.FontFamily,
        cursorBlink = TerminalSettings.CursorBlink,
        scrollback = TerminalSettings.ScrollbackSize,
        isDarkTheme = TerminalSettings.Theme == TerminalTheme.Dark,
        // Include basic color settings from ThemeColors for backward compatibility
        colors = new
        {
            background = ThemeColors.Background,
            foreground = ThemeColors.Foreground,
            cursor = ThemeColors.Cursor,
            scrollbarBackground = ThemeColors.ScrollbarBackground,
            scrollbarThumb = ThemeColors.ScrollbarThumb
        }
    });

    // Apply settings to the terminal
    LogDebug($"Sending terminal settings to WebView: {settingsJson}");
    await webView.CoreWebView2.ExecuteScriptAsync($"applyTerminalSettings({settingsJson})");
}

private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
{
    string message = e.TryGetWebMessageAsString();
    
    // Fix for string interpolation with conditional expression
    string logMessage;
    if (message.Length > 100) {
        logMessage = message.Substring(0, 100) + "...";
    } else {
        logMessage = message;
    }
    LogDebug($"Tab {TabId}: Received message from WebView: {logMessage}");
    
    // Check if this is a JSON message
    if (message.StartsWith("{"))
    {
        try
        {
            // Parse JSON message
            var json = System.Text.Json.JsonDocument.Parse(message);
            var root = json.RootElement;
            
            // Extract tab ID if present
            string messageTabId = null;
            if (root.TryGetProperty("tabId", out var tabIdElement))
            {
                messageTabId = tabIdElement.GetString();
                
                // If message is for a different tab, ignore it
                if (!string.IsNullOrEmpty(messageTabId) && messageTabId != TabId)
                {
                    LogDebug($"Ignoring message for tab {messageTabId}, current tab is {TabId}");
                    return;
                }
            }
            
            if (root.TryGetProperty("type", out var typeElement))
            {
                string messageType = typeElement.GetString();
                LogDebug($"Tab {TabId}: Received message type: {messageType}");
                
                // Handle different message types
                switch (messageType)
                {
                    case "resize":
                        if (root.TryGetProperty("dimensions", out var dimensions))
                        {
                            uint cols = (uint)dimensions.GetProperty("cols").GetInt32();
                            uint rows = (uint)dimensions.GetProperty("rows").GetInt32();
                            HandleTerminalResize(cols, rows);
                        }
                        break;
                        
                    case "ready":
                        LogDebug($"*** Tab {TabId}: Received terminal ready message! ***");
                        // Terminal is ready, complete initialization
                        ApplyTerminalSettings().ConfigureAwait(false);
                        
                        // Complete the initialization task if it's not already completed
                        if (!webViewInitialized.Task.IsCompleted)
                        {
                            LogDebug("Setting webViewInitialized to true based on ready message");
                            webViewInitialized.TrySetResult(true);
                        }
                        
                        // Raise the TerminalReady event
                        TerminalReady?.Invoke(this, EventArgs.Empty);
                        break;
                        
                    case "terminal-input":
                        // Handle terminal input specifically for this tab
                        if (root.TryGetProperty("data", out var inputData))
                        {
                            string data = inputData.GetString();
                            LogDebug($"Tab {TabId}: Received terminal input, length: {data?.Length ?? 0}");
                            
                            // Forward the input to SSH
                            sshService?.SendData(data);
                        }
                        break;
                        
                    default:
                        // Unknown message type, log for debugging
                        LogDebug($"Tab {TabId}: Unknown message type: {messageType}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Tab {TabId}: Error handling JSON message: {ex.Message}");
            OnTerminalError(new SshTerminalErrorEventArgs("Error handling JSON message", ex));
        }
    }
    else
    {
        // For backward compatibility, handle non-JSON messages as raw terminal data
        LogDebug($"Tab {TabId}: Received raw (non-JSON) terminal input, length: {message?.Length ?? 0}");
        sshService?.SendData(message);
    }
}

        private void HandleTerminalResize(uint cols, uint rows)
        {
            // Check if dimensions actually changed
            if (cols != currentCols || rows != currentRows)
            {
                currentCols = cols;
                currentRows = rows;
                
                // Resize the SSH shell if connected
                if (IsConnected)
                {
                    sshService.ResizeShell(cols, rows);
                }
                
                // Raise the TerminalResized event
                OnTerminalResized(new TerminalResizeEventArgs(cols, rows));
            }
        }
        
private void SshService_DataReceived(object sender, SshDataEventArgs e)
{
    if (disposed || webView?.CoreWebView2 == null) return;
    
    // Use base64 encoding to avoid JavaScript escaping issues
    string base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(e.Data));
    
    // Send data to terminal using base64 with tab ID check
    if (this.InvokeRequired)
    {
        this.Invoke(new Action(async () => {
            if (webView?.CoreWebView2 != null)
            {
                LogDebug($"Tab {TabId}: Sending {e.Data.Length} bytes to terminal");
                await webView.CoreWebView2.ExecuteScriptAsync(
                    $"if(window.tabId === '{TabId}') {{ receiveTerminalDataEncoded(\"{base64Data}\"); }}");
            }
        }));
    }
    else
    {
        if (webView?.CoreWebView2 != null)
        {
            LogDebug($"Tab {TabId}: Sending {e.Data.Length} bytes to terminal");
            webView.CoreWebView2.ExecuteScriptAsync(
                $"if(window.tabId === '{TabId}') {{ receiveTerminalDataEncoded(\"{base64Data}\"); }}").ConfigureAwait(false);
        }
    }
}
        private async void TerminalSettings_SettingsChanged(object sender, EventArgs e)
        {
            await ApplyTerminalSettings();
        }
        
        /// <summary>
        /// Connects to an SSH server using password authentication
        /// </summary>
        public async Task ConnectAsync(string host, int port, string username, string password)
        {
            try
            {
                LogDebug($"Starting connection to {host}:{port} with username {username}");
        
        // Wait for WebView to be initialized
        LogDebug("Waiting for WebView initialization...");
        await webViewInitialized.Task;
        LogDebug("WebView initialization complete");
                
                // Notify the terminal that connection is starting
                await webView.CoreWebView2.ExecuteScriptAsync(
                    $"handleConnect({{host:'{host}',port:{port},username:'{username}'}})");
                
                // Connect to the SSH server
                sshService.ConnectWithPassword(host, port, username, password);
                
                // Get terminal dimensions
                await GetTerminalDimensions();
                
                // Create the shell
                sshService.CreateShell(currentCols, currentRows);
                
                // Notify the terminal that connection is established
                await webView.CoreWebView2.ExecuteScriptAsync("terminalConnected()");
                
                // Raise connection state changed event
                OnConnectionStateChanged(new SshConnectionEventArgs(true, host, port, username));
            }
            catch (Exception ex)
            {
                // Notify the terminal about the connection failure
                if (webView?.CoreWebView2 != null)
                {
                    await webView.CoreWebView2.ExecuteScriptAsync(
                        $"connectionFailed('{ex.Message.Replace("'", "\\'")}')");
                }
                
                OnTerminalError(new SshTerminalErrorEventArgs("Connection error", ex));
                throw;
            }
        }
        
        /// <summary>
        /// Connects to an SSH server using private key authentication
        /// </summary>
        public async Task ConnectWithKeyAsync(string host, int port, string username, string privateKeyFile, string passphrase = null)
        {
            try
            {
                // Wait for WebView to be initialized
                await webViewInitialized.Task;
                
                // Notify the terminal that connection is starting
                await webView.CoreWebView2.ExecuteScriptAsync(
                    $"handleConnect({{host:'{host}',port:{port},username:'{username}'}})");
                
                // Connect to the SSH server
                sshService.ConnectWithPrivateKey(host, port, username, privateKeyFile, passphrase);
                
                // Get terminal dimensions
                await GetTerminalDimensions();
                
                // Create the shell
                sshService.CreateShell(currentCols, currentRows);
                
                // Notify the terminal that connection is established
                await webView.CoreWebView2.ExecuteScriptAsync("terminalConnected()");
                
                // Raise connection state changed event
                OnConnectionStateChanged(new SshConnectionEventArgs(true, host, port, username));
            }
            catch (Exception ex)
            {
                // Notify the terminal about the connection failure
                if (webView?.CoreWebView2 != null)
                {
                    await webView.CoreWebView2.ExecuteScriptAsync(
                        $"connectionFailed('{ex.Message.Replace("'", "\\'")}')");
                }
                
                OnTerminalError(new SshTerminalErrorEventArgs("Connection error", ex));
                throw;
            }
        }
        
        private async Task GetTerminalDimensions()
        {
            try
            {
                // Execute script to get dimensions
                string script = "JSON.stringify({cols: term.cols, rows: term.rows})";
                var dimensionsJson = await webView.CoreWebView2.ExecuteScriptAsync(script);
                
                // Remove quotes that ExecuteScriptAsync adds around the result
                dimensionsJson = dimensionsJson.Trim('"');
                // Unescape JSON string
                dimensionsJson = System.Text.RegularExpressions.Regex.Unescape(dimensionsJson);
                
                // Parse dimensions
                var json = System.Text.Json.JsonDocument.Parse(dimensionsJson);
                var root = json.RootElement;
                currentCols = (uint)root.GetProperty("cols").GetInt32();
                currentRows = (uint)root.GetProperty("rows").GetInt32();
            }
            catch (Exception ex)
            {
                OnTerminalError(new SshTerminalErrorEventArgs("Error getting terminal dimensions", ex));
                
                // Use default dimensions if getting dimensions fails
                currentCols = 80;
                currentRows = 24;
            }
        }
        
        /// <summary>
        /// Disconnects from the SSH server
        /// </summary>
        public async void Disconnect()
        {
            if (IsConnected)
            {
                sshService.Disconnect();
                
                // Notify the terminal about disconnection if WebView is still available
                if (webView?.CoreWebView2 != null)
                {
                    await webView.CoreWebView2.ExecuteScriptAsync("handleDisconnect()");
                }
                
                // Raise connection state changed event
                OnConnectionStateChanged(new SshConnectionEventArgs(false, string.Empty, 0, string.Empty));
            }
        }
        
        /// <summary>
        /// Raises the ConnectionStateChanged event
        /// </summary>
        protected virtual void OnConnectionStateChanged(SshConnectionEventArgs e)
        {
            ConnectionStateChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// Raises the TerminalError event
        /// </summary>
        protected virtual void OnTerminalError(SshTerminalErrorEventArgs e)
        {
            TerminalError?.Invoke(this, e);
        }
        
        /// <summary>
        /// Raises the TerminalResized event
        /// </summary>
        protected virtual void OnTerminalResized(TerminalResizeEventArgs e)
        {
            TerminalResized?.Invoke(this, e);
        }
        
        /// <summary>
        /// Sends a command to the terminal
        /// </summary>
        public void SendCommand(string command, bool addNewLine = true)
        {
            if (!IsConnected) return;
            
            if (addNewLine && !command.EndsWith("\n"))
            {
                command += "\n";
            }
            
            sshService.SendData(command);
        }
        
        /// <summary>
        /// Clears the terminal screen
        /// </summary>
        public async Task ClearScreen()
        {
            if (webView?.CoreWebView2 == null) return;
            
            await webView.CoreWebView2.ExecuteScriptAsync("term.clear()");
        }
        
        /// <summary>
        /// Copies the current selection to the clipboard
        /// </summary>
        public async Task CopySelection()
        {
            if (webView?.CoreWebView2 == null) return;
            
            await webView.CoreWebView2.ExecuteScriptAsync("copyTerminalSelection()");
        }
        
        /// <summary>
        /// Pastes text from clipboard to the terminal
        /// </summary>
        public async Task PasteText()
        {
            if (webView?.CoreWebView2 == null) return;
            
            await webView.CoreWebView2.ExecuteScriptAsync("pasteToTerminal()");
        }
        
        /// <summary>
        /// Updates the terminal theme
        /// </summary>
        public void SetTheme(TerminalTheme theme)
        {
            TerminalSettings.Theme = theme;
        }
        
        /// <summary>
        /// Updates the terminal font size
        /// </summary>
        public void SetFontSize(int fontSize)
        {
            TerminalSettings.FontSize = fontSize;
        }
        
        /// <summary>
        /// Releases resources used by the control
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    Disconnect();
                    
                    if (sshService != null)
                    {
                        sshService.Dispose();
                        sshService = null;
                    }
                    
                    if (webView != null)
                    {
                        webView.Dispose();
                        webView = null;
                    }
                }
                
                disposed = true;
            }
            
            base.Dispose(disposing);
        }
    }
}