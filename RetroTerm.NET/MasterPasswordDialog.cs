using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using YamlDotNet.Serialization;


namespace RetroTerm.NET
/// <summary>
/// Dialog for setting up or entering a master password
/// </summary>
/// 
{
    
public class MasterPasswordDialog : Form
{
    private TextBox passwordTextBox;
    private TextBox confirmPasswordTextBox;
    private Button okButton;
    private Button cancelButton;
    private Button resetButton;
    private Label infoLabel;
    private Label confirmLabel;
    private Label titleLabel;
    private Label passwordLabel;
    
    // Flag indicating if this is first-time setup
    private bool isSetup;
    
    // Theme-related fields
    private Color backgroundColor;
    private Color textColor;
    private Color buttonColor;
    private Color buttonTextColor;
    private Color borderColor;
    
    public string Password => passwordTextBox.Text;
    
    public MasterPasswordDialog(bool isFirstRun = false)
    {
        this.isSetup = isFirstRun;
        
        // Set default colors (fallback if theme manager not available)
        // backgroundColor = Color.FromArgb(0, 0, 170); // Borland Blue
        // textColor = Color.White;
        // buttonColor = Color.FromArgb(0, 170, 170); // Borland Cyan
        // buttonTextColor = Color.Black;
        // borderColor = Color.White;
        backgroundColor = ColorTranslator.FromHtml("#1E1E1E"); // Dark gray background
textColor = ColorTranslator.FromHtml("#FFFFFF"); // White text
buttonColor = ColorTranslator.FromHtml("#2D2D2D"); // Medium gray button
buttonTextColor = ColorTranslator.FromHtml("#FFFFFF"); // White button text
borderColor = ColorTranslator.FromHtml("#3F3F3F"); // Medium gray border
        
        // Try to get theme colors from theme manager if available
        try
        {
            var themeManager = GetThemeManager();
            if (themeManager?.CurrentTheme != null)
            {
                // Use theme colors
                backgroundColor = Models.Theme.HexToColor(themeManager.CurrentTheme.UI.Background);
                textColor = Models.Theme.HexToColor(themeManager.CurrentTheme.UI.Text);
                buttonColor = Models.Theme.HexToColor(themeManager.CurrentTheme.UI.ButtonBackground);
                buttonTextColor = Models.Theme.HexToColor(themeManager.CurrentTheme.UI.ButtonText);
                borderColor = Models.Theme.HexToColor(themeManager.CurrentTheme.UI.Border);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting theme colors: {ex.Message}");
            // Continue with default colors
        }
        
        SetupComponents();
    }
    
    // Helper method to find ThemeManager instance
    
    // In MasterPasswordDialog.cs
private Services.ThemeManager GetThemeManager()
{
    try
    {
        // Try to find the main form
        Form mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
        if (mainForm != null)
        {
            // Try to get theme manager using reflection (to avoid direct dependency)
            var themeManagerField = mainForm.GetType().GetField("themeManager", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (themeManagerField != null)
            {
                return themeManagerField.GetValue(mainForm) as Services.ThemeManager;
            }
        }
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error getting theme manager: {ex.Message}");
        return null;
    }
}

    private void SetupComponents()
    {
        // Form setup
        this.Text = "Set Master Password";
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;

        // Increase form size with proper padding to fix clipping and ensure all content is visible
        // Add extra width to prevent right-side clipping 
        this.Size = new Size(480, isSetup ? 380 : 320);
        
        this.ShowInTaskbar = false;
        this.BackColor = backgroundColor;
        this.ForeColor = textColor;
        
        // Title label - centered for better appearance
        titleLabel = new Label
        {
            Text = isSetup ? "Set Master Password" : "Enter Master Password",
            Font = new Font("Consolas", 12, FontStyle.Bold),
            ForeColor = textColor,
            BackColor = backgroundColor,
            Location = new Point(20, 25),
            Size = new Size(440, 30),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Info label for additional instructions
        infoLabel = new Label
        {
            Text = isSetup ? 
                "" : 
                "",
            Font = new Font("Consolas", 9),
            ForeColor = textColor,
            BackColor = backgroundColor,
            Location = new Point(20, 65),
            Size = new Size(440, 40),
            TextAlign = ContentAlignment.MiddleCenter
        };

        passwordLabel = new Label
        {
            Text = "Password:",
            Font = new Font("Consolas", 10),
            ForeColor = textColor,
            BackColor = backgroundColor,
            Location = new Point(20, 110),
            Size = new Size(180, 25),
            TextAlign = ContentAlignment.MiddleLeft
        };
        
        // Increased spacing and size of text box with proper right-side padding
        passwordTextBox = new TextBox
        {
            Location = new Point(20, 140),
            Size = new Size(390, 30),
            PasswordChar = '*',
            Font = new Font("Consolas", 10),
            BackColor = ControlPaint.Dark(backgroundColor, 0.2f),
            ForeColor = textColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        
        // Confirm password controls - only for setup with increased spacing
        confirmLabel = null;
        confirmPasswordTextBox = null;
        
        if (isSetup)
        {
            confirmLabel = new Label
            {
                Text = "Confirm password:",
                Font = new Font("Consolas", 10),
                ForeColor = textColor,
                BackColor = backgroundColor,
                Location = new Point(20, 180),
                Size = new Size(250, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            confirmPasswordTextBox = new TextBox
            {
                Location = new Point(20, 210),
                Size = new Size(440, 30),
                PasswordChar = '*',
                Font = new Font("Consolas", 10),
                BackColor = ControlPaint.Dark(backgroundColor, 0.2f),
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle
            };
        }
        
        // Calculate button positions for centered layout
        int buttonWidth = 115;
        int buttonSpacing = 15;
        int buttonY = isSetup ? 260 : 200;
        
        // Calculate total width of all buttons + spacing
        int totalButtonsWidth = (buttonWidth * 3) + (buttonSpacing * 2);
        
        // Calculate starting X to center the buttons
        int startX = (this.ClientSize.Width - totalButtonsWidth) / 2;
        
        // OK button - first button
        okButton = new Button
        {
            Text = "OK",
            Location = new Point(startX, buttonY),
            Size = new Size(buttonWidth, 35),
            DialogResult = DialogResult.OK,
            Font = new Font("Consolas", 10, FontStyle.Bold),
            BackColor = buttonColor,
            ForeColor = buttonTextColor,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        okButton.FlatAppearance.BorderColor = borderColor;
        okButton.FlatAppearance.BorderSize = 1;
        
        // Reset button - middle button
        resetButton = new Button
        {
            Text = "Reset",
            Location = new Point(startX + buttonWidth + buttonSpacing, buttonY),
            Size = new Size(buttonWidth, 35),
            DialogResult = DialogResult.Retry, // Using Retry for reset action
            Font = new Font("Consolas", 10, FontStyle.Bold),
            BackColor = buttonColor,
            ForeColor = buttonTextColor,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        resetButton.FlatAppearance.BorderColor = borderColor;
        resetButton.FlatAppearance.BorderSize = 1;
        
        // Cancel button - last button
        cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(startX + (buttonWidth + buttonSpacing) * 2, buttonY),
            Size = new Size(buttonWidth, 35),
            DialogResult = DialogResult.Cancel,
            Font = new Font("Consolas", 10, FontStyle.Bold),
            BackColor = buttonColor,
            ForeColor = buttonTextColor,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        cancelButton.FlatAppearance.BorderColor = borderColor;
        cancelButton.FlatAppearance.BorderSize = 1;
        
        // Add controls
        this.Controls.Add(titleLabel);
        this.Controls.Add(infoLabel);
        this.Controls.Add(passwordLabel);
        this.Controls.Add(passwordTextBox);
        
        if (isSetup && confirmLabel != null && confirmPasswordTextBox != null)
        {
            this.Controls.Add(confirmLabel);
            this.Controls.Add(confirmPasswordTextBox);
        }
        
        this.Controls.Add(okButton);
        this.Controls.Add(resetButton);
        this.Controls.Add(cancelButton);
        
        // Event handlers
        this.AcceptButton = okButton;
        this.CancelButton = cancelButton;
        okButton.Click += OkButton_Click;
        passwordTextBox.TextChanged += PasswordTextBox_TextChanged;
        
        if (isSetup && confirmPasswordTextBox != null)
        {
            confirmPasswordTextBox.TextChanged += PasswordTextBox_TextChanged;
        }
        
        // Add resize event handler to recenter buttons when form size changes
        this.Resize += (s, e) => {
            int newStartX = (this.ClientSize.Width - totalButtonsWidth) / 2;
            okButton.Left = newStartX;
            resetButton.Left = newStartX + buttonWidth + buttonSpacing;
            cancelButton.Left = newStartX + (buttonWidth + buttonSpacing) * 2;
        };
        resetButton.Click += resetButton_Click;

        // Add paint handler for border - renamed to avoid naming conflict
        this.Paint += FormBorder_Paint;
    }
    
    // Add to MasterPasswordDialog.cs
private void resetButton_Click(object sender, EventArgs e)
{
    // Confirm reset
    DialogResult result = MessageBox.Show(
        "Resetting your password will make all currently encrypted passwords unreadable.\n\n" +
        "You will need to re-enter all your session passwords after this action.\n\n" +
        "Are you sure you want to reset your password?",
        "Confirm Password Reset",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning,
        MessageBoxDefaultButton.Button2);
        
    if (result == DialogResult.Yes)
    {
        // Set the dialog result to Retry to indicate the reset action
        this.DialogResult = DialogResult.Retry;
        this.Close();
    }
    else
    {
        // User canceled the reset
        this.DialogResult = DialogResult.None;
    }
}
    // Renamed from MasterPasswordDialog_Paint to FormBorder_Paint to avoid naming conflict
    private void FormBorder_Paint(object sender, PaintEventArgs e)
    {
        // Draw a double border around the form for retro style
        Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
        using (Pen pen = new Pen(borderColor, 2))
        {
            e.Graphics.DrawRectangle(pen, rect);
        }
        
        // Inner border
        Rectangle innerRect = new Rectangle(3, 3, this.Width - 7, this.Height - 7);
        using (Pen pen = new Pen(borderColor, 1))
        {
            e.Graphics.DrawRectangle(pen, innerRect);
        }
    }
    
    private void OkButton_Click(object sender, EventArgs e)
    {
        // For setup mode, validate passwords match
        if (isSetup && confirmPasswordTextBox != null)
        {
            if (passwordTextBox.Text != confirmPasswordTextBox.Text)
            {
                MessageBox.Show(
                    "Passwords do not match. Please try again.",
                    "Password Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }
            
            // Validate password length
            if (string.IsNullOrWhiteSpace(passwordTextBox.Text) || passwordTextBox.Text.Length < 6)
            {
                MessageBox.Show(
                    "Please enter a password with at least 6 characters.",
                    "Password Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }
        }
        else if (!isSetup)
        {
            // For login mode, just make sure password isn't empty
            if (string.IsNullOrWhiteSpace(passwordTextBox.Text))
            {
                MessageBox.Show(
                    "Please enter your master password.",
                    "Password Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }
        }
        
        // All checks passed
        this.DialogResult = DialogResult.OK;
        this.Close();
    }
    
    private void PasswordTextBox_TextChanged(object sender, EventArgs e)
    {
        // Enable OK button logic
        if (isSetup && confirmPasswordTextBox != null)
        {
            // For setup: enable only if both fields have content
            okButton.Enabled = !string.IsNullOrWhiteSpace(passwordTextBox.Text) && 
                               !string.IsNullOrWhiteSpace(confirmPasswordTextBox.Text);
        }
        else
        {
            // For login: enable if password has content
            okButton.Enabled = !string.IsNullOrWhiteSpace(passwordTextBox.Text);
        }
    }
}

}