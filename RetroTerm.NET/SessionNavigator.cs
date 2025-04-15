using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Runtime.InteropServices;
using Aga.Controls.Tree;
using System.Text;
using System.Linq;

namespace RetroTerm.NET
{
    /// <summary>
    /// Session navigator control for SSH terminal sessions
    /// </summary>
    public class SessionNavigator : UserControl
    {
        // UI Components
        private TextBox searchBox;
        private ContainedTreeView sessionTree;
        
        public Form parentForm;

        private Panel quickConnectPanel;
        private Button newConnectionButton;
        
        // Events
        public event EventHandler<SessionConnectionEventArgs> ConnectRequested;
        
        // Theme management
        private Services.ThemeManager themeManager;
        
        // Sessions file path
        private string sessionsFilePath;
        
        // Store original data to preserve tree structure
        private List<SessionFolder> originalFolders = new List<SessionFolder>();
        
        public SessionNavigator()
        {
            // Initialize the control
            InitializeComponent();
            
            // Default to current directory for sessions file
            sessionsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessions.yaml");
        }
        
        public void SetParentForm(Form m_parentForm)
            {
    parentForm = m_parentForm;
    Console.WriteLine($"SetParentForm called with form: {parentForm != null}");
}
        public void RefreshSessions()
        {
            // Simply call LoadSessions to refresh
            LoadSessions();
        }

        /// <summary>
        /// Constructor with theme manager
        /// </summary>
        public SessionNavigator(Services.ThemeManager themeManager) : this()
        {
            SetThemeManager(themeManager);
        }
        
        /// <summary>
        /// Sets the sessions file path
        /// </summary>
        public void SetSessionsFilePath(string path)
        {
            sessionsFilePath = path;
            LoadSessions();
        }
        
        /// <summary>
        /// Sets the theme manager and subscribes to theme changes
        /// </summary>
        public void SetThemeManager(Services.ThemeManager themeManager)
        {
            this.themeManager = themeManager;
            
            // Subscribe to theme changes
            if (themeManager != null)
            {
                themeManager.ThemeChanged += ThemeManager_ThemeChanged;
                
                // Apply current theme
                ApplyTheme(themeManager.CurrentTheme);
            }
        }
        
        private void ThemeManager_ThemeChanged(object sender, Services.ThemeChangedEventArgs e)
        {
            ApplyTheme(e.Theme);
        }
        
        private void InitializeComponent()
        {
            // Setup the control
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            this.Margin = new Padding(0);

            // Create layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
            
            // Create main panel for sessions area
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            // Create search box
            searchBox = new TextBox
            {
                Dock = DockStyle.Top,
                PlaceholderText = "Search sessions... (press Enter to search)",
                Height = 30
            };
            // Use KeyDown event
            searchBox.KeyDown += SearchBox_KeyDown;
            
            // Create tree panel to hold tree
            Panel treePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0)
            };
            
            // Create session tree using our ContainedTreeView with TreeViewAdv
            sessionTree = new ContainedTreeView
            {
                Dock = DockStyle.Fill
            };
            
            // Set up event handlers
            sessionTree.NodeMouseDoubleClick += SessionTree_NodeMouseDoubleClick;
            sessionTree.TreeMouseUp += SessionTree_MouseUp;
            
            // Add controls to tree panel
            treePanel.Controls.Add(sessionTree);
            
            // Add controls to main panel
            mainPanel.Controls.Add(treePanel);
            mainPanel.Controls.Add(searchBox);
            
            // Create quick connect panel
            quickConnectPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            // Create new connection button
            newConnectionButton = new Button
            {
                Text = "NEW CONNECTION",
                Dock = DockStyle.Top,
                Height = 35,
                FlatStyle = FlatStyle.Flat
            };
            newConnectionButton.Click += NewConnectionButton_Click;
            
            // Add controls to quick connect panel
            quickConnectPanel.Controls.Add(newConnectionButton);
            
            // Add panels to main layout
            mainLayout.Controls.Add(mainPanel, 0, 0);
            mainLayout.Controls.Add(quickConnectPanel, 0, 1);
            
            // Add layout to control
            this.Controls.Add(mainLayout);
        }
        
        /// <summary>
        /// Handler for Enter key press in search box
        /// </summary>
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                
                // Perform the search
                PerformSearch();
            }
        }
        
        /// <summary>
        /// Performs search using current search text
        /// </summary>
        private void PerformSearch()
        {
            string searchText = searchBox.Text.ToLowerInvariant();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // If search is empty, restore all original folders
                LoadFolders(originalFolders);
            }
            else
            {
                // Filter folders and sessions based on search text
                var filteredFolders = new List<SessionFolder>();
                
                foreach (var originalFolder in originalFolders)
                {
                    var filteredFolder = new SessionFolder
                    {
                        FolderName = originalFolder.FolderName,
                        Sessions = new List<SessionData>()
                    };
                    
                    // Filter sessions in this folder
                    if (originalFolder.Sessions != null)
                    {
                        foreach (var session in originalFolder.Sessions)
                        {
                            string displayText = !string.IsNullOrEmpty(session.DisplayName) 
                                ? session.DisplayName 
                                : session.Host;
                                
                            if (displayText.ToLowerInvariant().Contains(searchText))
                            {
                                filteredFolder.Sessions.Add(session);
                            }
                        }
                    }
                    
                    // Only add folder if it has matching sessions
                    if (filteredFolder.Sessions.Count > 0)
                    {
                        filteredFolders.Add(filteredFolder);
                    }
                }
                
                // Load filtered data
                LoadFolders(filteredFolders);
            }
        }
        
        /// <summary>
        /// Helper method to load folders into the tree
        /// </summary>
        private void LoadFolders(List<SessionFolder> folders)
        {
            // Use the ContainedTreeView's Load method
            sessionTree.Load(folders,this.parentForm);
        }
        
        /// <summary>
        /// Loads sessions from YAML file
        /// </summary>
        public void LoadSessions(string fileContent = null)
        {
            try
            {
                string yamlContent;
                
                if (fileContent != null)
                {
                    yamlContent = fileContent;
                }
                else
                {
                    if (!File.Exists(sessionsFilePath))
                    {
                        MessageBox.Show($"Sessions file not found: {sessionsFilePath}", 
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    // Read YAML file
                    yamlContent = File.ReadAllText(sessionsFilePath);
                }
                
                // Setup YAML deserializer
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(NullNamingConvention.Instance)
                    .Build();
                
                // Deserialize sessions
                var folders = deserializer.Deserialize<List<SessionFolder>>(yamlContent);
                
                // Apply decryption to all passwords in the loaded sessions
                DecryptSessionPasswords(folders);
                
                // Store original data
                originalFolders = folders.Select(f => CloneSessionFolder(f)).ToList();
                
                // Load into tree
                LoadFolders(folders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sessions: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // Helper method to decrypt passwords in loaded sessions
        private void DecryptSessionPasswords(List<SessionFolder> folders)
{
    foreach (var folder in folders)
    {
        if (folder.Sessions != null)
        {
            foreach (var session in folder.Sessions)
            {
                // Use PasswordManager's built-in check and decryption
                if (!string.IsNullOrEmpty(session.Password))
                {
                    // PasswordManager.DecryptPassword already checks if it's encrypted
                    session.Password = PasswordManager.DecryptPassword(session.Password);
                }
            }
        }
    }
}
        
        /// <summary>
        /// Helper method to clone a SessionFolder
        /// </summary>
        private SessionFolder CloneSessionFolder(SessionFolder original)
        {
            var clone = new SessionFolder
            {
                FolderName = original.FolderName,
                Sessions = new List<SessionData>()
            };
            
            if (original.Sessions != null)
            {
                foreach (var session in original.Sessions)
                {
                    // Clone session data
                    var sessionClone = new SessionData
                    {
                        Host = session.Host,
                        Port = session.Port,
                        Username = session.Username,
                        Password = session.Password,
                        DisplayName = session.DisplayName
                        // Add any other properties as needed
                    };
                    
                    clone.Sessions.Add(sessionClone);
                }
            }
            
            return clone;
        }
        
        /// <summary>
        /// Applies theme to the control
        /// </summary>
        private void ApplyTheme(Models.Theme theme)
        {
            if (theme == null) return;
            
            // Apply colors to the control
            this.BackColor = Models.Theme.HexToColor(theme.UI.Background);
            
            // Style the search box
            searchBox.BackColor = ControlPaint.Dark(Models.Theme.HexToColor(theme.UI.Background), 0.2f);
            searchBox.ForeColor = Models.Theme.HexToColor(theme.UI.Text);
            searchBox.BorderStyle = BorderStyle.FixedSingle;
            searchBox.Font = new Font("Courier New", 9);
            
            // Style the session tree
            sessionTree.ApplyTheme(theme);
            
            // Style the quick connect panel
            quickConnectPanel.BackColor = Models.Theme.HexToColor(theme.UI.Background);
            quickConnectPanel.ForeColor = Models.Theme.HexToColor(theme.UI.Text);
            
            // Style the new connection button
            newConnectionButton.BackColor = Models.Theme.HexToColor(theme.UI.ButtonBackground);
            newConnectionButton.ForeColor = Models.Theme.HexToColor(theme.UI.ButtonText);
            newConnectionButton.FlatAppearance.BorderColor = Models.Theme.HexToColor(theme.UI.Border);
            newConnectionButton.Font = new Font("Courier New", 9, FontStyle.Bold);
            
            // Button hover styles are set via event handlers
            newConnectionButton.MouseEnter += (s, e) => {
                newConnectionButton.BackColor = ControlPaint.Light(Models.Theme.HexToColor(theme.UI.ButtonBackground), 0.1f);
                newConnectionButton.FlatAppearance.BorderColor = Models.Theme.HexToColor(theme.UI.Text);
            };
            
            newConnectionButton.MouseLeave += (s, e) => {
                newConnectionButton.BackColor = Models.Theme.HexToColor(theme.UI.ButtonBackground);
                newConnectionButton.FlatAppearance.BorderColor = Models.Theme.HexToColor(theme.UI.Border);
            };
        }
        
        /// <summary>
        /// Handles double-click on session tree nodes
        /// </summary>
        private void SessionTree_NodeMouseDoubleClick(object sender, TreeNodeAdvMouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
    {
        // Debug mode - show node properties
        DumpNodeProperties(e.Node);
        return;
    }
            // Check if the clicked node represents a session
            if (e.Node.Tag is SessionNode sessionNode)
            {
                var sessionData = sessionNode.Session;
                // Get password and check if it needs decryption
                string password = sessionData.Password ?? "";
                if (PasswordManager.IsEncrypted(password))
                {
                    password = PasswordManager.DecryptPassword(password);
                }
                // Create connection data dictionary
                var connectionData = new Dictionary<string, object>
                {
                    ["host"] = sessionData.Host,
                    ["port"] = sessionData.Port,
                    ["username"] = sessionData.Username ?? "",
                    ["password"] = sessionData.Password ?? "",
                    ["displayName"] = sessionData.DisplayName ?? sessionData.Host
                };
                
                // Raise connect event
                ConnectRequested?.Invoke(this, new SessionConnectionEventArgs(connectionData));
            }
        }
        
        
        private void DumpNodeProperties(TreeNodeAdv node)
{
    if (node == null || node.Tag == null) return;
    
    try
    {
        // Create a formatted string with all node properties
        StringBuilder sb = new StringBuilder();
        
        // Check based on tag type rather than class name
        if (node.Tag is SessionNode sessionNode)
        {
            var session = sessionNode.Session;
            
            // Add basic info
            sb.AppendLine($"Node Type: Session Node");
            sb.AppendLine($"Display Name: {sessionNode.DisplayName}");
            sb.AppendLine();
            
            // Add session properties
            sb.AppendLine("SESSION PROPERTIES:");
            sb.AppendLine($"DisplayName: {session.DisplayName}");
            sb.AppendLine($"Host: {session.Host}");
            sb.AppendLine($"Port: {session.Port}");
            sb.AppendLine($"Username: {session.Username}");
            
            // Password analysis
            string passwordStatus = "NULL";
            if (session.Password != null)
            {
                passwordStatus = $"Length: {session.Password.Length}, " +
                                $"IsEncrypted: {PasswordManager.IsEncrypted(session.Password)}, " +
                                $"First 3 chars: {(session.Password.Length > 3 ? session.Password.Substring(0, 3) : session.Password)}";
            }
            sb.AppendLine($"Password: {passwordStatus}");
            
            // Try to decrypt and show result
            if (session.Password != null)
            {
                string decryptResult = "N/A";
                try
                {
                    decryptResult = PasswordManager.DecryptPassword(session.Password);
                    if (decryptResult != null && decryptResult.Length > 3)
                    {
                        decryptResult = $"Length: {decryptResult.Length}, First 3 chars: {decryptResult.Substring(0, 3)}";
                    }
                }
                catch (Exception ex)
                {
                    decryptResult = $"Error: {ex.Message}";
                }
                sb.AppendLine($"Decrypted Password: {decryptResult}");
            }
        }
        else
        {
            // Just print what type of object we received
            sb.AppendLine($"Node Tag Type: {node.Tag.GetType().FullName}");
            
            // Try to extract common properties using reflection
            foreach (var prop in node.Tag.GetType().GetProperties())
            {
                try
                {
                    var value = prop.GetValue(node.Tag);
                    sb.AppendLine($"{prop.Name}: {value}");
                }
                catch
                {
                    // Skip properties that throw exceptions
                }
            }
        }
        
        // Show the properties in a dialog
        using (Form debugForm = new Form())
        {
            debugForm.Text = "Node Properties Debug";
            debugForm.Size = new Size(500, 400);
            debugForm.StartPosition = FormStartPosition.CenterScreen;
            
            TextBox textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                Text = sb.ToString(),
                ScrollBars = ScrollBars.Both
            };
            
            debugForm.Controls.Add(textBox);
            debugForm.ShowDialog();
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error dumping node properties: {ex.Message}", 
            "Debug Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
        
        
        /// <summary>
        /// Handles right-click on session tree nodes
        /// </summary>
        private void SessionTree_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Get the node under the mouse cursor
                TreeNodeAdv node = sessionTree.GetNodeAtPoint(e.Location);
                if (node != null && node.Tag is SessionNode sessionNode)
                {
                    // Create context menu
                    ContextMenuStrip contextMenu = new ContextMenuStrip();
                    
                    var connectItem = new ToolStripMenuItem("Connect");
                    connectItem.Click += (s, args) => {
                        // Create connection data and raise event
                        var sessionData = sessionNode.Session;
                        
                        string password = sessionData.Password ?? "";
                        if (PasswordManager.IsEncrypted(password))
                        {
                            password = PasswordManager.DecryptPassword(password);
                        }

                        var connectionData = new Dictionary<string, object>
                        {
                            ["host"] = sessionData.Host,
                            ["port"] = sessionData.Port,
                            ["username"] = sessionData.Username ?? "",
                            ["password"] = password,
                            ["displayName"] = sessionData.DisplayName ?? sessionData.Host
                        };
                        
                        // Raise connect event
                        ConnectRequested?.Invoke(this, new SessionConnectionEventArgs(connectionData));
                    };
                    
                    contextMenu.Items.Add(connectItem);
                    
                    // Show context menu
                    contextMenu.Show(sessionTree, e.Location);
                }
            }
        }
        
        /// <summary>
        /// Handles click on new connection button
        /// </summary>
        private void NewConnectionButton_Click(object sender, EventArgs e)
        {
            // Create a simple connection dictionary with defaults
            var connectionData = new Dictionary<string, object>
            {
                ["host"] = "",
                ["port"] = 22,
                ["username"] = "",
                ["password"] = "",
                ["displayName"] = ""
            };
            
            // Raise connect event to let the container handle showing a dialog
            ConnectRequested?.Invoke(this, new SessionConnectionEventArgs(connectionData, true));
        }
    }
    
    /// <summary>
    /// Event args for session connection requests
    /// </summary>
    public class SessionConnectionEventArgs : EventArgs
    {
        public Dictionary<string, object> ConnectionData { get; private set; }
        public bool IsNewConnection { get; private set; }
        
        public SessionConnectionEventArgs(Dictionary<string, object> connectionData, bool isNewConnection = false)
        {
            ConnectionData = connectionData;
            IsNewConnection = isNewConnection;
        }
    }
}