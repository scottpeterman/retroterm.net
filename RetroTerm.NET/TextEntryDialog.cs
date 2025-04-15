using System;
using System.Drawing;
using System.Windows.Forms;
using RetroTerm.NET.Services;
using RetroTerm.NET.Models;

namespace RetroTerm.NET
{
    /// <summary>
    /// A simple dialog for text entry with a retro DOS-style theme.
    /// Used for folder names and other simple string inputs.
    /// </summary>
    public class TextEntryDialog : Form
    {
        private TextBox textBox;
        
        /// <summary>
        /// Gets the text entered by the user.
        /// </summary>
        public string EnteredText => textBox.Text;
        
        /// <summary>
        /// Creates a new text entry dialog.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="prompt">The prompt text shown to the user.</param>
        /// <param name="defaultText">The default text to populate the text box.</param>
        /// <param name="themeManager">The theme manager for styling the dialog.</param>
        public TextEntryDialog(string title, string prompt, string defaultText, ThemeManager themeManager)
        {
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(450, 350);
            
            // Initialize DOS font
            Font dosFont;
            try
            {
                dosFont = new Font("Perfect DOS VGA 437", 12, FontStyle.Regular);
            }
            catch
            {
                dosFont = new Font(FontFamily.GenericMonospace, 12, FontStyle.Regular);
            }
            
            this.Font = dosFont;
            
            // Title bar
            Panel titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.Silver
            };
            
            Label titleLabel = new Label
            {
                Text = title,
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
            
            // Content panel
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };
            
            // Prompt label
            Label promptLabel = new Label
            {
                Text = prompt,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            
            // Text box
            textBox = new TextBox
            {
                Text = defaultText,
                Width = this.Width - 80, // Wider
                Height = 25,
                Location = new Point(20, promptLabel.Bottom + 15),
                Font = dosFont,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // Buttons Panel for centering
            Panel buttonPanel = new Panel
            {
                Width = this.Width - 80,
                Height = 50,
                Location = new Point(20, textBox.Bottom + 30),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            
            // Buttons with adequate width
            Button okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 40), // Wider and taller
                Location = new Point(buttonPanel.Width / 2 - 130, 5),
                Font = dosFont
            };
            
            Button cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 40), // Wider and taller
                Location = new Point(buttonPanel.Width / 2 + 10, 5),
                Font = dosFont
            };
            
            // Add buttons to panel
            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);
            
            // Add controls
            contentPanel.Controls.Add(promptLabel);
            contentPanel.Controls.Add(textBox);
            contentPanel.Controls.Add(buttonPanel);
            
            // Add panels to form
            this.Controls.Add(contentPanel);
            this.Controls.Add(titleBar);
            
            // Set accept button
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
            
            // Draw border - thicker 2px border
            this.Paint += FormBorderPaint;
            
            // Apply theme
            if (themeManager?.CurrentTheme != null)
            {
                ApplyTheme(themeManager.CurrentTheme);
            }
            
            // Handle resize event to reposition buttons
            this.Resize += Dialog_Resize;
        }
        
        /// <summary>
        /// Handles the form resize event to reposition buttons.
        /// </summary>
        private void Dialog_Resize(object sender, EventArgs e)
        {
            if (Controls.Count > 0 && Controls[0] is Panel contentPanel)
            {
                foreach (Control control in contentPanel.Controls)
                {
                    if (control is Panel buttonPanel)
                    {
                        foreach (Control btn in buttonPanel.Controls)
                        {
                            if (btn is Button button)
                            {
                                if (button.Text == "OK")
                                {
                                    button.Location = new Point(buttonPanel.Width / 2 - button.Width - 10, button.Location.Y);
                                }
                                else if (button.Text == "Cancel")
                                {
                                    button.Location = new Point(buttonPanel.Width / 2 + 10, button.Location.Y);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Paints the form border.
        /// </summary>
        private void FormBorderPaint(object sender, PaintEventArgs e)
        {
            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (Pen pen = new Pen(this.ForeColor, 2))
            {
                e.Graphics.DrawRectangle(pen, rect);
            }
        }
        
        /// <summary>
        /// Makes the title bar draggable.
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                const int WM_NCLBUTTONDOWN = 0xA1;
                const int HT_CAPTION = 0x2;
                
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        
        /// <summary>
        /// Applies the theme to the dialog.
        /// </summary>
        private void ApplyTheme(Models.Theme theme)
        {
            // Apply theme to form
            this.BackColor = Theme.HexToColor(theme.UI.Background);
            this.ForeColor = Theme.HexToColor(theme.UI.Text);
            
            // Apply theme to controls
            ApplyThemeToControls(this.Controls, theme);
        }
        
        /// <summary>
        /// Recursively applies the theme to all controls.
        /// </summary>
        private void ApplyThemeToControls(Control.ControlCollection controls, Models.Theme theme)
        {
            foreach (Control control in controls)
            {
                if (control is Panel panel)
                {
                    if (panel.Name == "titleBar" || control.Parent?.Name == "titleBar")
                    {
                        panel.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                    }
                    else
                    {
                        panel.BackColor = Theme.HexToColor(theme.UI.Background);
                    }
                    
                    // Apply to child controls
                    ApplyThemeToControls(panel.Controls, theme);
                }
                else if (control is Label label)
                {
                    if (control.Parent?.Name == "titleBar")
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
                else if (control is Button button)
                {
                    if (control.Parent?.Name == "titleBar")
                    {
                        button.BackColor = Theme.HexToColor(theme.UI.MenuBackground);
                        button.ForeColor = Theme.HexToColor(theme.UI.MenuText);
                        button.FlatAppearance.BorderSize = 0;
                    }
                    else
                    {
                        button.BackColor = Theme.HexToColor(theme.UI.ButtonBackground);
                        button.ForeColor = Theme.HexToColor(theme.UI.ButtonText);
                        button.FlatAppearance.BorderColor = Theme.HexToColor(theme.UI.Border);
                    }
                }
            }
        }
        
        /// <summary>
        /// Shows the dialog with validation for required input.
        /// </summary>
        public new DialogResult ShowDialog(IWin32Window owner = null)
        {
            DialogResult result;
            do
            {
                if (owner != null)
                {
                    result = base.ShowDialog(owner);
                }
                else
                {
                    result = base.ShowDialog();
                }
                
                // If OK was clicked but text is empty, show error
                if (result == DialogResult.OK && string.IsNullOrWhiteSpace(textBox.Text))
                {
                    MessageBox.Show("Please enter a value.", "Input Required", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
            } while (result == DialogResult.OK && string.IsNullOrWhiteSpace(textBox.Text));
            
            return result;
        }
        
        /// <summary>
        /// Sets the entered text programmatically.
        /// </summary>
        public void SetText(string text)
        {
            textBox.Text = text ?? string.Empty;
        }
        
        /// <summary>
        /// Native method for window dragging.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        
        /// <summary>
        /// Native method for window dragging.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
    }
}