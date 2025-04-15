using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace SshTerminalComponent.TestApp
{
    public class AboutForm : Form
    {
        // DOS/Borland color palette (matching your MainForm)
        private static readonly Color BorlandBlue = Color.FromArgb(0, 0, 170);
        private static readonly Color BorlandCyan = Color.FromArgb(0, 170, 170);
        private static readonly Color BorlandWhite = Color.FromArgb(255, 255, 255);
        private static readonly Color BorlandYellow = Color.FromArgb(255, 255, 0);
        private static readonly Color BorlandBlack = Color.FromArgb(0, 0, 0);
        
        public AboutForm()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            // Form setup
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = BorlandBlue;
            this.ForeColor = BorlandWhite;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(650, 450);
            this.KeyPreview = true;
            
            // Create title bar
            Label titleLabel = new Label
            {
                Text = " ABOUT ",
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(FontFamily.GenericMonospace, 14, FontStyle.Bold),
                ForeColor = BorlandWhite,
                BackColor = BorlandBlue
            };
            
            // Main panel with border
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = BorlandBlue
            };
            
            // Draw DOS-style double-line border
            mainPanel.Paint += (s, e) => {
                // Draw outer border
                e.Graphics.DrawRectangle(new Pen(BorlandWhite), 0, 0, 
                    mainPanel.Width - 1, mainPanel.Height - 1);
                
                // Draw inner border (double-line effect)
                e.Graphics.DrawRectangle(new Pen(BorlandWhite), 2, 2, 
                    mainPanel.Width - 5, mainPanel.Height - 5);
            };
            
            // Content panel
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = BorlandBlue
            };
            
            // Application name with version
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            
            Label appNameLabel = new Label
            {
                Text = "RetroTerm.net",
                Font = new Font("Consolas", 16, FontStyle.Bold),
                ForeColor = BorlandCyan,
                BackColor = BorlandBlue,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            
            Label versionLabel = new Label
            {
                // Text = $"\r\n\r\nVersion {version}",
                Text = $"\r\n\r\nVersion 1.0.0",
                Font = new Font("Consolas", 12, FontStyle.Regular),
                ForeColor = BorlandWhite,
                BackColor = BorlandBlue,
                AutoSize = true,
                Location = new Point(20, 50)
            };
            
            Label copyrightLabel = new Label
            {
                Text = "",
                Font = new Font("Consolas", 12, FontStyle.Regular),
                ForeColor = BorlandWhite,
                BackColor = BorlandBlue,
                AutoSize = true,
                Location = new Point(20, 80)
            };
            
            // Description text
            Label descriptionLabel = new Label
            {
                Text = "\r\nA nostalgic Retro-style SSH terminal interface designed\r\n" +
                       "to bring back the classic look and feel of DOS and CRT-era\r\n" +
                       "terminal applications while providing modern SSH\r\n" +
                       "connectivity and features.\r\n\r\n© 2025 Scott Peterman",
                Font = new Font("Consolas", 10, FontStyle.Regular),
                ForeColor = BorlandYellow,
                BackColor = BorlandBlue,
                AutoSize = true,
                Location = new Point(20, 120)
            };
            
            // Credits
            Label creditsLabel = new Label
            {
                Text = "DEVELOPERS:",
                Font = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = BorlandWhite,
                BackColor = BorlandBlue,
                AutoSize = true,
                Location = new Point(20, 200)
            };
            
            Label developersLabel = new Label
            {
                Text = "Scott Peterman",
                Font = new Font("Consolas", 10, FontStyle.Regular),
                ForeColor = BorlandWhite,
                BackColor = BorlandBlue,
                AutoSize = true,
                Location = new Point(150, 200)
            };
            
            // Function key panel
            Panel functionPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(192, 192, 192), // Gray like your function key bar
                ForeColor = BorlandBlack
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
            
            // Function key labels
            Label keyLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(15, 3, 15, 3),
                Text = "Esc-Close",
                Font = new Font(FontFamily.GenericMonospace, 10, FontStyle.Regular),
                BackColor = Color.Transparent
            };
            
            // Custom paint for Turbo C style (F-key in red)
            keyLabel.Paint += (s, e) =>
            {
                e.Graphics.Clear(Color.FromArgb(192, 192, 192)); // Gray background
                
                string text = ((Label)s).Text;
                int dashIndex = text.IndexOf('-');
                if (dashIndex > 0)
                {
                    string key = text.Substring(0, dashIndex);
                    string desc = text.Substring(dashIndex);
                    
                    // Draw the function key part in red
                    using (Brush redBrush = new SolidBrush(Color.Red))
                    {
                        e.Graphics.DrawString(key, ((Label)s).Font, redBrush, 0, 0);
                    }
                    
                    // Get key width to position description
                    SizeF keySize = e.Graphics.MeasureString(key, ((Label)s).Font);
                    
                    // Draw the description in black
                    using (Brush blackBrush = new SolidBrush(Color.Black))
                    {
                        e.Graphics.DrawString(desc, ((Label)s).Font, blackBrush, keySize.Width, 0);
                    }
                }
            };
            
            keyFlow.Controls.Add(keyLabel);
            
            // Add the flow layout to the function panel
            functionPanel.Controls.Add(keyFlow);
            
            // Add content to panels
            contentPanel.Controls.Add(appNameLabel);
            contentPanel.Controls.Add(versionLabel);
            contentPanel.Controls.Add(copyrightLabel);
            contentPanel.Controls.Add(descriptionLabel);
            contentPanel.Controls.Add(creditsLabel);
            contentPanel.Controls.Add(developersLabel);
            
            // Add all panels to the form
            mainPanel.Controls.Add(contentPanel);
            this.Controls.Add(functionPanel);
            this.Controls.Add(mainPanel);
            this.Controls.Add(titleLabel);
            
            // Add event handlers for keyboard
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            };
        }
    }
}