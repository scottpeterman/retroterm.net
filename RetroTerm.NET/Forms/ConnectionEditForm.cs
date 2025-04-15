using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SshTerminalComponent;
using RetroTerm.NET.Models;
using RetroTerm.NET.Services;
using RetroTermProfile = RetroTerm.NET.Models.ConnectionProfile;
using RetroTerm.NET.Forms;


namespace RetroTerm.NET.Forms
{
    // Add a simple edit form for connections that uses theming
    public class ConnectionEditForm : Form
    {
        private TextBox nameTextBox;
        private TextBox hostTextBox;
        private TextBox portTextBox;
        private TextBox usernameTextBox;
        private TextBox passwordTextBox;
        private Button saveButton;
        private Button cancelButton;
        
        // Reference to the theme manager
        private ThemeManager _themeManager;
        
        // public ConnectionProfile Profile { get; private set; }
        public RetroTermProfile Profile { get; private set; }

        public ConnectionEditForm(RetroTermProfile profile, ThemeManager themeManager)
        {
            Profile = profile;
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
            
            // Subscribe to theme change events
            _themeManager.ThemeChanged += ThemeManager_ThemeChanged;
            
            InitializeComponent();
            
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
            
            // Update controls with current theme colors
            nameTextBox.BackColor = Theme.HexToColor(theme.UI.Background);
            nameTextBox.ForeColor = Theme.HexToColor(theme.UI.InputText);
            
            hostTextBox.BackColor = Theme.HexToColor(theme.UI.Background);
            hostTextBox.ForeColor = Theme.HexToColor(theme.UI.InputText);
            
            portTextBox.BackColor = Theme.HexToColor(theme.UI.Background);
            portTextBox.ForeColor = Theme.HexToColor(theme.UI.InputText);
            
            usernameTextBox.BackColor = Theme.HexToColor(theme.UI.Background);
            usernameTextBox.ForeColor = Theme.HexToColor(theme.UI.InputText);
            
            passwordTextBox.BackColor = Theme.HexToColor(theme.UI.Background);
            passwordTextBox.ForeColor = Theme.HexToColor(theme.UI.InputText);
            
            saveButton.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
            saveButton.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
            saveButton.FlatAppearance.BorderColor = Theme.HexToColor(theme.UI.ButtonBorder);
            
            cancelButton.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
            cancelButton.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
            cancelButton.FlatAppearance.BorderColor = Theme.HexToColor(theme.UI.ButtonBorder);
            
            // Update all controls recursively (for labels and panels)
            UpdateControlColors(this, theme);
            
            // Force a repaint to update the borders
            this.Invalidate();
        }
        
        // Helper to recursively update control colors
        private void UpdateControlColors(Control parent, Theme theme)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Label label)
                {
                    label.BackColor = Theme.HexToColor(theme.UI.Background);
                    label.ForeColor = Theme.HexToColor(theme.UI.Text);
                }
                else if (control is Panel panel)
                {
                    panel.BackColor = Theme.HexToColor(theme.UI.Background);
                    panel.ForeColor = Theme.HexToColor(theme.UI.Text);
                }
                
                // Update any child controls
                if (control.Controls.Count > 0)
                {
                    UpdateControlColors(control, theme);
                }
            }
        }
        
        
        // InitializeComponent method improvements
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

    Color buttonBorderColor = _themeManager?.CurrentTheme != null 
        ? Theme.HexToColor(_themeManager.CurrentTheme.UI.ButtonBorder) 
        : Color.FromArgb(0, 0, 0); // Default Black
            
    Color borderColor = _themeManager?.CurrentTheme != null 
        ? Theme.HexToColor(_themeManager.CurrentTheme.UI.Border) 
        : Color.FromArgb(255, 255, 255); // Default White

    // Form setup
    this.FormBorderStyle = FormBorderStyle.None;
    this.BackColor = bgColor;
    this.ForeColor = textColor;
    this.StartPosition = FormStartPosition.CenterParent;
    this.Size = new Size(700, 450);
    this.KeyPreview = true;
    
    // Create a TableLayoutPanel for consistent layout
    TableLayoutPanel mainLayout = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        RowCount = 3,
        ColumnCount = 1,
        BackColor = bgColor
    };
    
    // Configure row styles
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Title bar
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // Buttons
    
    // Title bar
    Panel titleBar = new Panel
    {
        Dock = DockStyle.Fill,
        BackColor = bgColor,
        Margin = new Padding(0)
    };
    
    Label titleLabel = new Label
    {
        Text = " EDIT CONNECTION ",
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Consolas", 16, FontStyle.Bold),
        ForeColor = textColor,
        BackColor = bgColor,
        Dock = DockStyle.Fill
    };
    
    titleBar.Controls.Add(titleLabel);
    
    // Content panel with border
    Panel contentPanel = new Panel
    {
        Dock = DockStyle.Fill,
        BackColor = bgColor,
        Padding = new Padding(20)
    };
    
    // Draw DOS-style double-line border with theme colors
    contentPanel.Paint += (s, e) => {
        // Get current theme color for border - this handles runtime theme changes
        Color currentBorderColor = _themeManager?.CurrentTheme != null 
            ? Theme.HexToColor(_themeManager.CurrentTheme.UI.Border) 
            : borderColor;
        
        // Draw outer border
        e.Graphics.DrawRectangle(new Pen(currentBorderColor, 1), 0, 0, 
            contentPanel.Width - 1, contentPanel.Height - 1);
        
        // Draw inner border (double-line effect)
        e.Graphics.DrawRectangle(new Pen(currentBorderColor, 1), 3, 3, 
            contentPanel.Width - 7, contentPanel.Height - 7);
    };
    
    // Form fields panel using TableLayoutPanel for proper alignment
    TableLayoutPanel fieldsTable = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        RowCount = 5, // 5 fields: Name, Host, Port, Username, Password
        ColumnCount = 2, // Label | TextBox
        BackColor = bgColor,
        Padding = new Padding(15, 25, 15, 15) // More padding at top to prevent overlap with border
    };
    
    // Configure column styles
    fieldsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));  // Labels
    fieldsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));  // TextBoxes
    
    // Set consistent row heights
    for (int i = 0; i < 5; i++)
    {
        fieldsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
    }
    
    // Create field pairs with consistent styling
    // Name field
    AddFormField(fieldsTable, 0, "Name:", Profile.Name, out nameTextBox,
        bgColor, textColor, inputTextColor);
    
    // Host field
    AddFormField(fieldsTable, 1, "Host:", Profile.Host, out hostTextBox,
        bgColor, textColor, inputTextColor);
    
    // Port field
    AddFormField(fieldsTable, 2, "Port:", Profile.Port.ToString(), out portTextBox,
        bgColor, textColor, inputTextColor);
    
    // Username field
    AddFormField(fieldsTable, 3, "Username:", Profile.Username, out usernameTextBox,
        bgColor, textColor, inputTextColor);
    
    // Password field
    AddFormField(fieldsTable, 4, "Password:", Profile.EncryptedPassword, out passwordTextBox,
        bgColor, textColor, inputTextColor, isPassword: true);
    
    contentPanel.Controls.Add(fieldsTable);
    
    // Button panel
    Panel buttonPanel = new Panel
    {
        Dock = DockStyle.Fill,
        BackColor = bgColor
    };
    
    // Center the buttons horizontally
    TableLayoutPanel buttonLayout = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        RowCount = 1,
        ColumnCount = 4,
        BackColor = bgColor
    };
    
    // Set column styles for button spacing
    buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); // Left space
    buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));   // Save button
    buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));   // Cancel button
    buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); // Right space
    
    saveButton = new Button
    {
        Text = "Save",
        BackColor = buttonBgColor,
        ForeColor = buttonTextColor,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Consolas", 10, FontStyle.Regular),
        Size = new Size(120, 40),
        Margin = new Padding(5)
    };
    
    cancelButton = new Button
    {
        Text = "Cancel",
        BackColor = buttonBgColor,
        ForeColor = buttonTextColor,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Consolas", 10, FontStyle.Regular),
        Size = new Size(120, 40),
        Margin = new Padding(5)
    };
    
    saveButton.FlatAppearance.BorderColor = buttonBorderColor;
    cancelButton.FlatAppearance.BorderColor = buttonBorderColor;
    
    // Add buttons to layout
    buttonLayout.Controls.Add(new Panel(), 0, 0); // Spacer
    buttonLayout.Controls.Add(saveButton, 1, 0);
    buttonLayout.Controls.Add(cancelButton, 2, 0);
    buttonLayout.Controls.Add(new Panel(), 3, 0); // Spacer
    
    buttonPanel.Controls.Add(buttonLayout);
    
    // Add panels to main layout
    mainLayout.Controls.Add(titleBar, 0, 0);
    mainLayout.Controls.Add(contentPanel, 0, 1);
    mainLayout.Controls.Add(buttonPanel, 0, 2);
    
    // Add main layout to form
    this.Controls.Add(mainLayout);
    
    // Add event handlers
    saveButton.Click += SaveButton_Click;
    cancelButton.Click += CancelButton_Click;
    this.KeyDown += (s, e) => {
        if (e.KeyCode == Keys.Escape)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        else if (e.KeyCode == Keys.Enter)
        {
            SaveProfile();
        }
    };
    
    // Set focus to name textbox and force repaint
    this.Load += (s, e) => {
        this.Refresh();
        nameTextBox.Focus();
    };
}



private void AddFormField(TableLayoutPanel table, int row, string labelText, string value, 
    out TextBox textBox, Color bgColor, Color labelColor, Color textColor, bool isPassword = false)
{
    Label label = new Label
    {
        Text = labelText,
        TextAlign = ContentAlignment.MiddleRight,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 13, FontStyle.Regular),
        BackColor = bgColor,
        ForeColor = labelColor,
        Margin = new Padding(5)
    };
    
    textBox = new TextBox
    {
        Text = value,
        BackColor = bgColor,
        ForeColor = textColor,
        Font = new Font("Consolas", 13, FontStyle.Regular),
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(5)
    };
    
    if (isPassword)
    {
        textBox.PasswordChar = '*';
    }
    
    table.Controls.Add(label, 0, row);
    table.Controls.Add(textBox, 1, row);
}


        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveProfile();
        }
        
        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        
        private void SaveProfile()
        {
            if (string.IsNullOrWhiteSpace(hostTextBox.Text))
            {
                MessageBox.Show("Host is required", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(usernameTextBox.Text))
            {
                MessageBox.Show("Username is required", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            int port;
            if (!int.TryParse(portTextBox.Text, out port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Port must be a number between 1 and 65535", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Update profile
            Profile.Name = string.IsNullOrWhiteSpace(nameTextBox.Text) ? 
                           $"{usernameTextBox.Text}@{hostTextBox.Text}" : nameTextBox.Text;
            Profile.Host = hostTextBox.Text;
            Profile.Port = port;
            Profile.Username = usernameTextBox.Text;
            Profile.EncryptedPassword = passwordTextBox.Text;
            // Profile.LastConnected = DateTime.Now;
            Profile.LastAccessedDate = DateTime.Now;

            this.DialogResult = DialogResult.OK;
            this.Close();
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