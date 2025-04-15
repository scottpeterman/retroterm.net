using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using RetroTerm.NET.Models;
using SshTerminalComponent;

namespace RetroTerm.NET.Controls
{
    /// <summary>
    /// A custom tab page that encapsulates a terminal control
    /// </summary>
    public class TerminalTabPage : TabPage
    {
        private Panel terminalPanel;
        public SshTerminalControl Terminal { get; private set; }
        
        // Add Tab ID property
        public string TabId { get; private set; }
        
        // Connection properties
        public string Host { get; set; }
        public int Port { get; set; } = 22;
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsConnected => Terminal?.IsConnected ?? false;
        public string ConnectionName { get; set; }
        
        // Events
        public event EventHandler<SshConnectionEventArgs> ConnectionStateChanged;
        public event EventHandler<SshTerminalErrorEventArgs> TerminalError;
        public event EventHandler<TerminalResizeEventArgs> TerminalResized;
        
        // Constructor
        public TerminalTabPage(string title = "New Terminal", string tabId = null)
        {
            // Initialize tab ID (generate one if not provided)
            TabId = tabId ?? Guid.NewGuid().ToString();
            Console.WriteLine($"Creating new TerminalTabPage with Tab ID: {TabId}");
            
            Text = title;
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            // Create terminal panel with double-line border
            terminalPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(4),
                Margin = new Padding(4),
                BackColor = Color.FromArgb(0, 0, 170) // Borland Blue
            };
            

            // Terminal control with tab ID
            Terminal = new SshTerminalControl(TabId)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4) // Add margin inside the double border
            };
            
            // Subscribe to terminal events
            Terminal.ConnectionStateChanged += Terminal_ConnectionStateChanged;
            Terminal.TerminalError += Terminal_TerminalError;
            Terminal.TerminalResized += Terminal_TerminalResized;
            
            terminalPanel.Controls.Add(Terminal);
            this.Controls.Add(terminalPanel);
        }
        
        private void Terminal_ConnectionStateChanged(object sender, SshConnectionEventArgs e)
        {
            Console.WriteLine($"Tab {TabId} connection state changed: {(e.IsConnected ? "Connected" : "Disconnected")}");
            
            // Update tab text based on connection state
            if (e.IsConnected)
            {
                this.Text = $"{e.Username}@{e.Host}:{e.Port}";
            }
            else
            {
                this.Text = ConnectionName ?? "Disconnected";
            }
            
            // Forward the event
            ConnectionStateChanged?.Invoke(this, e);
        }
        
        private void Terminal_TerminalError(object sender, SshTerminalErrorEventArgs e)
        {
            Console.WriteLine($"Tab {TabId} terminal error: {e.Message}");
            
            // Forward the event
            TerminalError?.Invoke(this, e);
        }
        
        private void Terminal_TerminalResized(object sender, TerminalResizeEventArgs e)
        {
            Console.WriteLine($"Tab {TabId} terminal resized to {e.Columns}x{e.Rows}");
            
            // Forward the event
            TerminalResized?.Invoke(this, e);
        }
        
        public async Task ConnectAsync()
        {
            if (string.IsNullOrEmpty(Host) || Port <= 0 || string.IsNullOrEmpty(Username))
                return;
                
            try
            {
                Console.WriteLine($"Tab {TabId} connecting to {Username}@{Host}:{Port}");
                await Terminal.ConnectAsync(Host, Port, Username, Password);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tab {TabId} connection failed: {ex.Message}");
                throw new Exception($"Failed to connect to {Host}:{Port}", ex);
            }
        }
        
        public void Disconnect()
        {
            if (Terminal.IsConnected)
            {
                Console.WriteLine($"Tab {TabId} disconnecting");
                Terminal.Disconnect();
            }
        }
        
        // Set terminal theme colors
        public async Task ApplyThemeAsync(Theme theme)
        {
            if (theme == null) return;
            
            Console.WriteLine($"Tab {TabId} applying theme");
            
            // Set terminal theme (dark/light mode)
            Terminal.SetTheme(theme.IsDarkTheme ? TerminalTheme.Dark : TerminalTheme.Light);
            
            // Apply xterm.js specific colors
            await Terminal.SetTerminalColorsAsync(
                theme.Terminal.Background,
                theme.Terminal.Foreground,
                theme.Terminal.Cursor,
                theme.Terminal.ScrollbarBackground,
                theme.Terminal.ScrollbarThumb
            );
            
            // Update panel colors
            terminalPanel.BackColor = Theme.HexToColor(theme.UI.Background);
            terminalPanel.Invalidate(); // Force redraw of borders
        }
        
        // Play connection sound if enabled
        public async Task PlayConnectionSoundAsync(bool enableSound)
        {
            if (!enableSound) return;
            
            Console.WriteLine($"Tab {TabId} playing connection sound");
            
            string soundFilePath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "modem_short.wav");
                
            if (System.IO.File.Exists(soundFilePath))
            {
                using (System.Media.SoundPlayer player = new System.Media.SoundPlayer(soundFilePath))
                {
                    player.PlaySync(); // Wait until sound finishes
                }
            }
            
            
            await Task.Delay(50); // Small delay to avoid UI lag
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Console.WriteLine($"Tab {TabId} disposing resources");
                
                if (Terminal != null)
                {
                    if (Terminal.IsConnected)
                    {
                        Terminal.Disconnect();
                    }
                    Terminal.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}