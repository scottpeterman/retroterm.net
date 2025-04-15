using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RetroTerm.NET.Controls;
using System.Threading.Tasks;
using RetroTerm.NET.Models;
using RetroTerm.NET.Services;
using SshTerminalComponent;

namespace RetroTerm.NET.Services
{
    /// <summary>
    /// Service to manage tab creation, deletion, and state
    /// </summary>
    public class TabManager
    {
        private Forms.ThemedTabControl tabControl;
        private ThemeManager themeManager;
        private List<TerminalTabPage> tabs = new List<TerminalTabPage>();
        private bool enableModemSound = false;
        public event EventHandler<SshConnectionEventArgs> ConnectionStateChanged;


        public event EventHandler<TabEventArgs> TabAdded;
        public event EventHandler<TabEventArgs> TabRemoved;
        public event EventHandler<TabEventArgs> TabSelected;
        
        public TerminalTabPage CurrentTab => 
            tabControl.SelectedTab as TerminalTabPage;
            
        public bool EnableModemSound
        {
            get => enableModemSound;
            set => enableModemSound = value;
        }
        // In TabManager.cs - In SetTabFontSize method
public void SetTabFontSize(float size)
{
    if (tabControl != null)
    {
        // Create a new font with the same family and style but with the new size
        Font currentFont = tabControl.Font;
        Font newFont = new Font(currentFont.FontFamily, size, currentFont.Style);
        
        // Set the new font
        tabControl.Font = newFont;
        
        // Force a redraw
        tabControl.Invalidate();
    }
}
        
        public TabManager(TabControl tabControl, ThemeManager themeManager)
        {
            this.tabControl = (Forms.ThemedTabControl?)tabControl;
            this.themeManager = themeManager;
            SetTabFontSize(8.0f);
            // Set up tab control events
            this.tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            this.tabControl.HandleCreated += TabControl_HandleCreated;
            
            // Set up theme manager events
            this.themeManager.ThemeChanged += ThemeManager_ThemeChanged;
        }
        
        private void TabControl_HandleCreated(object sender, EventArgs e)
        {
            // Create "New Tab" button tab after control is created
            CreateNewTabButton();
        }
        
        public void FixTabBackgrounds()
        {
            if (themeManager?.CurrentTheme == null) return;
            
            // Apply to all tabs, including the "+" tab
            foreach (TabPage page in tabControl.TabPages)
            {
                page.BackColor = Theme.HexToColor(themeManager.CurrentTheme.UI.Background);
                
                // For terminal tab pages, also apply terminal theme
                if (page is TerminalTabPage terminalTab)
                {
                    ApplyThemeToTab(terminalTab);
                }
            }
            
            // Force redraw
            tabControl.Invalidate(true);
        }

        // Create the "+" tab for adding new tabs
        private void CreateNewTabButton()
        {
            TabPage addTab = new TabPage("+");
            addTab.ToolTipText = "Create new terminal";
            addTab.Tag = "new-tab-button";
            if (themeManager?.CurrentTheme != null)
            {
                addTab.BackColor = Theme.HexToColor(themeManager.CurrentTheme.UI.Background);
            }
            tabControl.TabPages.Add(addTab);
            
            // Change this to use a MouseDown event instead
            tabControl.MouseDown += (sender, e) => {
                // Get the tab that was clicked
                for (int i = 0; i < tabControl.TabCount; i++)
                {
                    Rectangle rect = tabControl.GetTabRect(i);
                    if (rect.Contains(e.Location) && 
                        tabControl.TabPages[i].Tag != null && 
                        tabControl.TabPages[i].Tag.ToString() == "new-tab-button")
                    {
                        // Schedule the tab creation for after the current event cycle
                        tabControl.Invoke(() => {
                            CreateNewTab();
                            FixTabBackgrounds();
                        });
                        break;
                    }
                }
            };
        }
            
        public TerminalTabPage CreateNewTab(string title = "New Terminal")
        {
            // Generate a unique ID for this tab
            string tabId = Guid.NewGuid().ToString();
            Console.WriteLine($"Creating new tab with ID: {tabId}");
            
            // Create the tab with the ID
            TerminalTabPage newTab = new TerminalTabPage(title, tabId);
            
            // Set up event handlers
            newTab.ConnectionStateChanged += TabPage_ConnectionStateChanged;
            newTab.TerminalError += TabPage_TerminalError;
            newTab.TerminalResized += TabPage_TerminalResized;
            
            // Apply current theme
            ApplyThemeToTab(newTab);
            FixTabBackgrounds();

            // Find the "+" tab
            int newTabButtonIndex = -1;
            for (int i = 0; i < tabControl.TabCount; i++)
            {
                var currentPage = tabControl.TabPages[i];
                if (currentPage.Tag != null && currentPage.Tag.ToString() == "new-tab-button")
                {
                    newTabButtonIndex = i;
                    break;
                }
            }
            
            // Add the new tab after the "+" tab if it exists, otherwise just add it
            if (newTabButtonIndex >= 0)
            {
                // Insert new tab immediately after the "+" tab
                tabControl.TabPages.Insert(newTabButtonIndex + 1, newTab);
            }
            else
            {
                // If "+" tab doesn't exist for some reason, add to the end
                tabControl.TabPages.Add(newTab);
            }
            
            tabs.Add(newTab);
            
            // Select the new tab
            tabControl.SelectedTab = newTab;
            
            // Notify listeners
            TabAdded?.Invoke(this, new TabEventArgs(newTab));
            FixTabBackgrounds();

            return newTab;
        }
        
        public void CloseTab(TerminalTabPage tab = null)
        {
            TerminalTabPage tabToClose = tab ?? CurrentTab;
            
            if (tabToClose != null)
            {
                Console.WriteLine($"Closing tab with ID: {tabToClose.TabId}");
                
                // Confirm close if connected
                if (tabToClose.IsConnected)
                {
                    DialogResult result = MessageBox.Show(
                        "This terminal is connected. Are you sure you want to close it?",
                        "Confirm Close",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2); // Default to "No"
                        
                    if (result != DialogResult.Yes)
                    {
                        return;
                    }
                }
                
                // Disconnect and clean up
                tabToClose.Disconnect();
                
                // Remove from control and list
                tabControl.TabPages.Remove(tabToClose);
                tabs.Remove(tabToClose);
                
                // Notify listeners
                TabRemoved?.Invoke(this, new TabEventArgs(tabToClose));
                
                // Dispose the tab
                tabToClose.Dispose();
                
                // Create new tab if none left (except the "+" tab)
                if (tabs.Count == 0)
                {
                    CreateNewTab();
                }
            }
        }
        public async Task ConnectTabAsync(TerminalTabPage tab = null)
{
    TerminalTabPage tabToConnect = tab ?? CurrentTab;
    
    if (tabToConnect != null)
    {
        Console.WriteLine($"TabManager: Attempting to connect tab with ID {tabToConnect.TabId}");
        try
        {
            // Play connection sound if enabled
            await tabToConnect.PlayConnectionSoundAsync(enableModemSound);
            
            // Connect to SSH
            Console.WriteLine($"TabManager: Calling ConnectAsync on tab {tabToConnect.TabId}");
            await tabToConnect.ConnectAsync();
            Console.WriteLine($"TabManager: ConnectAsync completed for tab {tabToConnect.TabId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TabManager: Error connecting tab {tabToConnect.TabId}: {ex}");
            // Show error to user
            MessageBox.Show($"Connection failed: {ex.Message}", 
                "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    else
    {
        Console.WriteLine("TabManager: No tab to connect");
    }
}
        public void DisconnectTab(TerminalTabPage tab = null)
        {
            TerminalTabPage tabToDisconnect = tab ?? CurrentTab;
            
            if (tabToDisconnect != null && tabToDisconnect.IsConnected)
            {
                Console.WriteLine($"Disconnecting tab with ID: {tabToDisconnect.TabId}");
                tabToDisconnect.Disconnect();
            }
        }
        
        public void ApplyThemeToTab(TerminalTabPage tab)
        {
            var theme = themeManager.CurrentTheme;
            if (theme == null) return;
            
            try
            {
                Console.WriteLine($"Applying theme to tab with ID: {tab.TabId}");
                tab.ApplyThemeAsync(theme).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying theme to tab {tab.TabId}: {ex.Message}");
            }
        }
        
        public void ApplyThemeToAllTabs()
        {
            Console.WriteLine($"Applying theme to all tabs ({tabs.Count} tabs)");
            foreach (TerminalTabPage tab in tabs)
            {
                ApplyThemeToTab(tab);
            }
        }
        
        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CurrentTab != null)
            {
                Console.WriteLine($"Selected tab changed to: {CurrentTab.TabId}");
                TabSelected?.Invoke(this, new TabEventArgs(CurrentTab));
            }
        }
        
        private void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            Console.WriteLine("Theme changed, updating all tabs");
            ApplyThemeToAllTabs();
            FixTabBackgrounds(); 
        }
        
        // In TabManager.cs - add a new event

// Then in your TabPage_ConnectionStateChanged method, forward this event
private void TabPage_ConnectionStateChanged(object sender, SshConnectionEventArgs e)
{
    // Handle terminal connection state changes
    if (sender is TerminalTabPage tab)
    {
        Console.WriteLine($"Tab {tab.TabId} connection state changed: {(e.IsConnected ? "Connected" : "Disconnected")}");
        
        // Update tab appearance based on connection state
        if (e.IsConnected)
        {
            // Connected state
            // (Tab text is already updated in TerminalTabPage)
        }
        else
        {
            // Disconnected state
        }
        
        // Forward the event to listeners (add this line)
        ConnectionStateChanged?.Invoke(tab, e);
    }
}
        private void TabPage_TerminalError(object sender, SshTerminalErrorEventArgs e)
        {
            // Handle terminal errors - just forward to subscribers
            if (sender is TerminalTabPage tab)
            {
                Console.WriteLine($"Tab {tab.TabId} terminal error: {e.Message}");
            }
        }
        
        private void TabPage_TerminalResized(object sender, TerminalResizeEventArgs e)
        {
            // Handle terminal resize - just forward to subscribers
            if (sender is TerminalTabPage tab)
            {
                Console.WriteLine($"Tab {tab.TabId} terminal resized: {e.Columns}x{e.Rows}");
            }
        }
    }
    
    public class TabEventArgs : EventArgs
    {
        public TerminalTabPage Tab { get; }
        
        public TabEventArgs(TerminalTabPage tab)
        {
            Tab = tab;
        }
    }
}