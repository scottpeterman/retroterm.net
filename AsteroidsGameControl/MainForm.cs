using System;
using System.Drawing;
using System.Windows.Forms;

namespace AsteroidsGame
{
    public partial class MainForm : Form
    {
        private AsteroidsControl asteroidsControl;
        
        public MainForm()
        {
            // InitializeComponent();
            
            // Create and add the Asteroids control
            asteroidsControl = new AsteroidsControl();
            asteroidsControl.GameColor = Color.Cyan; // Set your preferred color
            asteroidsControl.Dock = DockStyle.Fill;
            
            // Connect to events if needed
            asteroidsControl.ScoreChanged += (s, score) => { /* Update score display */ };
            asteroidsControl.GameStateChanged += (s, state) => 
            { 
                // Update UI based on game state
                switch (state)
                {
                    case GameState.RUNNING:
                        // Update UI for running state
                        break;
                    case GameState.PAUSED:
                        // Update UI for paused state
                        break;
                    case GameState.GAME_OVER:
                        // Update UI for game over state
                        break;
                }
            };
            
            this.Controls.Add(asteroidsControl);
            
            // Make sure the control can receive keyboard input
            this.KeyPreview = false;
            asteroidsControl.Focus();
        }
        
        // Add menu items or buttons to control the game
        private void startButton_Click(object sender, EventArgs e)
        {
            asteroidsControl.StartGame();
            asteroidsControl.Focus();
        }
        
        private void pauseButton_Click(object sender, EventArgs e)
        {
            asteroidsControl.PauseGame();
            asteroidsControl.Focus();
        }
        
        private void resumeButton_Click(object sender, EventArgs e)
        {
            asteroidsControl.ResumeGame();
            asteroidsControl.Focus();
        }
    }
}