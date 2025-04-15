using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using RetroTerm.NET.Models;
using RetroTerm.NET.Services;
using SshTerminalComponent;

namespace RetroTerm.NET.Forms
{
    /// <summary>
    /// Modal dialog for SSH connection details
    /// </summary>
    public partial class ConnectionDialog : Form
    {
        private ThemeManager themeManager;
        private TextBox hostTextBox;
        private TextBox portTextBox;
        private TextBox usernameTextBox;
        private TextBox passwordTextBox;
        
        public string Host { get; private set; }
        public int Port { get; private set; } = 22;
        public string Username { get; private set; }
        public string Password { get; private set; }
        
        // Keep these properties but with simplified implementation
        public bool SaveConnection { get; private set; } = false;
        public string ConnectionName { get; private set; }
        public bool ConnectImmediately { get; private set; } = true;
        
        public ConnectionDialog(ThemeManager themeManager = null)
        {
            this.themeManager = themeManager;
            InitializeComponent();
            ApplyTheme();
        }
        
        // Load with existing profile
        public ConnectionDialog(Models.ConnectionProfile profile, ThemeManager themeManager = null) 
            : this(themeManager)
        {
            if (profile != null)
            {
                hostTextBox.Text = profile.Host;
                portTextBox.Text = profile.Port.ToString();
                usernameTextBox.Text = profile.Username;
                passwordTextBox.Text = profile.EncryptedPassword;
                ConnectionName = profile.Name;
            }
        }

        private void InitializeComponent()
        {
            // Form properties
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(520, 310); // Reduced height since we're removing save options

            // Get theme colors - default to Borland colors if no theme manager
            Color bgColor = Color.FromArgb(0, 0, 170); // Borland Blue
            Color textColor = Color.White;
            Color buttonColor = Color.FromArgb(0, 170, 170); // Borland Cyan
            Color buttonTextColor = Color.Black;
            Color inputTextColor = Color.Yellow;
            
            if (themeManager?.CurrentTheme != null)
            {
                bgColor = Theme.HexToColor(themeManager.CurrentTheme.UI.Background);
                textColor = Theme.HexToColor(themeManager.CurrentTheme.UI.Text);
                buttonColor = Theme.HexToColor(themeManager.CurrentTheme.UI.ButtonBackground);
                buttonTextColor = Theme.HexToColor(themeManager.CurrentTheme.UI.ButtonText);
                inputTextColor = Theme.HexToColor(themeManager.CurrentTheme.UI.InputText);
            }
            
            this.BackColor = bgColor;
            this.ForeColor = textColor;
            
            // Create the main layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0, 0, 0, 10),
                Margin = new Padding(0)
            };
            
            // Set row styles
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Title bar height
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 120F)); // Form area
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));

            // Title bar
            Panel titleBar = new Panel
            {
                Height = 40,
                Dock = DockStyle.Fill,
                BackColor = buttonColor
            };
            
            Label titleLabel = new Label
            {
                Text = "SSH CONNECTION",
                Font = new Font("Consolas", 13, FontStyle.Bold),
                ForeColor = buttonTextColor,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            
            titleBar.Controls.Add(titleLabel);
            
            // Form panel for inputs
            TableLayoutPanel formPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4, // Reduced to 4 rows after removing save connection 
                Padding = new Padding(20, 10, 20, 10)
            };
            
            // Set column styles
            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            
            // Set row styles for the form panel to give appropriate space to each row
            formPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F)); // Host
            formPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F)); // Port
            formPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F)); // Username
            formPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F)); // Password
            
            // Host field
            Label hostLabel = new Label
            {
                Text = "Host:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = textColor,
                Font = new Font("Consolas", 10)
            };
            
            hostTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = bgColor,
                ForeColor = inputTextColor
            };
            
            formPanel.Controls.Add(hostLabel, 0, 0);
            formPanel.Controls.Add(hostTextBox, 1, 0);
            
            // Port field
            Label portLabel = new Label
            {
                Text = "Port:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = textColor,
                Font = new Font("Consolas", 10)
            };
            
            portTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = "22",
                BackColor = bgColor,
                ForeColor = inputTextColor
            };
            
            formPanel.Controls.Add(portLabel, 0, 1);
            formPanel.Controls.Add(portTextBox, 1, 1);
            
            // Username field
            Label usernameLabel = new Label
            {
                Text = "Username:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = textColor,
                Font = new Font("Consolas", 10)
            };
            
            usernameTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = bgColor,
                ForeColor = inputTextColor
            };
            
            formPanel.Controls.Add(usernameLabel, 0, 2);
            formPanel.Controls.Add(usernameTextBox, 1, 2);
            
            // Password field
            Label passwordLabel = new Label
            {
                Text = "Password:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = textColor,
                Font = new Font("Consolas", 10)
            };
            
            passwordTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                PasswordChar = '*',
                BackColor = bgColor,
                ForeColor = inputTextColor
            };
            
            formPanel.Controls.Add(passwordLabel, 0, 3);
            formPanel.Controls.Add(passwordTextBox, 1, 3);
            
            // Buttons panel
            TableLayoutPanel buttonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(10, 5, 10, 5)
            };
            
            // Set column styles for balanced button layout
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // Left spacer
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Connect button
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Cancel button
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // Right spacer
            
            // Connect button
            Button connectButton = new Button
            {
                Text = "CONNECT",
                BackColor = buttonColor,
                ForeColor = buttonTextColor,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(5, 15, 15, 15),
                Font = new Font("Consolas", 9, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 5)
            };
            
            connectButton.Click += ConnectButton_Click;
            
            // Cancel button
            Button cancelButton = new Button
            {
                Text = "CANCEL",
                BackColor = buttonColor,
                ForeColor = buttonTextColor,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(15, 15, 5, 15),
                Font = new Font("Consolas", 9, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 5)
            };
            
            cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            
            // Add buttons to panel with better spacing
            buttonsPanel.Controls.Add(new Panel(), 0, 0); // Left spacer
            buttonsPanel.Controls.Add(connectButton, 2, 0);
            buttonsPanel.Controls.Add(cancelButton, 3, 0);
            
            // Add all panels to main layout
            mainLayout.Controls.Add(titleBar, 0, 0);
            mainLayout.Controls.Add(formPanel, 0, 1);
            mainLayout.Controls.Add(buttonsPanel, 0, 2);
            
            this.Controls.Add(mainLayout);
            
            // Add border
            this.Paint += (s, e) => {
                // Draw double border around form
                Color borderColor = textColor; // Use text color for border
                Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                e.Graphics.DrawRectangle(new Pen(borderColor, 2), rect);
                
                // Inner border
                Rectangle innerRect = new Rectangle(4, 4, this.Width - 9, this.Height - 9);
                e.Graphics.DrawRectangle(new Pen(borderColor), innerRect);
            };
            
            // Make title bar draggable
            titleBar.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    const int WM_NCLBUTTONDOWN = 0xA1;
                    const int HT_CAPTION = 0x2;
                    
                    [DllImport("user32.dll")]
                    static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
                    [DllImport("user32.dll")]
                    static extern bool ReleaseCapture();
                    
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                // Set connection parameters
                Host = hostTextBox.Text.Trim();
                
                if (int.TryParse(portTextBox.Text, out int portValue))
                    Port = portValue;
                
                Username = usernameTextBox.Text.Trim();
                Password = passwordTextBox.Text;
                SaveConnection = false; // Always set to false since we've removed save functionality
                ConnectionName = $"{Username}@{Host}"; // Set a default name based on username and host
                ConnectImmediately = true;
                
                this.DialogResult = DialogResult.OK;
                this.Close(); // Explicitly close the dialog
            }
        }   

        private bool ValidateInput()
        {
            // Validate host
            if (string.IsNullOrWhiteSpace(hostTextBox.Text))
            {
                MessageBox.Show("Please enter a host name or IP address.", 
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                hostTextBox.Focus();
                return false;
            }
            
            // Validate port
            if (!int.TryParse(portTextBox.Text, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Please enter a valid port number (1-65535).", 
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                portTextBox.Focus();
                return false;
            }
            
            // Validate username
            if (string.IsNullOrWhiteSpace(usernameTextBox.Text))
            {
                MessageBox.Show("Please enter a username.", 
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                usernameTextBox.Focus();
                return false;
            }
            
            return true;
        }

        private void ApplyTheme()
        {
            // Apply theme colors if theme manager is available
            if (themeManager?.CurrentTheme == null) return;
            
            var theme = themeManager.CurrentTheme;
            
            // Apply theme colors to form controls
            this.BackColor = Theme.HexToColor(theme.UI.Background);
            this.ForeColor = Theme.HexToColor(theme.UI.Text);
            
            foreach (Control control in this.Controls)
            {
                ApplyThemeToControl(control, theme);
            }
        }
        
       
       
    
    private void ApplyThemeToControl(Control control, Theme theme)
{
    // Apply theme based on control type
    if (control is TextBox textBox)
    {
        textBox.BackColor = Theme.HexToColor(theme.UI.Background);
        textBox.ForeColor = Theme.HexToColor(theme.UI.InputText);
    }
    else if (control is Button button)
    {
        button.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
        button.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
        button.FlatAppearance.BorderColor = Theme.HexToColor(theme.UI.Border);
    }
    else if (control is Label label)
    {
        label.BackColor = Theme.HexToColor(theme.UI.Background);
        label.ForeColor = Theme.HexToColor(theme.UI.Text);
    }
    else if (control is Panel panelControl) // Changed variable name from 'panel' to 'panelControl'
    {
        panelControl.BackColor = Theme.HexToColor(theme.UI.Background);
        panelControl.ForeColor = Theme.HexToColor(theme.UI.Text);
    }
    
    // Apply theme to child controls recursively
    foreach (Control child in control.Controls)
    {
        ApplyThemeToControl(child, theme);
    }
}
    
    
    }
}