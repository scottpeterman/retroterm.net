using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Aga.Controls.Tree;

namespace RetroTerm.NET
{
    public class ContainedTreeView : Panel
    {
        private TreeViewAdv treeView;
        
        // TreeViewAdv model
        private TreeModel treeModel;
        
        // Events to forward
        public event EventHandler<TreeNodeAdvMouseEventArgs> NodeMouseDoubleClick;
        public event MouseEventHandler TreeMouseUp;
        private Models.Theme currentTheme;
        
        public ContainedTreeView()
        {
            // Configure panel
            this.Padding = new Padding(0);
            this.Margin = new Padding(0);
            
            // Initialize the tree model
            treeModel = new TreeModel();
            
            // Create TreeViewAdv with native scrollbars
            treeView = new TreeViewAdv
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                UseColumns = false,
                ShowLines = true,
                FullRowSelect = true,
                Model = treeModel,
                
            };
            this.AutoScroll = true;

            // Set a fixed row height
            treeView.RowHeight = 25;
            treeView.MaximumSize = new Size(0, 0); // No maximum size restriction

            // Add a text renderer
            Aga.Controls.Tree.NodeControls.NodeTextBox textRenderer = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            textRenderer.DataPropertyName = "Text"; // Binds to the Text property on Node objects
            textRenderer.LeftMargin = 3; // Add some margin
            textRenderer.ParentColumn = null; // Not using columns
            
            // Subscribe to the DrawText event to customize the appearance
            textRenderer.DrawText += TextRenderer_DrawText;
            
            treeView.NodeControls.Add(textRenderer);
            
            // Forward events
            treeView.NodeMouseDoubleClick += (s, e) => NodeMouseDoubleClick?.Invoke(s, e);
            treeView.MouseUp += (s, e) => TreeMouseUp?.Invoke(s, e);
            
            // Add control
            this.Controls.Add(treeView);
        }
        
        // Add after the constructor
        // Add after the constructor
public void CustomizeSelectionColors(Color selectionBackColor)
{
    if (treeView != null)
    {
        try
        {
            // Since TreeViewAdv doesn't expose selection color directly,
            // we need to handle it via the NodeControls
            foreach (var control in treeView.NodeControls)
            {
                // Use reflection to set selection color properties
                var type = control.GetType();
                
                // Try to set various selection-related properties
                var props = type.GetProperties(System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                
                foreach (var prop in props)
                {
                    if (prop.Name.Contains("Selection") && prop.PropertyType == typeof(Color))
                    {
                        try
                        {
                            prop.SetValue(control, selectionBackColor);
                            Console.WriteLine($"Set selection property: {prop.Name}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to set {prop.Name}: {ex.Message}");
                        }
                    }
                }
            }
            
            // Try to set selection color through DrawEventArgs in custom handlers
            // Subscribe to drawing events to override selection colors
            if (treeView.NodeControls != null)
            {
                // For the text renderer
                foreach (var control in treeView.NodeControls)
                {
                    if (control is Aga.Controls.Tree.NodeControls.NodeTextBox textBox)
                    {
                        // Modify the existing DrawText handler
                        textBox.DrawText -= TextRenderer_DrawText;
                        textBox.DrawText += (s, e) => {
                            if (e.Node.IsSelected)
                            {
                                e.TextColor = Color.White; // Text color for selected nodes
                                // If we can, set background color too
                                var backColor = e.GetType().GetProperty("BackColor");
                                if (backColor != null && backColor.CanWrite)
                                {
                                    backColor.SetValue(e, selectionBackColor);
                                }
                            }
                            else
                            {
                                // Normal text color from theme
                                if (currentTheme != null)
                                {
                                    e.TextColor = Models.Theme.HexToColor(currentTheme.UI.Text);
                                }
                                else
                                {
                                    e.TextColor = Color.White; // Default fallback color
                                }
                            }
                        };
                    }
                }
            }
            
            // Force redraw
            treeView.Invalidate(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error customizing selection colors: {ex.Message}");
        }
    }
}

        
        public void ExpandAll()
        {
            treeView.ExpandAll();
        }
        
        // Event handler for customizing the text appearance
        private void TextRenderer_DrawText(object sender, Aga.Controls.Tree.NodeControls.DrawEventArgs e)
        {
            // Set the text color based on the current theme
            if (currentTheme != null)
            {
                e.TextColor = Models.Theme.HexToColor(currentTheme.UI.Text);
            }
            else
            {
                e.TextColor = Color.White; // Default fallback color
            }
        }
        
        // Load data into the tree

public void Load(List<SessionFolder> folders, Form parentForm = null)
{
    if (!PasswordManager._isInitialized)  // If this is a public field or has a public getter
        {
            PasswordManager.Initialize(parentForm);
        }
    // Clear tree model
    treeModel.Nodes.Clear();
    
    // Add nodes from folders collection
    foreach (var folder in folders)
    {
        var folderNode = new SessionFolderNode(folder.FolderName);
        
        // Add sessions
        if (folder.Sessions != null)
        {
            foreach (var session in folder.Sessions)
            {
                // Decrypt password if it's encrypted
                if (!string.IsNullOrEmpty(session.Password) && PasswordManager.IsEncrypted(session.Password))
                {
                    session.Password = PasswordManager.DecryptPassword(session.Password);
                    
                }
                
                string displayText = !string.IsNullOrEmpty(session.DisplayName) 
                    ? session.DisplayName 
                    : session.Host;
                    
                var sessionNode = new SessionNode(displayText, session);
                folderNode.Nodes.Add(sessionNode);
            }
        }
        
        treeModel.Nodes.Add(folderNode);
    }
    
    // Refresh TreeViewAdv
    treeView.Model = treeModel;
}

        // Public method to get a node at a specific point
        public TreeNodeAdv GetNodeAtPoint(Point point)
        {
            return treeView.GetNodeAt(point);
        }
        
        // Apply theme
        
        // Modify the ApplyTheme method in ContainedTreeView.cs
        
        public void ApplyTheme(Models.Theme theme)
{
    if (theme == null) return;
    
    try
    {
        // Store the theme for use in event handlers
        currentTheme = theme;
        
        // Apply theme to controls
        this.BackColor = Models.Theme.HexToColor(theme.UI.Background);
        treeView.BackColor = Models.Theme.HexToColor(theme.UI.Background);
        
        // Set selection color to match theme's highlight color
        CustomizeSelectionColors(Models.Theme.HexToColor(theme.UI.Highlight));
        
        // Force redraw of the tree view to apply the new colors
        treeView.Invalidate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying theme: {ex.Message}");
        
        // Fallback to safe values
        treeView.BackColor = Color.Black;
    }
}
       

       
        public class SessionFolderNode : Node
        {
            public string FolderName { get; set; }
            
            public SessionFolderNode(string folderName)
            {
                FolderName = folderName;
                Text = folderName;
            }
            
            public override string ToString()
            {
                return FolderName;
            }
        }
        
}

        public class SessionNode : Node
        {
            public SessionData Session { get; set; }
            public string DisplayName { get; set; }
            
            public SessionNode(string displayName, SessionData session)
            {
                DisplayName = displayName;
                Session = session;
                Text = displayName;
            }
            
            public override string ToString()
            {
                return DisplayName;
            }
        }
    }