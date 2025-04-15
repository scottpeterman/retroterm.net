using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using RetroTerm.NET.Services;  // For ThemeManager
using RetroTerm.NET.Models;
using RetroTerm.NET;

namespace RetroTerm.NET
{
    
// This class maps to what the application expects
public class FolderData
{
    [YamlMember(Alias = "FolderName", ApplyNamingConventions = false)]
    public string FolderName { get; set; } = "";
    
    [YamlMember(Alias = "sessions", ApplyNamingConventions = false)]
    public List<SessionData> Sessions { get; set; } = new List<SessionData>();
}

// public class SessionData
// {
//     [YamlMember(Alias = "DeviceType", ApplyNamingConventions = false)]
//     public string DeviceType { get; set; } = "Linux";
    
//     // Regular properties without aliases
//     public string Model { get; set; } = "";
//     public string SerialNumber { get; set; } = "";
//     public string SoftwareVersion { get; set; } = "";
//     public string Vendor { get; set; } = "";
    
//     [YamlMember(Alias = "credsid", ApplyNamingConventions = false)]
//     public string CredsId { get; set; } = "";
    
//     [YamlMember(Alias = "DisplayName", ApplyNamingConventions = false)]
//     public string DisplayName { get; set; } = "New Session";
    
//     [YamlMember(Alias = "Host", ApplyNamingConventions = false)]
//     public string Host { get; set; } = "";
    
//     [YamlMember(Alias = "Port", ApplyNamingConventions = false)]
//     public int Port { get; set; } = 22;
    
//     // Additional properties
//     public string Username { get; set; } = "";
//     public string Password { get; set; } = "";
// }

// This class maps to what the application expects
    public class SessionEditorDialog : Form
    {
        private readonly string _sessionFilePath;
        private List<FolderData> _sessionsData = new List<FolderData>();
        private TreeNode _dragNode;
        private TreeView treeViewSessions;
        private ThemeManager _themeManager;
        private Font _dosFont;

        // Session data classes to match YAML structure
        public SessionEditorDialog(string sessionFilePath, ThemeManager themeManager)
        {
            _sessionFilePath = sessionFilePath;
            _themeManager = themeManager;
            
            // Initialize DOS font
            InitializeDOSFont();
            
            InitializeComponent();
            LoadSessions();
            
            // Apply theme
            ApplyTheme();
        }
        
        private void InitializeDOSFont()
        {
            // Try to use Perfect DOS VGA 437 if available, otherwise fall back to Consolas
            try
            {
                _dosFont = new Font("Perfect DOS VGA 437", 12, FontStyle.Regular);
            }
            catch
            {
                try 
                {
                    _dosFont = new Font("Consolas", 14, FontStyle.Regular);
                }
                catch
                {
                    // Last resort fallback
                    _dosFont = new Font(FontFamily.GenericMonospace, 14, FontStyle.Regular);
                }
            }
        }
        
        private void ApplyTheme()
        {
            if (_themeManager?.CurrentTheme == null) return;
            
            var theme = _themeManager.CurrentTheme;
            
            // Apply theme to form
            this.BackColor = Theme.HexToColor(theme.UI.Background);
            this.ForeColor = Theme.HexToColor(theme.UI.Text);
            
            // Apply theme to tree view
            treeViewSessions.BackColor = Theme.HexToColor(theme.UI.Background);
            treeViewSessions.ForeColor = Theme.HexToColor(theme.UI.Text);
            
            // Apply theme to all controls
            ApplyThemeToContainer(this, theme);
        }
        
        private void ApplyThemeToContainer(Control container, Models.Theme theme)
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
                else if (control is TreeView tree)
                {
                    tree.BackColor = Theme.HexToColor(theme.UI.Background);
                    tree.ForeColor = Theme.HexToColor(theme.UI.Text);
                }
                else if (control is Label label)
                {
                    if (label.Parent is Panel panel2 && panel2.Name == "titleBar")
                    {
                        // Title bar label
                        label.ForeColor = Theme.HexToColor(theme.UI.MenuText);
                    }
                    else
                    {
                        label.ForeColor = Theme.HexToColor(theme.UI.Text);
                    }
                }
                
                // Recursively apply to child controls
                if (control.Controls.Count > 0)
                    ApplyThemeToContainer(control, theme);
            }
        }
        
private void InitializeComponent()
{
    // Form setup with proper size and border style
    this.Text = "Edit Sessions";
    this.Size = new System.Drawing.Size(800, 500); // Increased height
    this.FormBorderStyle = FormBorderStyle.Sizable; // Changed to Sizable
    this.StartPosition = FormStartPosition.CenterParent;
    this.Font = _dosFont;
    this.MinimumSize = new Size(600, 600); // Set minimum size
    
    // Create title bar similar to main form
    Panel titleBar = new Panel
    {
        Name = "titleBar",
        Dock = DockStyle.Top,
        Height = 30,
        BackColor = Color.Silver
    };
    
    Label titleLabel = new Label
    {
        Text = "Edit Sessions",
        TextAlign = ContentAlignment.MiddleLeft,
        Dock = DockStyle.Fill,
        Padding = new Padding(10, 0, 0, 0),
        Font = _dosFont
    };
    
    Button closeButton = new Button
    {
        Text = "[X]",
        FlatStyle = FlatStyle.Flat,
        Size = new Size(40, 28),
        Location = new Point(this.Width - 42, 1),
        Anchor = AnchorStyles.Top | AnchorStyles.Right,
        TabStop = false,
        Font = new Font(_dosFont.FontFamily, 8, FontStyle.Bold)
    };
    
    closeButton.FlatAppearance.BorderSize = 0;
    closeButton.Click += (s, e) => this.Close();
    
    titleBar.Controls.Add(titleLabel);
    titleBar.Controls.Add(closeButton);
    
    // Make title bar draggable
    titleBar.MouseDown += TitleBar_MouseDown;
    
    // Create a main container with padding
    Panel mainContainer = new Panel
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(10)
    };
    
    // Create tree panel
    Panel treePanel = new Panel
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(10),
        Margin = new Padding(0, 0, 0, 50) // Add bottom margin for buttons
    };
    
    // Create tree view
    treeViewSessions = new TreeView
    {
        Dock = DockStyle.Fill,
        AllowDrop = true,
        HideSelection = false,
        ShowLines = true,
        ShowPlusMinus = true,
        ShowRootLines = true,
        Font = _dosFont
    };
    
// Add tree event handlers
    treeViewSessions.ItemDrag += TreeView_ItemDrag;
    treeViewSessions.DragEnter += TreeView_DragEnter;
    treeViewSessions.DragOver += TreeView_DragOver;
    treeViewSessions.DragDrop += TreeView_DragDrop;
    treeViewSessions.NodeMouseDoubleClick += TreeView_NodeMouseDoubleClick;
    
    // Create the context menu
    ContextMenuStrip contextMenu = new ContextMenuStrip();
    contextMenu.Opening += ContextMenu_Opening;
    
    // Add menu items
    ToolStripMenuItem editItem = new ToolStripMenuItem("Edit");
    editItem.Name = "Edit";
    editItem.Click += ContextMenu_Edit;
    
    ToolStripMenuItem deleteItem = new ToolStripMenuItem("Delete");
    deleteItem.Name = "Delete";
    deleteItem.Click += ContextMenu_Delete;
    
    ToolStripMenuItem addSessionItem = new ToolStripMenuItem("Add Session");
    addSessionItem.Name = "Add Session";
    addSessionItem.Click += ContextMenu_AddSession;
    
    // Add items to menu
    contextMenu.Items.Add(editItem);
    contextMenu.Items.Add(deleteItem);
    contextMenu.Items.Add(new ToolStripSeparator());
    contextMenu.Items.Add(addSessionItem);
    
    // Set the context menu
    treeViewSessions.ContextMenuStrip = contextMenu;
        
    // Add the tree to the panel
    treePanel.Controls.Add(treeViewSessions);
    
    // Create a fixed-height button panel at the bottom
    Panel buttonPanel = new Panel
    {
        Dock = DockStyle.Bottom,
        Height = 60,
        Padding = new Padding(10)
    };
    
    // Create a flow layout panel for buttons
    FlowLayoutPanel buttonFlow = new FlowLayoutPanel
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        AutoSize = true
    };
    
    // Add folder button
    Button addFolderButton = new Button
    {
        Text = "+ Folder",
        FlatStyle = FlatStyle.Flat,
        Width = 180,
        Height = 40,
        Margin = new Padding(5),
        Font = _dosFont
    };
    addFolderButton.Click += AddFolderButton_Click;
    
    // Add session button
    Button addSessionButton = new Button
    {
        Text = "+ Session",
        FlatStyle = FlatStyle.Flat,
        Width = 180,
        Height = 40,
        Margin = new Padding(5),
        Font = _dosFont
    };
    addSessionButton.Click += AddSessionButton_Click;
    
    // Add buttons to flow panel
    buttonFlow.Controls.Add(addFolderButton);
    buttonFlow.Controls.Add(addSessionButton);
    
    // Create a panel for the OK/Cancel buttons
    Panel okCancelPanel = new Panel
    {
        Dock = DockStyle.Right,
        Width = 280,
        AutoSize = true
    };
    
    // Save button
    Button saveButton = new Button
    {
        Text = "Save & Close",
        FlatStyle = FlatStyle.Flat,
        Width = 130,
        Height = 40,
        Location = new Point(140, 0),
        Font = _dosFont
    };
    saveButton.Click += SaveButton_Click;
    
    // Cancel button
    Button cancelButton = new Button
    {
        Text = "Cancel",
        FlatStyle = FlatStyle.Flat,
        Width = 130,
        Height = 40,
        Location = new Point(0, 0),
        DialogResult = DialogResult.Cancel,
        Font = _dosFont
    };
    cancelButton.Click += (s, e) => this.Close();
    
    // Add OK/Cancel buttons to their panel
    okCancelPanel.Controls.Add(cancelButton);
    okCancelPanel.Controls.Add(saveButton);
    
    // Add panels to the button panel
    buttonPanel.Controls.Add(buttonFlow);
    buttonPanel.Controls.Add(okCancelPanel);
    
    // Add panels to main container
    mainContainer.Controls.Add(treePanel);
    mainContainer.Controls.Add(buttonPanel);
    
    // Add main container and title bar to form
    this.Controls.Add(mainContainer);
    this.Controls.Add(titleBar);
    
    // Set up form events
    this.Paint += SessionEditorDialog_Paint;
    this.Load += (s, e) => { 
        // Center OK/Cancel buttons on load
        int panelWidth = okCancelPanel.Width;
        int totalButtonWidth = cancelButton.Width + saveButton.Width + 10;
        int startX = (panelWidth - totalButtonWidth) / 2;
        cancelButton.Location = new Point(startX, 0);
        saveButton.Location = new Point(startX + cancelButton.Width + 10, 0);
    };
}

        private void SessionEditorDialog_Paint(object sender, PaintEventArgs e)
        {
            // Draw border around form
            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (Pen pen = new Pen(_themeManager?.CurrentTheme != null ? 
                Theme.HexToColor(_themeManager.CurrentTheme.UI.Border) : Color.White, 2))
            {
                e.Graphics.DrawRectangle(pen, rect);
            }
        }
        
        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // For window dragging
                const int WM_NCLBUTTONDOWN = 0xA1;
                const int HT_CAPTION = 0x2;
                
                // Release mouse capture
                ReleaseCapture();
                
                // Send message
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        
        private void LoadSessions()
{
    try
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)  // Match the Navigator's deserializer
            .Build();

        _sessionsData = deserializer.Deserialize<List<FolderData>>(File.ReadAllText(_sessionFilePath));
                DecryptSessionPasswords(_sessionsData);

        treeViewSessions.Nodes.Clear();
        
        foreach (var folder in _sessionsData)
        {
            var folderNode = new TreeNode(folder.FolderName)
            {
                Tag = new NodeData { Type = "folder", Data = folder }
            };
            
            foreach (var session in folder.Sessions)
            {
                var sessionNode = new TreeNode(session.DisplayName)
                {
                    Tag = new NodeData { Type = "session", Data = session }
                };
                folderNode.Nodes.Add(sessionNode);
            }
            
            treeViewSessions.Nodes.Add(folderNode);
        }
        
        treeViewSessions.ExpandAll();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error loading sessions: {ex.Message}", "Error", 
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

private void DecryptSessionPasswords(List<FolderData> folders)
{
    if (folders == null) return;
    
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

private bool SaveSessions()
{
    try
    {
        // Convert tree to data structure
        _sessionsData.Clear();
        
        foreach (TreeNode folderNode in treeViewSessions.Nodes)
        {
            var folder = new FolderData
            {
                FolderName = folderNode.Text,
                Sessions = new List<SessionData>()
            };
            
            foreach (TreeNode sessionNode in folderNode.Nodes)
            {
                var nodeData = (NodeData)sessionNode.Tag;
                var session = (SessionData)nodeData.Data;
                folder.Sessions.Add(session);
            }
            
            _sessionsData.Add(folder);
        }
        
        // Create a deep copy of the session data for encryption
        var serializerForCopy = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();
        var deserializerForCopy = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();
        
        string tempYaml = serializerForCopy.Serialize(_sessionsData);
        var encryptedData = deserializerForCopy.Deserialize<List<FolderData>>(tempYaml);
        
        // Encrypt passwords in the copy
        EncryptSessionPasswords(encryptedData);
        
        // Serialize the encrypted copy to YAML
        var serializer = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();
        
        string yaml = serializer.Serialize(encryptedData);
        File.WriteAllText(_sessionFilePath, yaml);
        
        return true;
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error saving sessions: {ex.Message}", "Error", 
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }
}


private void EncryptSessionPasswords(List<FolderData> folders)
{
    if (folders == null) return;
    
    foreach (var folder in folders)
    {
        if (folder.Sessions != null)
        {
            foreach (var session in folder.Sessions)
            {
                // Only encrypt if there's a password and it's not already encrypted
                if (!string.IsNullOrEmpty(session.Password) && !PasswordManager.IsEncrypted(session.Password))
                {
                    try
                    {
                        session.Password = PasswordManager.EncryptPassword(session.Password);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing other sessions
                        Console.WriteLine($"Error encrypting password for {session.DisplayName}: {ex.Message}");
                    }
                }
            }
        }
    }
}

       
       
       // Tree drag and drop handling
        private void TreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            _dragNode = (TreeNode)e.Item;
            if (_dragNode != null)
                DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void TreeView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
        }

        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            Point targetPoint = treeViewSessions.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeViewSessions.GetNodeAt(targetPoint);
            
            if (targetNode == null)
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            
            NodeData dragData = (NodeData)_dragNode.Tag;
            NodeData targetData = (NodeData)targetNode.Tag;
            
            // Validate drag operation
            if (dragData.Type == "session" && targetData.Type == "folder")
            {
                // Allow dropping sessions onto folders
                e.Effect = DragDropEffects.Move;
            }
            else if (dragData.Type == "session" && targetData.Type == "session" && 
                     _dragNode.Parent == targetNode.Parent)
            {
                // Allow reordering sessions within the same folder
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void TreeView_DragDrop(object sender, DragEventArgs e)
        {
            Point targetPoint = treeViewSessions.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeViewSessions.GetNodeAt(targetPoint);
            
            if (targetNode == null || _dragNode == null)
                return;
            
            NodeData dragData = (NodeData)_dragNode.Tag;
            NodeData targetData = (NodeData)targetNode.Tag;
            
            if (dragData.Type == "session" && targetData.Type == "folder")
            {
                // Move session to a different folder
                TreeNode dragNodeClone = (TreeNode)_dragNode.Clone();
                _dragNode.Remove();
                targetNode.Nodes.Add(dragNodeClone);
                targetNode.Expand();
                treeViewSessions.SelectedNode = dragNodeClone;
            }
            else if (dragData.Type == "session" && targetData.Type == "session" && 
                     _dragNode.Parent == targetNode.Parent)
            {
                // Reorder within the same folder
                int targetIndex = targetNode.Index;
                TreeNode parentNode = targetNode.Parent;
                
                TreeNode dragNodeClone = (TreeNode)_dragNode.Clone();
                _dragNode.Remove();
                
                parentNode.Nodes.Insert(targetIndex, dragNodeClone);
                treeViewSessions.SelectedNode = dragNodeClone;
            }
        }

        // Context menu handling
        private void ContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TreeNode selectedNode = treeViewSessions.SelectedNode;
            if (selectedNode == null)
            {
                e.Cancel = true;
                return;
            }
            
            ContextMenuStrip menu = (ContextMenuStrip)sender;
            NodeData nodeData = (NodeData)selectedNode.Tag;
            
            // Customize menu based on node type
            menu.Items["Add Session"].Visible = (nodeData.Type == "folder");
        }

        private void ContextMenu_Edit(object sender, EventArgs e)
        {
            EditSelectedNode();
        }
        
        private void ContextMenu_AddSession(object sender, EventArgs e)
        {
            AddSessionToSelectedFolder();
        }

        private void ContextMenu_Delete(object sender, EventArgs e)
        {
            DeleteSelectedNode();
        }

        private void TreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            EditSelectedNode();
        }

        private void EditSelectedNode()
        {
            TreeNode selectedNode = treeViewSessions.SelectedNode;
            if (selectedNode == null)
                return;
            
            NodeData nodeData = (NodeData)selectedNode.Tag;
            
            if (nodeData.Type == "folder")
            {
                // Simple folder name edit
                using (var dialog = new TextEntryDialog("Edit Folder", "Folder name:", selectedNode.Text, _themeManager))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dialog.EnteredText))
                    {
                        selectedNode.Text = dialog.EnteredText;
                    }
                }
            }
            else // "session"
            {
                // Edit session in property dialog
                SessionData sessionData = (SessionData)nodeData.Data;
                using (var dialog = new SessionPropertyDialog(sessionData, _themeManager, _dosFont))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        // Update session data
                        SessionData updatedData = dialog.GetSessionData();
                        selectedNode.Tag = new NodeData { Type = "session", Data = updatedData };
                        selectedNode.Text = updatedData.DisplayName;
                    }
                }
            }
        }

        private void DeleteSelectedNode()
        {
            TreeNode selectedNode = treeViewSessions.SelectedNode;
            if (selectedNode == null)
                return;
            
            string nodeType = ((NodeData)selectedNode.Tag).Type == "folder" ? "folder" : "session";
            
            DialogResult result = MessageBox.Show(
                $"Are you sure you want to delete this {nodeType}?\n\n{selectedNode.Text}",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                selectedNode.Remove();
            }
        }

        private void AddFolderButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new TextEntryDialog("New Folder", "Folder name:", "", _themeManager))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dialog.EnteredText))
                {
                var folder = new FolderData { FolderName = dialog.EnteredText };

                    var folderNode = new TreeNode(dialog.EnteredText)
                    {
                        Tag = new NodeData { Type = "folder", Data = folder }
                    };
                    
                    treeViewSessions.Nodes.Add(folderNode);
                    treeViewSessions.SelectedNode = folderNode;
                }
            }
        }

        private void AddSessionButton_Click(object sender, EventArgs e)
        {
            AddSessionToSelectedFolder();
        }
        
        private void AddSessionToSelectedFolder()
        {
            TreeNode selectedNode = treeViewSessions.SelectedNode;
            TreeNode folderNode = null;
            
            if (selectedNode == null)
            {
                if (treeViewSessions.Nodes.Count > 0)
                    folderNode = treeViewSessions.Nodes[0]; // Default to first folder
                else
                {
                    MessageBox.Show("Please create a folder first.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                NodeData nodeData = (NodeData)selectedNode.Tag;
                folderNode = nodeData.Type == "folder" ? selectedNode : selectedNode.Parent;
            }
            
            var sessionData = new SessionData();
            using (var dialog = new SessionPropertyDialog(sessionData, _themeManager, _dosFont))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    SessionData newSession = dialog.GetSessionData();
                    var sessionNode = new TreeNode(newSession.DisplayName)
                    {
                        Tag = new NodeData { Type = "session", Data = newSession }
                    };
                    
                    folderNode.Nodes.Add(sessionNode);
                    folderNode.Expand();
                    treeViewSessions.SelectedNode = sessionNode;
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (SaveSessions())
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
    
    // Helper class for storing node data
    public class NodeData
    {
        public string Type { get; set; } // "folder" or "session"
        public object Data { get; set; } // FolderData or SessionData
    }
}