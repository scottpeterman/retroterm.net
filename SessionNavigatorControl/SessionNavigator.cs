using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SessionNavigatorControl;

    /// <summary>
    /// Session navigator control for SSH terminal sessions
    /// </summary>
    public class SessionNavigator : UserControl
    {
        // UI Components
        private TextBox searchBox;
        private TreeView sessionTree;
        private Panel quickConnectPanel;
        private Label quickConnectHeader;
        private Button newConnectionButton;
        
        // Events
        public event EventHandler<SessionConnectionEventArgs> ConnectRequested;
        
        // Current theme
        private string currentTheme = "dark";
        private TerminalThemeColors themeColors;
        
        // Sessions file path
        private string sessionsFilePath;
        
        public SessionNavigator()
        {
            // Initialize the control
            InitializeComponent();
            
            // Default to current directory for sessions file
            sessionsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessions.yaml");
            
            // Create default theme colors
            themeColors = new TerminalThemeColors();
            
            // Apply initial theme
            ApplyTheme(themeColors);
        }
        
        /// <summary>
        /// Constructor with theme colors
        /// </summary>
        public SessionNavigator(TerminalThemeColors colors) : this()
        {
            themeColors = colors;
            ApplyTheme(colors);
        }
        
        /// <summary>
        /// Sets the sessions file path
        /// </summary>
        public void SetSessionsFilePath(string path)
        {
            sessionsFilePath = path;
            LoadSessions();
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
                PlaceholderText = "Search sessions...",
                Height = 30
            };
            searchBox.TextChanged += SearchBox_TextChanged;
            
            // Create session tree
            sessionTree = new TreeView
            {
                Dock = DockStyle.Fill,
                ShowLines = true,
                HideSelection = false,
                FullRowSelect = true,
                ItemHeight = 25,
                ShowPlusMinus = true,
                ShowRootLines = true
            };
            sessionTree.NodeMouseDoubleClick += SessionTree_NodeMouseDoubleClick;
            sessionTree.MouseUp += SessionTree_MouseUp;
            
            // Add controls to main panel
            mainPanel.Controls.Add(sessionTree);
            mainPanel.Controls.Add(searchBox);
            
            // Create quick connect panel
            quickConnectPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            // Create quick connect header
            quickConnectHeader = new Label
            {
                Text = "Quick Connect",
                Dock = DockStyle.Top,
                Font = new Font("Courier New", 10, FontStyle.Bold),
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft
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
            quickConnectPanel.Controls.Add(quickConnectHeader);
            
            // Add panels to main layout
            mainLayout.Controls.Add(mainPanel, 0, 0);
            mainLayout.Controls.Add(quickConnectPanel, 0, 1);
            
            // Add layout to control
            this.Controls.Add(mainLayout);
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
                        MessageBox.Show($"Sessions file not found: {sessionsFilePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    // Read YAML file
                    yamlContent = File.ReadAllText(sessionsFilePath);
                }
                
                // Setup YAML deserializer
                var deserializer = new DeserializerBuilder()
    .WithNamingConvention(NullNamingConvention.Instance) // Correct for PascalCase keys
    .Build();

                
                // Deserialize sessions
                var folders = deserializer.Deserialize<List<SessionFolder>>(yamlContent);
                
                // Clear current tree
                sessionTree.Nodes.Clear();
                
                // Add folders and sessions to tree
                foreach (var folder in folders)
                {
                    TreeNode folderNode = new TreeNode(folder.FolderName);
                    folderNode.Tag = new NodeData { Type = "folder" };
                    
                    // Add sessions in this folder
                    if (folder.Sessions != null)
                    {
                        foreach (var session in folder.Sessions)
                        {
                            // Use display name if available, otherwise use host
                            string displayText = !string.IsNullOrEmpty(session.DisplayName) 
                                ? session.DisplayName 
                                : session.Host;
                                
                            TreeNode sessionNode = new TreeNode(displayText);
                            sessionNode.Tag = new NodeData { Type = "session", Data = session };
                            
                            folderNode.Nodes.Add(sessionNode);
                        }
                    }
                    
                    sessionTree.Nodes.Add(folderNode);
                }
                
                // Expand all nodes
                sessionTree.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sessions: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Update theme colors
        /// </summary>
        public void UpdateTheme(TerminalThemeColors colors)
        {
            themeColors = colors;
            ApplyTheme(colors);
        }
        
        /// <summary>
        /// Applies theme to the control
        /// </summary>
        private void ApplyTheme(TerminalThemeColors colors)
        {
            // Apply colors to the control
            this.BackColor = ColorTranslator.FromHtml(colors.Background);
            
            // Style the search box
            searchBox.BackColor = ControlPaint.Dark(ColorTranslator.FromHtml(colors.Background), 0.2f);
            searchBox.ForeColor = ColorTranslator.FromHtml(colors.Foreground);
            searchBox.BorderStyle = BorderStyle.FixedSingle;
            searchBox.Font = new Font("Courier New", 9);
            
            // Style the session tree
            sessionTree.BackColor = ControlPaint.Dark(ColorTranslator.FromHtml(colors.Background), 0.2f);
            sessionTree.ForeColor = ColorTranslator.FromHtml(colors.Foreground);
            sessionTree.BorderStyle = BorderStyle.FixedSingle;
            sessionTree.Font = new Font("Courier New", 9);
            
            // Style the quick connect panel
            quickConnectPanel.BackColor = ColorTranslator.FromHtml(colors.Background);
            quickConnectPanel.ForeColor = ColorTranslator.FromHtml(colors.Foreground);
            
            // Style the quick connect header
            quickConnectHeader.ForeColor = ColorTranslator.FromHtml(colors.Foreground);
            
            // Style the new connection button
            newConnectionButton.BackColor = ControlPaint.Dark(ColorTranslator.FromHtml(colors.Background), 0.2f);
            newConnectionButton.ForeColor = ColorTranslator.FromHtml(colors.Foreground);
            newConnectionButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml(colors.Cursor);
            newConnectionButton.Font = new Font("Courier New", 9, FontStyle.Bold);
            
            // Button hover styles are set via event handlers since WinForms doesn't support CSS-like :hover
            newConnectionButton.MouseEnter += (s, e) => {
                newConnectionButton.BackColor = ControlPaint.Light(ColorTranslator.FromHtml(colors.Background), 0.1f);
                newConnectionButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml(colors.Foreground);
            };
            
            newConnectionButton.MouseLeave += (s, e) => {
                newConnectionButton.BackColor = ControlPaint.Dark(ColorTranslator.FromHtml(colors.Background), 0.2f);
                newConnectionButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml(colors.Cursor);
            };
        }
        
        /// <summary>
/// Filters the session tree based on search text
/// </summary>
private void SearchBox_TextChanged(object sender, EventArgs e)
{
    string searchText = searchBox.Text.ToLowerInvariant();

    // Backup original nodes
    List<TreeNode> allNodes = new List<TreeNode>();

    foreach (TreeNode folderNode in sessionTree.Nodes)
    {
        allNodes.Add(folderNode);

        foreach (TreeNode sessionNode in folderNode.Nodes)
        {
            allNodes.Add(sessionNode);
        }
    }

    // Clear the tree and re-add matching nodes
    sessionTree.Nodes.Clear();

    foreach (var folderNode in allNodes)
    {
        TreeNode newFolderNode = new TreeNode(folderNode.Text);

        foreach (TreeNode sessionNode in folderNode.Nodes)
        {
            bool matches = string.IsNullOrWhiteSpace(searchText) ||
                           sessionNode.Text.ToLowerInvariant().Contains(searchText);

            if (matches)
            {
                TreeNode newSessionNode = new TreeNode(sessionNode.Text)
                {
                    Tag = sessionNode.Tag
                };
                newFolderNode.Nodes.Add(newSessionNode);
            }
        }

        if (newFolderNode.Nodes.Count > 0)
        {
            sessionTree.Nodes.Add(newFolderNode);
        }
    }

    sessionTree.ExpandAll();
}

        
        /// <summary>
        /// Handles double-click on session tree nodes
        /// </summary>
        private void SessionTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var nodeData = e.Node.Tag as NodeData;
            if (nodeData?.Type == "session" && nodeData.Data != null)
            {
                var sessionData = nodeData.Data as SessionData;
                if (sessionData != null)
                {
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
        }
        
        /// <summary>
        /// Handles right-click on session tree nodes
        /// </summary>
        private void SessionTree_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Get the node under the mouse cursor
                TreeNode node = sessionTree.GetNodeAt(e.X, e.Y);
                if (node != null)
                {
                    sessionTree.SelectedNode = node;
                    
                    var nodeData = node.Tag as NodeData;
                    if (nodeData?.Type == "session")
                    {
                        // Create context menu
                        ContextMenuStrip contextMenu = new ContextMenuStrip();
                        
                        var connectItem = new ToolStripMenuItem("Connect");
                        connectItem.Click += (s, args) => {
                            // Simulate double-click on the node
                            SessionTree_NodeMouseDoubleClick(
                                sender, 
                                new TreeNodeMouseClickEventArgs(node, MouseButtons.Left, 1, e.X, e.Y)
                            );
                        };
                        
                        contextMenu.Items.Add(connectItem);
                        
                        // Show context menu
                        contextMenu.Show(sessionTree, e.Location);
                    }
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
    /// Data classes for session folders and items
    /// </summary>

public class SessionFolder
{
    [YamlMember(Alias = "folder_name", ApplyNamingConventions = false)]
    public string FolderName { get; set; }

    [YamlMember(Alias = "sessions", ApplyNamingConventions = false)]
    public List<SessionData> Sessions { get; set; }
}


public class SessionData
{
    [YamlMember(Alias = "DeviceType", ApplyNamingConventions = false)]
    public string DeviceType { get; set; }

    public string Model { get; set; }
    public string SerialNumber { get; set; }
    public string SoftwareVersion { get; set; }
    public string Vendor { get; set; }

    [YamlMember(Alias = "credsid", ApplyNamingConventions = false)]
    public string CredsId { get; set; }

    [YamlMember(Alias = "display_name", ApplyNamingConventions = false)]
    public string DisplayName { get; set; }

    [YamlMember(Alias = "host", ApplyNamingConventions = false)]
    public string Host { get; set; }

    [YamlMember(Alias = "port", ApplyNamingConventions = false)]
    public int Port { get; set; }

    public string Username { get; set; }
    public string Password { get; set; }
}

     /// <summary>
    /// Node data for TreeView
    /// </summary>
    public class NodeData
    {
        public string Type { get; set; }
        public object Data { get; set; }
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
