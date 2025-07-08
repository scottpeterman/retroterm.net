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
    // UI Components - initialized in InitializeComponent
    private TextBox searchBox = null!;
    private TreeView sessionTree = null!;
    private Panel quickConnectPanel = null!;
    private Label quickConnectHeader = null!;
    private Button newConnectionButton = null!;
    private Button newFolderButton = null!;
    
    // Events
    public event EventHandler<SessionConnectionEventArgs>? ConnectRequested;
    
    // Current theme
    private string currentTheme = "dark";
    private TerminalThemeColors themeColors = null!;
    
    // Sessions file path
    private string sessionsFilePath = string.Empty;
    
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
    /// Loads sessions from YAML file
    /// </summary>
    public void LoadSessions(string? fileContent = null)
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
                TreeNode folderNode = new TreeNode(folder.FolderName ?? "Unnamed Folder");
                folderNode.Tag = new NodeData { Type = "folder" };
                
                // Add sessions in this folder
                if (folder.Sessions != null)
                {
                    foreach (var session in folder.Sessions)
                    {
                        // Use display name if available, otherwise use host
                        string displayText = !string.IsNullOrEmpty(session.DisplayName) 
                            ? session.DisplayName 
                            : session.Host ?? "Unknown Host";
                            
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
    /// Sets the sessions file path
    /// </summary>
    public void SetSessionsFilePath(string path)
    {
        sessionsFilePath = path;
        LoadSessions();
    }
    
    /// <summary>
    /// Update theme colors
    /// </summary>
    public void UpdateTheme(TerminalThemeColors colors)
    {
        themeColors = colors;
        ApplyTheme(colors);
    }

    private void InitializeComponent()
    {
        // Setup the control
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.LightGray;
        
        // Create a simple vertical layout
        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = Color.Red // DEBUG COLOR
        };
        
        // Set row styles - sessions get 70%, buttons get 30%
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F)); // Sessions
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F)); // Connection button
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F)); // Folder button
        
        // =====================================
        // ROW 0 - Sessions Panel
        // =====================================
        Panel sessionsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Blue,
            Padding = new Padding(10)
        };
        
        // Search box
        searchBox = new TextBox
        {
            Dock = DockStyle.Top,
            PlaceholderText = "Search sessions...",
            Height = 30,
            BackColor = Color.White,
            ForeColor = Color.Black
        };
        searchBox.TextChanged += SearchBox_TextChanged;
        
        // Session tree
        sessionTree = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            ForeColor = Color.Black,
            ShowLines = true,
            HideSelection = false,
            FullRowSelect = true,
            ItemHeight = 25,
            ShowPlusMinus = true,
            ShowRootLines = true
        };
        sessionTree.NodeMouseDoubleClick += SessionTree_NodeMouseDoubleClick;
        sessionTree.MouseUp += SessionTree_MouseUp;
        
        sessionsPanel.Controls.Add(sessionTree);
        sessionsPanel.Controls.Add(searchBox);
        
        // =====================================
        // ROW 1 - Connection Button
        // =====================================
        newConnectionButton = new Button
        {
            Text = "NEW CONNECTION",
            Dock = DockStyle.Fill,
            BackColor = Color.Cyan,
            ForeColor = Color.Black,
            Font = new Font("Arial", 10, FontStyle.Bold),
            Margin = new Padding(10)
        };
        newConnectionButton.Click += NewConnectionButton_Click;
        
        // =====================================
        // ROW 2 - Folder Button
        // =====================================
        newFolderButton = new Button
        {
            Text = "NEW FOLDER",
            Dock = DockStyle.Fill,
            BackColor = Color.Magenta,
            ForeColor = Color.Black,
            Font = new Font("Arial", 10, FontStyle.Bold),
            Margin = new Padding(10)
        };
        newFolderButton.Click += NewFolderButton_Click;
        
        // Add everything to the main layout
        mainLayout.Controls.Add(sessionsPanel, 0, 0);
        mainLayout.Controls.Add(newConnectionButton, 0, 1);
        mainLayout.Controls.Add(newFolderButton, 0, 2);
        
        // Add main layout to control
        this.Controls.Add(mainLayout);
        
        // Force the layout to happen immediately
        this.PerformLayout();
        mainLayout.PerformLayout();
        
        // Create the quickConnectPanel and header for compatibility with other code
        quickConnectPanel = new Panel(); // Dummy panel for compatibility
        quickConnectHeader = new Label { Text = "Quick Actions" }; // Dummy label
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

        // Style the new folder button
        newFolderButton.BackColor = ControlPaint.Dark(ColorTranslator.FromHtml(colors.Background), 0.2f);
        newFolderButton.ForeColor = ColorTranslator.FromHtml(colors.Foreground);
        newFolderButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml(colors.Cursor);
        newFolderButton.Font = new Font("Courier New", 9, FontStyle.Bold);
    }

    /// <summary>
    /// Handles click on new folder button
    /// </summary>
    private void NewFolderButton_Click(object? sender, EventArgs e)
    {
        using (var dialog = new TextInputDialog("New Folder", "Enter folder name:"))
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string folderName = dialog.InputText;
                if (!string.IsNullOrWhiteSpace(folderName))
                {
                    CreateNewFolder(folderName);
                }
            }
        }
    }

    /// <summary>
    /// Creates a new folder in the session tree and saves to YAML
    /// </summary>
    private void CreateNewFolder(string folderName)
    {
        try
        {
            // Check if folder already exists
            foreach (TreeNode existingNode in sessionTree.Nodes)
            {
                if (existingNode.Text.Equals(folderName, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"Folder '{folderName}' already exists.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            
            // Create new folder node
            TreeNode folderNode = new TreeNode(folderName);
            folderNode.Tag = new NodeData { Type = "folder" };
            
            // Add to tree
            sessionTree.Nodes.Add(folderNode);
            
            // Save to YAML file
            SaveSessionsToYaml();
            
            // Select the new folder
            sessionTree.SelectedNode = folderNode;
            folderNode.EnsureVisible();
            
            MessageBox.Show($"Folder '{folderName}' created successfully.", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating folder: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Saves the current session tree to YAML file
    /// </summary>
    private void SaveSessionsToYaml()
    {
        try
        {
            var folders = new List<SessionFolder>();
            
            foreach (TreeNode folderNode in sessionTree.Nodes)
            {
                var folder = new SessionFolder
                {
                    FolderName = folderNode.Text,
                    Sessions = new List<SessionData>()
                };
                
                foreach (TreeNode sessionNode in folderNode.Nodes)
                {
                    var nodeData = sessionNode.Tag as NodeData;
                    if (nodeData?.Data is SessionData sessionData)
                    {
                        folder.Sessions.Add(sessionData);
                    }
                }
                
                folders.Add(folder);
            }
            
            var serializer = new SerializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build();
                
            string yamlContent = serializer.Serialize(folders);
            File.WriteAllText(sessionsFilePath, yamlContent);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving sessions: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    /// <summary>
    /// Filters the session tree based on search text
    /// </summary>
    private void SearchBox_TextChanged(object? sender, EventArgs e)
    {
        string searchText = searchBox.Text.ToLowerInvariant();

        // For now, just return - search functionality can be implemented later
        // to avoid complexity with nullable tree node handling
        return;
    }
        
    /// <summary>
    /// Handles double-click on session tree nodes
    /// </summary>
    private void SessionTree_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
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
                    ["host"] = sessionData.Host ?? "",
                    ["port"] = sessionData.Port,
                    ["username"] = sessionData.Username ?? "",
                    ["password"] = sessionData.Password ?? "",
                    ["displayName"] = sessionData.DisplayName ?? sessionData.Host ?? ""
                };
                
                // Raise connect event
                ConnectRequested?.Invoke(this, new SessionConnectionEventArgs(connectionData));
            }
        }
    }
    
    /// <summary>
    /// Handles right-click on session tree nodes
    /// </summary>
    private void SessionTree_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            // Get the node under the mouse cursor
            TreeNode? node = sessionTree.GetNodeAt(e.X, e.Y);
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
    private void NewConnectionButton_Click(object? sender, EventArgs e)
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
    public string? FolderName { get; set; }

    [YamlMember(Alias = "sessions", ApplyNamingConventions = false)]
    public List<SessionData>? Sessions { get; set; }
}

public class SessionData
{
    [YamlMember(Alias = "DeviceType", ApplyNamingConventions = false)]
    public string? DeviceType { get; set; }

    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? SoftwareVersion { get; set; }
    public string? Vendor { get; set; }

    [YamlMember(Alias = "credsid", ApplyNamingConventions = false)]
    public string? CredsId { get; set; }

    [YamlMember(Alias = "display_name", ApplyNamingConventions = false)]
    public string? DisplayName { get; set; }

    [YamlMember(Alias = "host", ApplyNamingConventions = false)]
    public string? Host { get; set; }

    [YamlMember(Alias = "port", ApplyNamingConventions = false)]
    public int Port { get; set; }

    public string? Username { get; set; }
    public string? Password { get; set; }
}

/// <summary>
/// Node data for TreeView
/// </summary>
public class NodeData
{
    public string? Type { get; set; }
    public object? Data { get; set; }
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

/// <summary>
/// Simple text input dialog
/// </summary>
public class TextInputDialog : Form
{
    private TextBox textBox = null!;
    private Button okButton = null!;
    private Button cancelButton = null!;
    
    public string InputText => textBox.Text;
    
    public TextInputDialog(string title, string prompt, string defaultValue = "")
    {
        InitializeDialog(title, prompt, defaultValue);
    }
    
    private void InitializeDialog(string title, string prompt, string defaultValue)
    {
        this.Text = title;
        this.Size = new Size(400, 150);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 2,
            Padding = new Padding(10)
        };
        
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        
        var promptLabel = new Label
        {
            Text = prompt,
            Dock = DockStyle.Fill,
            AutoSize = true
        };
        layout.Controls.Add(promptLabel, 0, 0);
        layout.SetColumnSpan(promptLabel, 2);
        
        textBox = new TextBox
        {
            Text = defaultValue,
            Dock = DockStyle.Fill,
            Font = new Font("Courier New", 9)
        };
        layout.Controls.Add(textBox, 0, 1);
        layout.SetColumnSpan(textBox, 2);
        
        okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Dock = DockStyle.Fill
        };
        
        cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Dock = DockStyle.Fill
        };
        
        layout.Controls.Add(okButton, 0, 2);
        layout.Controls.Add(cancelButton, 1, 2);
        
        this.Controls.Add(layout);
        this.AcceptButton = okButton;
        this.CancelButton = cancelButton;
        
        textBox.SelectAll();
        textBox.Focus();
    }
}