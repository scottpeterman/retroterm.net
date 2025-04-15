using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RetroTerm.NET.Services;
using RetroTerm.NET.Models;

namespace RetroTerm.NET
{
    public class SessionPropertyDialog : Form
    {
        private SessionData _sessionData;
        private Dictionary<string, Control> _fields = new Dictionary<string, Control>();
        private ThemeManager _themeManager;
        private bool _isNewSession;
        
        public SessionPropertyDialog(SessionData sessionData, ThemeManager themeManager, Font dosFont, bool isNewSession = false)
        {
            _sessionData = sessionData ?? new SessionData();
            _themeManager = themeManager;
            _isNewSession = isNewSession;
            
            InitializeComponent(dosFont);
            LoadData();
            
            // Set form title based on mode
            this.Text = _isNewSession ? "Add New Session" : "Edit Session Properties";
            
            // Apply theme
            if (_themeManager?.CurrentTheme != null)
            {
                ApplyTheme();
            }
        }
        
        private void ApplyTheme()
        {
            var theme = _themeManager.CurrentTheme;
            
            // Apply theme to form
            this.BackColor = Theme.HexToColor(theme.UI.Background);
            this.ForeColor = Theme.HexToColor(theme.UI.Text);
            
            // Apply theme to all controls
            foreach (Control control in this.Controls)
            {
                ApplyThemeToControl(control, theme);
            }
        }
        
        private void ApplyThemeToControl(Control control, Models.Theme theme)
        {
            if (control is Panel panel)
            {
                panel.BackColor = Theme.HexToColor(theme.UI.Background);
                
                // Apply theme to panel controls recursively
                foreach (Control childControl in panel.Controls)
                {
                    ApplyThemeToControl(childControl, theme);
                }
            }
            else if (control is Label label)
            {
                if (label.Parent is Panel titlePanel && titlePanel.Name == "titleBar")
                {
                    label.ForeColor = Theme.HexToColor(theme.UI.MenuText);
                }
                else
                {
                    label.ForeColor = Theme.HexToColor(theme.UI.Text);
                }
            }
            else if (control is TextBox textBox)
            {
                textBox.BackColor = Theme.HexToColor(theme.UI.Background);
                textBox.ForeColor = Theme.HexToColor(theme.UI.InputText);
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.BackColor = Theme.HexToColor(theme.UI.Background);
                comboBox.ForeColor = Theme.HexToColor(theme.UI.InputText);
            }
            else if (control is Button button)
            {
                button.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
                button.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
                button.FlatAppearance.BorderColor = Theme.HexToColor(theme.UI.Border);
            }
            else if (control is NumericUpDown numericUpDown)
            {
                numericUpDown.BackColor = Theme.HexToColor(theme.UI.Background);
                numericUpDown.ForeColor = Theme.HexToColor(theme.UI.InputText);
            }
            else if (control is TableLayoutPanel tablePanel)
            {
                tablePanel.BackColor = Theme.HexToColor(theme.UI.Background);
                
                // Apply to controls in the table layout recursively
                foreach (Control childControl in tablePanel.Controls)
                {
                    ApplyThemeToControl(childControl, theme);
                }
            }
        }
        
        private void InitializeComponent(Font dosFont)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new System.Drawing.Size(500, 600);
            this.Font = dosFont;
            
            // Title bar
            Panel titleBar = new Panel
            {
                Name = "titleBar",
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.Silver
            };
            
            Label titleLabel = new Label
            {
                Text = _isNewSession ? "Add New Session" : "Edit Session Properties",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 0, 0)
            };
            
            Button closeButton = new Button
            {
                Text = "[X]",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(30, 28),
                Location = new Point(this.Width - 32, 1),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TabStop = false,
                Font = new Font(dosFont.FontFamily, 8, FontStyle.Bold)
            };
            
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => this.Close();
            
            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(closeButton);
            
            // Make title bar draggable
            titleBar.MouseDown += TitleBar_MouseDown;
            
            // Main content panel
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BorderStyle = BorderStyle.FixedSingle,
                Height = this.Height - 30
            };
            
            // Create fields layout
            TableLayoutPanel fieldsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(0, 0, 0, 60), // Bottom padding for buttons
                AutoSize = true
            };
            
            // Configure columns
            fieldsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            fieldsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            
            // Add form fields
            int row = 0;
            
            // Display Name
            AddFormField(fieldsTable, row++, "Display Name:", new TextBox { 
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            });
            
            // Host
            AddFormField(fieldsTable, row++, "Host:", new TextBox { 
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            });
            
            // Port
            NumericUpDown portUpDown = new NumericUpDown {
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 65535,
                Value = 22,
                BorderStyle = BorderStyle.FixedSingle
            };
            AddFormField(fieldsTable, row++, "Port:", portUpDown);
            
            // Device Type
            ComboBox deviceTypeCombo = new ComboBox {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            deviceTypeCombo.Items.AddRange(new object[] { "Linux", "cisco_ios", "hp_procurve" });
            AddFormField(fieldsTable, row++, "Device Type:", deviceTypeCombo);
            
            // Model
            AddFormField(fieldsTable, row++, "Model:", new TextBox { 
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            });
            
            // Serial Number
            AddFormField(fieldsTable, row++, "Serial Number:", new TextBox { 
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            });
            
            // Software Version
            AddFormField(fieldsTable, row++, "Software Version:", new TextBox { 
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            });
            
            // Vendor
            AddFormField(fieldsTable, row++, "Vendor:", new TextBox { 
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            });
            
            // Username
AddFormField(fieldsTable, row++, "Username:", new TextBox { 
    Dock = DockStyle.Fill,
    BorderStyle = BorderStyle.FixedSingle
});

// Password
TextBox passwordTextBox = new TextBox { 
    Dock = DockStyle.Fill,
    BorderStyle = BorderStyle.FixedSingle,
    PasswordChar = '*' // Hide password characters for security
};
AddFormField(fieldsTable, row++, "Password:", passwordTextBox);



            // Create invisible field for credsid - not shown to user but preserved
            TextBox credsidField = new TextBox { Visible = false };
            _fields["CredentialsID"] = credsidField;
            contentPanel.Controls.Add(credsidField);
            
            // Button panel
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(0, 10, 0, 0)
            };
            
            // Save button
            Button saveButton = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 40),
                Location = new Point(this.Width / 2 - 130, 10)
            };
            
            // Cancel button
            Button cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 40),
                Location = new Point(this.Width / 2 + 10, 10)
            };
            
            // Add buttons to panel
            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(cancelButton);
            
            // Add panels to form
            contentPanel.Controls.Add(fieldsTable);
            contentPanel.Controls.Add(buttonPanel);
            
            this.Controls.Add(contentPanel);
            this.Controls.Add(titleBar);
            
            // Set accept/cancel buttons
            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
            
            // Draw border
            this.Paint += (s, e) => {
                Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                using (Pen pen = new Pen(_themeManager?.CurrentTheme != null ? 
                    Theme.HexToColor(_themeManager.CurrentTheme.UI.Border) : Color.White, 2))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };
            
            // Adjust button positions on resize
            this.Resize += (s, e) => {
                saveButton.Location = new Point(this.Width / 2 - 130, 10);
                cancelButton.Location = new Point(this.Width / 2 + 10, 10);
            };
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
        
        private void AddFormField(TableLayoutPanel layout, int row, string label, Control control)
        {
            Label fieldLabel = new Label
            {
                Text = label,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 8, 8)
            };
            
            // Configure control
            control.Margin = new Padding(0, 8, 0, 8);
            
            // Add to layout
            layout.Controls.Add(fieldLabel, 0, row);
            layout.Controls.Add(control, 1, row);
            
            // Store in fields dictionary
            string fieldName = label.Replace(":", "").Replace(" ", "");
            _fields[fieldName] = control;
        }
        
        private void LoadData()
        {
            // Set field values from session data
            SetFieldValue("DisplayName", _sessionData.DisplayName);
            SetFieldValue("Host", _sessionData.Host);
            SetFieldValue("Port", _sessionData.Port.ToString());
            SetFieldValue("DeviceType", _sessionData.DeviceType);
            SetFieldValue("Model", _sessionData.Model);
            SetFieldValue("SerialNumber", _sessionData.SerialNumber);
            SetFieldValue("SoftwareVersion", _sessionData.SoftwareVersion);
            SetFieldValue("Vendor", _sessionData.Vendor);
            SetFieldValue("Username", _sessionData.Username);
            SetFieldValue("Password", _sessionData.Password);
            SetFieldValue("CredentialsID", _sessionData.CredsId);
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
        DeviceType = GetFieldValue("DeviceType"),
        Model = GetFieldValue("Model"),
        SerialNumber = GetFieldValue("SerialNumber"),
        SoftwareVersion = GetFieldValue("SoftwareVersion"),
        Vendor = GetFieldValue("Vendor"),
        Username = GetFieldValue("Username"),
        Password = GetFieldValue("Password"), // We store password unencrypted in the object
        CredsId = GetFieldValue("CredentialsID")
    };
    
    // Handle Port as int
    if (int.TryParse(GetFieldValue("Port"), out int portValue))
        data.Port = portValue;
    else
        data.Port = 22; // Default port
    
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