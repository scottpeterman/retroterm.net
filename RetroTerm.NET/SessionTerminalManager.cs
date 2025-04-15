using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SSHTerminalManager
{

public class SessionData
{
    [YamlMember(Alias = "DeviceType", ApplyNamingConventions = false)]
    public string DeviceType { get; set; } = "Linux";

    public string Model { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string SoftwareVersion { get; set; } = "";
    public string Vendor { get; set; } = "";

    [YamlMember(Alias = "credsid", ApplyNamingConventions = false)]
    public string CredsId { get; set; } = "";

    [YamlMember(Alias = "display_name", ApplyNamingConventions = false)]
    public string DisplayName { get; set; } = "New Session";

    [YamlMember(Alias = "host", ApplyNamingConventions = false)]
    public string Host { get; set; } = "";

    [YamlMember(Alias = "port", ApplyNamingConventions = false)]
    public int Port { get; set; } = 22;

    // Additional properties
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class FolderData
{
    [YamlMember(Alias = "FolderName", ApplyNamingConventions = false)]
    public string FolderName { get; set; } = "";
    
    [YamlMember(Alias = "sessions", ApplyNamingConventions = false)]
    public List<SessionData> Sessions { get; set; } = new List<SessionData>();
}


    public partial class SessionEditorDialog : Form
    {
        private readonly string _sessionFilePath;
        private List<FolderData> _sessionsData = new List<FolderData>();
        private TreeNode _dragNode;

        public SessionEditorDialog(string sessionFilePath)
        {
            InitializeComponent();
            _sessionFilePath = sessionFilePath;
            LoadSessions();
        }

        private void InitializeComponent()
        {
            // Form setup
            this.Text = "Edit Sessions";
            this.Size = new System.Drawing.Size(800, 600);

            // Create tree view
            var treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                AllowDrop = true,
                HideSelection = false
            };

            // Add event handlers for drag and drop
            treeView.ItemDrag += TreeView_ItemDrag;
            treeView.DragEnter += TreeView_DragEnter;
            treeView.DragOver += TreeView_DragOver;
            treeView.DragDrop += TreeView_DragDrop;
            treeView.NodeMouseDoubleClick += TreeView_NodeMouseDoubleClick;

            // Add context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Edit", null, ContextMenu_Edit);
            contextMenu.Items.Add("Delete", null, ContextMenu_Delete);
            contextMenu.Opening += ContextMenu_Opening;
            treeView.ContextMenuStrip = contextMenu;

            // Buttons
            var addFolderButton = new Button
            {
                Text = "Add Folder",
                Width = 100
            };
            addFolderButton.Click += AddFolderButton_Click;

            var addSessionButton = new Button
            {
                Text = "Add Session",
                Width = 100
            };
            addSessionButton.Click += AddSessionButton_Click;

            var saveButton = new Button
            {
                Text = "Save & Close",
                Width = 100
            };
            saveButton.Click += SaveButton_Click;

            // Button layout
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };
            
            addFolderButton.Location = new System.Drawing.Point(10, 10);
            addSessionButton.Location = new System.Drawing.Point(120, 10);
            saveButton.Location = new System.Drawing.Point(690, 10);
            
            buttonPanel.Controls.Add(addFolderButton);
            buttonPanel.Controls.Add(addSessionButton);
            buttonPanel.Controls.Add(saveButton);

            // Add controls to form
            this.Controls.Add(treeView);
            this.Controls.Add(buttonPanel);

            // Save references
            this.treeViewSessions = treeView;
        }

        private TreeView treeViewSessions;

private void LoadSessions()
{
    try
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

        _sessionsData = deserializer.Deserialize<List<FolderData>>(File.ReadAllText(_sessionFilePath));
        
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
        
        // treeViewSessions.ExpandAll();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error loading sessions: {ex.Message}", "Error", 
            MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        
        // Serialize to YAML
        var serializer = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();
        
        string yaml = serializer.Serialize(_sessionsData);
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
                string folderName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter folder name:", "Edit Folder", selectedNode.Text);
                
                if (!string.IsNullOrEmpty(folderName))
                    selectedNode.Text = folderName;
            }
            else // "session"
            {
                // Edit session in property dialog
                SessionData sessionData = (SessionData)nodeData.Data;
                using (var dialog = new SessionPropertyDialog(sessionData))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
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
            
            DialogResult result = MessageBox.Show(
                $"Are you sure you want to delete '{selectedNode.Text}'?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                selectedNode.Remove();
            }
        }

        private void AddFolderButton_Click(object sender, EventArgs e)
        {
            string folderName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter folder name:", "New Folder", "");
                
            if (!string.IsNullOrEmpty(folderName))
            {
                var folder = new FolderData { FolderName = folderName };
                var folderNode = new TreeNode(folderName)
                {
                    Tag = new NodeData { Type = "folder", Data = folder }
                };
                
                treeViewSessions.Nodes.Add(folderNode);
                treeViewSessions.SelectedNode = folderNode;
            }
        }

        private void AddSessionButton_Click(object sender, EventArgs e)
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
            using (var dialog = new SessionPropertyDialog(sessionData))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
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

    // Helper classes
    public class NodeData
    {
        public string Type { get; set; } // "folder" or "session"
        public object Data { get; set; } // FolderData or SessionData
    }

    public class SessionPropertyDialog : Form
    {
        private SessionData _sessionData;
        private Dictionary<string, Control> _fields = new Dictionary<string, Control>();

        public SessionPropertyDialog(SessionData sessionData)
        {
            _sessionData = sessionData;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Edit Session Properties";
            this.Size = new System.Drawing.Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 10,
                Padding = new Padding(10)
            };
            
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            
            // Create form fields
            AddFormField(layout, 0, "Display Name:", new TextBox());
            AddFormField(layout, 1, "Host:", new TextBox());
            
            NumericUpDown portSpinner = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 65535,
                Value = 22
            };
            AddFormField(layout, 2, "Port:", portSpinner);
            
            ComboBox deviceTypeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            deviceTypeCombo.Items.AddRange(new object[] { "Linux", "cisco_ios", "hp_procurve" });
            AddFormField(layout, 3, "Device Type:", deviceTypeCombo);
            
            AddFormField(layout, 4, "Model:", new TextBox());
            AddFormField(layout, 5, "Serial Number:", new TextBox());
            AddFormField(layout, 6, "Software Version:", new TextBox());
            AddFormField(layout, 7, "Vendor:", new TextBox());
            AddFormField(layout, 8, "Credentials ID:", new TextBox());
            
            // Add buttons
            TableLayoutPanel buttonPanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                Dock = DockStyle.Fill
            };
            
            Button saveButton = new Button { Text = "Save", DialogResult = DialogResult.OK };
            saveButton.Click += (s, e) => this.Close();
            
            Button cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
            cancelButton.Click += (s, e) => this.Close();
            
            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(cancelButton);
            
            layout.Controls.Add(buttonPanel, 1, 9);
            
            this.Controls.Add(layout);
            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
        }
        
        private void AddFormField(TableLayoutPanel layout, int row, string label, Control control)
        {
            layout.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, row);
            layout.Controls.Add(control, 1, row);
            
            control.Dock = DockStyle.Fill;
            string fieldName = label.Replace(":", "").Replace(" ", "");
            _fields[fieldName] = control;
        }
        
        private void LoadData()
        {
            // Set field values from session data
            SetFieldValue("DisplayName", _sessionData.DisplayName);
            SetFieldValue("Host", _sessionData.Host);
            SetFieldValue("Port", _sessionData.Password);
            SetFieldValue("DeviceType", _sessionData.DeviceType);
            SetFieldValue("Model", _sessionData.Model);
            SetFieldValue("SerialNumber", _sessionData.SerialNumber);
            SetFieldValue("SoftwareVersion", _sessionData.SoftwareVersion);
            SetFieldValue("Vendor", _sessionData.Vendor);
            // SetFieldValue("CredentialsID", _sessionData.credsid);
        }
        
        private void SetFieldValue(string fieldName, string value)
        {
            if (!_fields.TryGetValue(fieldName, out Control control))
                return;
                
            if (control is TextBox textBox)
            {
                textBox.Text = value ?? "";
            }
            else if (control is ComboBox comboBox)
            {
                int index = comboBox.FindStringExact(value);
                comboBox.SelectedIndex = index >= 0 ? index : 0;
            }
            else if (control is NumericUpDown spinner)
            {
                if (int.TryParse(value, out int portNumber))
                    spinner.Value = portNumber;
                else
                    spinner.Value = 22; // Default port
            }
        }
        
        public SessionData GetSessionData()
{
    var data = new SessionData
    {
        DisplayName = GetFieldValue("DisplayName"),
        Host = GetFieldValue("Host"),
        Port = int.TryParse(GetFieldValue("Port"), out int portValue) ? portValue : 22,
        DeviceType = GetFieldValue("DeviceType"),
        Model = GetFieldValue("Model"),
        SerialNumber = GetFieldValue("SerialNumber"),
        SoftwareVersion = GetFieldValue("SoftwareVersion"),
        Vendor = GetFieldValue("Vendor"),
        CredsId = GetFieldValue("CredentialsID")  // Changed from credsid to CredsId
    };
    
    return data;
}
        
        private string GetFieldValue(string fieldName)
        {
            if (!_fields.TryGetValue(fieldName, out Control control))
                return "";
                
            if (control is TextBox textBox)
            {
                return textBox.Text;
            }
            else if (control is ComboBox comboBox)
            {
                return comboBox.SelectedItem?.ToString() ?? "";
            }
            else if (control is NumericUpDown spinner)
            {
                return spinner.Value.ToString();
            }
            
            return "";
        }
    }
}