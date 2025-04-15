using SessionNavigatorControl;

namespace SessionNavigatorControl.TestApp;


partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private SessionNavigator sessionNavigator;

    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
       this.sessionNavigator = new SessionNavigator();
    
    // Configure session navigator
    this.sessionNavigator.Dock = System.Windows.Forms.DockStyle.Fill;
    this.sessionNavigator.ConnectRequested += SessionNavigator_ConnectRequested;

    // Add the control to the form
    this.Controls.Add(this.sessionNavigator);

    // Form Settings
    this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
    this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
    this.ClientSize = new System.Drawing.Size(800, 450);
    this.Text = "SessionNavigator Test App";
    }

    #endregion
}
