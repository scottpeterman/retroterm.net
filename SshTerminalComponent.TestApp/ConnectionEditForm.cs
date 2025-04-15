using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SshTerminalComponent;

namespace SshTerminalComponent.TestApp
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
        
        public ConnectionProfile Profile { get; private set; }
        
        public ConnectionEditForm(ConnectionProfile profile, ThemeManager themeManager)
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
            
            // Title bar
            Label titleLabel = new Label
            {
                Text = " EDIT CONNECTION ",
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Consolas", 16, FontStyle.Bold),
                ForeColor = textColor,
                BackColor = bgColor
            };
            
            // Main panel with border
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = bgColor,
                Margin = new Padding(0, 0, 0, 60)
            };
            
            // Draw DOS-style double-line border with theme colors
            this.Paint += (s, e) => {
                // Get current theme color for border - this handles runtime theme changes
                Color currentBorderColor = _themeManager?.CurrentTheme != null 
                    ? Theme.HexToColor(_themeManager.CurrentTheme.UI.Border) 
                    : borderColor;
                
                // Draw outer border for the entire form
                e.Graphics.DrawRectangle(new Pen(currentBorderColor, 1), 1, 1, 
                    this.Width - 3, this.Height - 3);
                
                // Draw inner border (double-line effect)
                e.Graphics.DrawRectangle(new Pen(currentBorderColor, 1), 4, 4, 
                    this.Width - 9, this.Height - 9);
            };
            
            // Fields panel
            Panel fieldsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 300,
                BackColor = bgColor
            };
            
            // Create each field with absolute positioning and FIXED width for textboxes
            // Name field
            Label nameLabel = new Label
            {
                Text = "Name:",
                ForeColor = textColor,
                BackColor = bgColor,
                Font = new Font("Consolas", 13, FontStyle.Regular),
                AutoSize = false,
                Size = new Size(130, 30),
                Location = new Point(30, 20),
                TextAlign = ContentAlignment.MiddleRight
            };
            
            nameTextBox = new TextBox
            {
                Text = Profile.Name,
                BackColor = bgColor,
                ForeColor = inputTextColor,
                Font = new Font("Consolas", 13, FontStyle.Regular),
                Size = new Size(390, 30),  // Fixed width
                Location = new Point(210, 20),  // Shifted right
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // Host field
            Label hostLabel = new Label
            {
                Text = "Host:",
                ForeColor = textColor,
                BackColor = bgColor,
                Font = new Font("Consolas", 13, FontStyle.Regular),
                AutoSize = false,
                Size = new Size(130, 30),
                Location = new Point(30, 60),
                TextAlign = ContentAlignment.MiddleRight
            };
            
            hostTextBox = new TextBox
            {
                Text = Profile.Host,
                BackColor = bgColor,
                ForeColor = inputTextColor,
                Font = new Font("Consolas", 13, FontStyle.Regular),
                Size = new Size(390, 30),  // Fixed width
                Location = new Point(210, 60),  // Shifted right
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // Port field
            Label portLabel = new Label
            {
                Text = "Port:",
                ForeColor = textColor,
                BackColor = bgColor,
                Font = new Font("Consolas", 13, FontStyle.Regular),
                AutoSize = false,
                Size = new Size(130, 30),
                Location = new Point(30, 100),
                TextAlign = ContentAlignment.MiddleRight
            };
            
            portTextBox = new TextBox
            {
                Text = Profile.Port.ToString(),
                BackColor = bgColor,
                ForeColor = inputTextColor,
                Font = new Font("Consolas", 13, FontStyle.Regular),
                Size = new Size(390, 30),  // Fixed width
                Location = new Point(210, 100),  // Shifted right
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // Username field
            Label usernameLabel = new Label
            {
                Text = "Username:",
                ForeColor = textColor,
                BackColor = bgColor,
                Font = new Font("Consolas", 13, FontStyle.Regular),
                AutoSize = false,
                Size = new Size(170, 30),  // Even wider for username
                Location = new Point(30, 130),
                TextAlign = ContentAlignment.MiddleRight
            };
            
            usernameTextBox = new TextBox
            {
                Text = Profile.Username,
                BackColor = bgColor,
                ForeColor = inputTextColor,
                Font = new Font("Consolas", 13, FontStyle.Regular),
                Size = new Size(390, 30),  // Fixed width
                Location = new Point(210, 130),  // Shifted right
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // Password field
            Label passwordLabel = new Label
            {
                Text = "Password:",
                ForeColor = textColor,
                BackColor = bgColor,
                Font = new Font("Consolas", 13, FontStyle.Regular),
                AutoSize = false,
                Size = new Size(170, 30),  // Even wider for password
                Location = new Point(30, 180),
                TextAlign = ContentAlignment.MiddleRight
            };
            
            passwordTextBox = new TextBox
            {
                Text = Profile.EncryptedPassword,
                PasswordChar = '*',
                BackColor = bgColor,
                ForeColor = inputTextColor,
                Font = new Font("Consolas", 13, FontStyle.Regular),
                Size = new Size(390, 30),  // Fixed width
                Location = new Point(210, 180),  // Shifted right
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // Button panel
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = bgColor
            };
            
            saveButton = new Button
            {
                Text = "Save",
                BackColor = buttonBgColor,
                ForeColor = buttonTextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 13, FontStyle.Regular),
                Size = new Size(120, 40),
                Location = new Point(205, 15)
            };
            
            cancelButton = new Button
            {
                Text = "Cancel",
                BackColor = buttonBgColor,
                ForeColor = buttonTextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 13, FontStyle.Regular),
                Size = new Size(130, 40),
                Location = new Point(375, 15)
            };
            
            saveButton.FlatAppearance.BorderColor = buttonBorderColor;
            cancelButton.FlatAppearance.BorderColor = buttonBorderColor;
            
            // Add fields to fieldsPanel
            fieldsPanel.Controls.Add(nameLabel);
            fieldsPanel.Controls.Add(nameTextBox);
            fieldsPanel.Controls.Add(hostLabel);
            fieldsPanel.Controls.Add(hostTextBox);
            fieldsPanel.Controls.Add(portLabel);
            fieldsPanel.Controls.Add(portTextBox);
            fieldsPanel.Controls.Add(usernameLabel);
            fieldsPanel.Controls.Add(usernameTextBox);
            fieldsPanel.Controls.Add(passwordLabel);
            fieldsPanel.Controls.Add(passwordTextBox);
            
            // Add buttons to buttonPanel
            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(cancelButton);
            
            // Add panels to form
            mainPanel.Controls.Add(fieldsPanel);
            this.Controls.Add(buttonPanel);
            this.Controls.Add(mainPanel);
            this.Controls.Add(titleLabel);
            
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
            Profile.LastConnected = DateTime.Now;
            
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