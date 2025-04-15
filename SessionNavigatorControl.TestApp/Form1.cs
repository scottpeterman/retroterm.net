using System;
using System.IO;
using System.Windows.Forms;
using SessionNavigatorControl;

namespace SessionNavigatorControl.TestApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Attempt to load 'sessions.yaml' in the working directory
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessions.yaml");

            if (File.Exists(defaultPath))
            {
                sessionNavigator.SetSessionsFilePath(defaultPath);
            }
            else
            {
                MessageBox.Show("sessions.yaml not found in application folder.", 
                                "Warning", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Warning);
            }
        }

        private void SessionNavigator_ConnectRequested(object sender, SessionConnectionEventArgs e)
        {
            string host = e.ConnectionData["host"].ToString();
            int port = Convert.ToInt32(e.ConnectionData["port"]);
            string username = e.ConnectionData["username"].ToString();
            string password = e.ConnectionData["password"].ToString();

            MessageBox.Show(
                $"Connecting to:\nHost: {host}\nPort: {port}\nUser: {username}",
                "Connection Info",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }
}
