using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SshTerminalComponent;

namespace SshTerminalComponent.TestApp
{
    public partial class ConnectionDirectoryForm : Form
    {
        private readonly ConnectionDirectoryService _directoryService = new ConnectionDirectoryService();
        private Dictionary<int, ConnectionProfile> _profiles;
        private TextBox _selectionTextBox;
        private Panel _mainPanel;
        private Label[] _connectionLabels = new Label[10];
        
        // Reference to the theme manager
        private ThemeManager _themeManager;
        
        public ConnectionProfile SelectedProfile { get; private set; }
        
        public ConnectionDirectoryForm(ThemeManager themeManager)
        {
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
            
            // Subscribe to theme change events
            _themeManager.ThemeChanged += ThemeManager_ThemeChanged;
            
            InitializeComponent();
            LoadProfiles();
            
            // Apply the current theme
            ApplyCurrentTheme();
        }
        
        // Theme change handler
        private void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            ApplyCurrentTheme();
        }
        
        // Apply theme colors to form elements
        private void ApplyCurrentTheme()
        {
            var theme = _themeManager.CurrentTheme;
            if (theme == null) return;
            
            // Apply main form colors
            this.BackColor = Theme.HexToColor(theme.UI.Background);
            this.ForeColor = Theme.HexToColor(theme.UI.Text);
            
            // Update all controls recursively
            UpdateControlColors(this, theme);
        }
        
        // Helper to recursively update control colors
        private void UpdateControlColors(Control parent, Theme theme)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Label label)
                {
                    // Special handling for function key labels in the gray bar
                    if (control.Parent is FlowLayoutPanel && control.Parent.BackColor == Color.FromArgb(192, 192, 192))
                    {
                        // Don't change these labels' backgrounds as they have custom painting
                        continue;
                    }
                    
                    label.BackColor = Theme.HexToColor(theme.UI.Background);
                    label.ForeColor = Theme.HexToColor(theme.UI.Text);
                }
                else if (control is TextBox textBox)
                {
                    textBox.BackColor = Theme.HexToColor(theme.UI.Background);
                    textBox.ForeColor = Theme.HexToColor(theme.UI.InputText);
                }
                else if (control is Button button)
                {
                    button.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
                    button.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
                    button.FlatAppearance.BorderColor = Theme.HexToColor(theme.UI.ButtonBorder);
                }
                else if (control is Panel panel)
                {
                    // For the function key panel (assuming it's the panel with height 40)
                    if (panel.Height == 40)
                    {
                        panel.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                        panel.ForeColor = Theme.HexToColor(theme.UI.MenuText);
                    }
                    else
                    {
                        panel.BackColor = Theme.HexToColor(theme.UI.Background);
                        panel.ForeColor = Theme.HexToColor(theme.UI.Text);
                    }
                }
                else if (control is FlowLayoutPanel flowPanel)
                {
                    // For the flow panel containing function keys
                    if (flowPanel.Parent is Panel && flowPanel.Parent.Height == 40)
                    {
                        flowPanel.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                        flowPanel.ForeColor = Theme.HexToColor(theme.UI.MenuText);
                    }
                    else
                    {
                        flowPanel.BackColor = Theme.HexToColor(theme.UI.Background);
                        flowPanel.ForeColor = Theme.HexToColor(theme.UI.Text);
                    }
                }
                
                // Update any child controls
                if (control.Controls.Count > 0)
                {
                    UpdateControlColors(control, theme);
                }
            }
        }
        

// Replace the InitializeComponent method with this version
private void InitializeComponent()
{
    
    // Get theme colors (or fallback to defaults if theme manager not available yet)
    Color bgColor = _themeManager?.CurrentTheme != null 
        ? Theme.HexToColor(_themeManager.CurrentTheme.UI.Background) 
        : Color.FromArgb(0, 0, 170); // Default Borland Blue

    Color textColor = _themeManager?.CurrentTheme != null 
        ? Theme.HexToColor(_themeManager.CurrentTheme.UI.Text) 
        : Color.FromArgb(255, 255, 255); // Default White

    Color inputTextColor = _themeManager?.CurrentTheme != null 
        ? Theme.HexToColor(_themeManager.CurrentTheme.UI.InputText) 
        : Color.FromArgb(255, 255, 0); // Default Yellow

    Color buttonBgColor = _themeManager?.CurrentTheme != null 
        ? Theme.HexToColor(_themeManager.CurrentTheme.UI.ButtonBackground) 
        : Color.FromArgb(0, 170, 170); // Default Cyan

    Color buttonTextColor = _themeManager?.CurrentTheme != null 
        ? Theme.HexToColor(_themeManager.CurrentTheme.UI.ButtonText) 
        : Color.FromArgb(0, 0, 0); // Default Black

    Color borderColor = _themeManager?.CurrentTheme != null 
        ? Theme.HexToColor(_themeManager.CurrentTheme.UI.Border) 
        : Color.FromArgb(255, 255, 255); // Default White

    Color menuBgColor = _themeManager?.CurrentTheme != null 
        ? Theme.HexToColor(_themeManager.CurrentTheme.UI.MenuBackground) 
        : Color.FromArgb(192, 192, 192); // Default Gray

    Color fnKeyColor = _themeManager?.CurrentTheme != null 
        ? Theme.HexToColor(_themeManager.CurrentTheme.UI.FunctionKeyText) 
        : Color.FromArgb(255, 0, 0); // Default Red

    Color fnKeyDescColor = _themeManager?.CurrentTheme != null 
        ? Theme.HexToColor(_themeManager.CurrentTheme.UI.FunctionKeyDescriptionText) 
        : Color.FromArgb(0, 0, 0); // Default Black

    // Form setup
    this.FormBorderStyle = FormBorderStyle.None;
    this.BackColor = bgColor;
    this.ForeColor = textColor;
    this.StartPosition = FormStartPosition.CenterParent;
    this.Size = new Size(900, 600);
    this.KeyPreview = true;
    
    // Create a TableLayoutPanel to manage the layout
    TableLayoutPanel tableLayout = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        RowCount = 5,  // Added an extra row for spacing
        ColumnCount = 1,
        BackColor = bgColor
    };
    
    // Configure row styles
    tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Title
    tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Main content
    tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Selection (increased height)
    tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));   // Spacing row (increased)
    tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // Function keys
    
    // Create title label
    Label titleLabel = new Label
    {
        Text = " CONNECTION DIRECTORY ",
        Dock = DockStyle.Fill,
        Padding = new Padding(5),
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font(FontFamily.GenericMonospace, 12, FontStyle.Bold),
        ForeColor = textColor,
        BackColor = bgColor
    };
    
    // Main panel with border
    _mainPanel = new Panel
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(5),
        BackColor = bgColor,
        Margin = new Padding(5)
    };
    
    // Draw DOS-style double-line border with theme colors
    _mainPanel.Paint += (s, e) => {
        // Get current theme color for border - this handles runtime theme changes
        Color currentBorderColor = _themeManager?.CurrentTheme != null 
            ? Theme.HexToColor(_themeManager.CurrentTheme.UI.Border) 
            : borderColor;
        
        // Draw outer border
        e.Graphics.DrawRectangle(new Pen(currentBorderColor), 0, 0, 
            _mainPanel.Width - 1, _mainPanel.Height - 1);
        
        // Draw inner border (double-line effect)
        e.Graphics.DrawRectangle(new Pen(currentBorderColor), 2, 2, 
            _mainPanel.Width - 5, _mainPanel.Height - 5);
    };
    
    // Panel for connection list
    Panel listPanel = new Panel
    {
        Dock = DockStyle.Fill,
        AutoScroll = false,
        Margin = new Padding(10),
        Padding = new Padding(10),
        BackColor = bgColor
    };
    
    // Create labels for each connection slot
    for (int i = 0; i < 10; i++)
    {
        _connectionLabels[i] = new Label
        {
            Text = $"{i + 1}. [Empty]",
            ForeColor = textColor,
            BackColor = bgColor,
            Font = new Font("Consolas", 13, FontStyle.Regular), // Use Consolas for better monospace rendering
            Location = new Point(15, 15 + (i * 40)),
            Size = new Size(550, 50), // Increased height for better visibility
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false // Ensure fixed size
        };
        
        listPanel.Controls.Add(_connectionLabels[i]);
    }
    
    // Selection panel
    Panel selectionPanel = new Panel
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(5, 5, 5, 10), // Added more padding at the bottom
        BackColor = bgColor
    };
    
    Label selectionLabel = new Label
    {
        Text = "Selection (1-10):",
        ForeColor = textColor,
        BackColor = bgColor,
        Font = new Font(FontFamily.GenericMonospace, 12, FontStyle.Regular),
        Location = new Point(15, 8),
        Size = new Size(350, 30),
        TextAlign = ContentAlignment.MiddleLeft
    };
    
    _selectionTextBox = new TextBox
    {
        BackColor = bgColor,
        ForeColor = inputTextColor,
        Font = new Font(FontFamily.GenericMonospace, 12, FontStyle.Regular),
        Location = new Point(380, 8),  // Moved up slightly
        Size = new Size(40, 30),  // Adjusted height
        MaxLength = 2,
        BorderStyle = BorderStyle.FixedSingle  // Ensure border is visible
    };
    
    _selectionTextBox.KeyPress += (s, e) => {
        // Only allow digits 1-9 and control characters
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
        {
            e.Handled = true;
        }
        
        // Limit input to 1-10
        if (char.IsDigit(e.KeyChar))
        {
            string newText = _selectionTextBox.Text + e.KeyChar;
            int value;
            if (int.TryParse(newText, out value))
            {
                if (value < 1 || value > 10)
                {
                    e.Handled = true;
                }
            }
        }
    };
    
    selectionPanel.Controls.Add(selectionLabel);
    selectionPanel.Controls.Add(_selectionTextBox);
    
    // Function key panel
    Panel functionPanel = new Panel
    {
        Dock = DockStyle.Fill,
        BackColor = menuBgColor, // Using theme menu background color
        ForeColor = fnKeyDescColor,  // Using theme function key description color
        Margin = new Padding(0, 0, 0, 0)  // No need for margin since we have a spacing row
    };
    
    // Create a FlowLayoutPanel for function keys
    FlowLayoutPanel keyFlow = new FlowLayoutPanel
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        AutoScroll = false,
        BackColor = Color.Transparent,
        Padding = new Padding(10, 5, 10, 5)
    };
    
    // Function key labels with more spacing
    string[] functionKeys = {
        "F2-Edit",  "F5/Enter-Connect", "Esc-Cancel"
    };
    
    foreach (var keyText in functionKeys)
    {
        Label keyLabel = new Label
        {
            AutoSize = true,
            Margin = new Padding(15, 3, 15, 3), // Increased horizontal spacing
            Text = keyText,
            Font = new Font(FontFamily.GenericMonospace, 12, FontStyle.Regular),
            BackColor = Color.Transparent
        };
        
        // Custom paint for Turbo C style (F-key in theme color)
        keyLabel.Paint += (s, e) =>
        {
            e.Graphics.Clear(menuBgColor); // Use theme menu background color
            
            // Get current function key colors in case theme changes at runtime
            Color currentFnKeyColor = _themeManager?.CurrentTheme != null 
                ? Theme.HexToColor(_themeManager.CurrentTheme.UI.FunctionKeyText) 
                : fnKeyColor;
                
            Color currentFnDescColor = _themeManager?.CurrentTheme != null 
                ? Theme.HexToColor(_themeManager.CurrentTheme.UI.FunctionKeyDescriptionText) 
                : fnKeyDescColor;
            
            string text = ((Label)s).Text;
            int dashIndex = text.IndexOf('-');
            if (dashIndex > 0)
            {
                string key = text.Substring(0, dashIndex);
                string desc = text.Substring(dashIndex);
                
                // Draw the function key part in theme function key color
                using (Brush keyBrush = new SolidBrush(currentFnKeyColor))
                {
                    e.Graphics.DrawString(key, ((Label)s).Font, keyBrush, 0, 0);
                }
                
                // Get key width to position description
                SizeF keySize = e.Graphics.MeasureString(key, ((Label)s).Font);
                
                // Draw the description in theme function key description color
                using (Brush descBrush = new SolidBrush(currentFnDescColor))
                {
                    e.Graphics.DrawString(desc, ((Label)s).Font, descBrush, keySize.Width, 0);
                }
            }
        };
        
        keyFlow.Controls.Add(keyLabel);
    }
    
    // Add the flow layout to the function panel
    functionPanel.Controls.Add(keyFlow);
    
    // Add the panels to the main panel
    _mainPanel.Controls.Add(listPanel);
    
    // Add all components to the table layout
    tableLayout.Controls.Add(titleLabel, 0, 0);
    tableLayout.Controls.Add(_mainPanel, 0, 1);
    tableLayout.Controls.Add(selectionPanel, 0, 2);
    // Row 3 is empty for spacing
    tableLayout.Controls.Add(functionPanel, 0, 4);
    
    // Add the table layout to the form
    this.Controls.Add(tableLayout);
    
    // Add event handlers for keyboard
    this.KeyDown += ConnectionDirectoryForm_KeyDown;
    
    // Set focus to selection textbox
    this.Load += (s, e) => _selectionTextBox.Focus();

    }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Check if it's a function key
            if (keyData == Keys.F2 || keyData == Keys.F3 || 
                keyData == Keys.F4 || keyData == Keys.F5 || keyData == Keys.Enter)
            {
                // Create a KeyEventArgs object
                KeyEventArgs e = new KeyEventArgs(keyData);
                
                // Call our handler
                ConnectionDirectoryForm_KeyDown(this, e);
                
                // Return true to indicate we've handled the key
                return true;
            }
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void LoadProfiles()
        {
            _profiles = _directoryService.GetAllProfiles();
            
            // Update labels with proper formatting
            for (int i = 1; i <= 10; i++)
            {
                if (_profiles.TryGetValue(i, out ConnectionProfile profile))
                {
                    // Properly formatted text with spacing
                    _connectionLabels[i-1].Text = string.Format("{0}. {1,-20} {2}@{3}:{4}",
                        i,
                        profile.Name,
                        profile.Username,
                        profile.Host,
                        profile.Port);
                }
                else
                {
                    _connectionLabels[i-1].Text = $"{i}. [Empty]";
                }
            }
        }

        private void ConnectionDirectoryForm_KeyDown(object sender, KeyEventArgs e)
        {
            int selection = 0;
            if (!string.IsNullOrEmpty(_selectionTextBox.Text))
            {
                int.TryParse(_selectionTextBox.Text, out selection);
            }
            
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    break;
                
                case Keys.F2: // Edit
                    if (selection >= 1 && selection <= 10)
                    {
                        EditProfile(selection);
                    }
                    break;
                
                case Keys.F3: // New
                    if (selection >= 1 && selection <= 10)
                    {
                        CreateNewProfile(selection);
                    }
                    break;
                
                case Keys.F5: // Connect
                {
                    ConnectionProfile profileF5;
                    if (selection >= 1 && selection <= 10 && _profiles.TryGetValue(selection, out profileF5))
                    {
                        SelectedProfile = profileF5;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
                break;
                
                case Keys.Enter: // Connect
                {
                    ConnectionProfile profileEnter;
                    if (selection >= 1 && selection <= 10 && _profiles.TryGetValue(selection, out profileEnter))
                    {
                        SelectedProfile = profileEnter;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
                break;
            }
        }
        
        private void EditProfile(int slot)
        {
            // Check if profile exists
            if (!_profiles.TryGetValue(slot, out ConnectionProfile profile))
            {
                profile = new ConnectionProfile
                {
                    Name = $"Connection {slot}",
                    Port = 22
                };
            }
            
            // Pass the theme manager to the edit form to ensure consistent theming
            using (ConnectionEditForm editForm = new ConnectionEditForm(profile, _themeManager))
            {
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    _directoryService.SaveProfile(slot, editForm.Profile);
                    LoadProfiles();
                }
            }
        }
        
        private void CreateNewProfile(int slot)
        {
            // Create a new profile
            ConnectionProfile profile = new ConnectionProfile
            {
                Name = $"Connection {slot}",
                Port = 22
            };
            
            // Pass the theme manager to the edit form to ensure consistent theming
            using (ConnectionEditForm editForm = new ConnectionEditForm(profile, _themeManager))
            {
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    _directoryService.SaveProfile(slot, editForm.Profile);
                    LoadProfiles();
                }
            }
        }
        
        private void DeleteProfile(int slot)
        {
            if (_profiles.ContainsKey(slot))
            {
                DialogResult result = MessageBox.Show(
                    $"Are you sure you want to delete connection {slot}?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    _directoryService.DeleteProfile(slot);
                    LoadProfiles();
                }
            }
        }
        
        // Make sure to unsubscribe from events on close
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_themeManager != null)
            {
                _themeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            }
            
            base.OnFormClosed(e);
        }
    }

}