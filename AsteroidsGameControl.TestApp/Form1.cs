using AsteroidsGameControl;

namespace AsteroidsGameControl.TestApp
{
    public partial class Form1 : Form
    {
        private AsteroidsControl asteroidsGame;
        
        public Form1()
        {
            InitializeComponent();
            
            // Create and add the Asteroids control
            asteroidsGame = new AsteroidsControl();
            asteroidsGame.Dock = DockStyle.Fill;
            this.Controls.Add(asteroidsGame);
            
            // Make sure the control gets focus for keyboard input
            this.Load += (s, e) => asteroidsGame.Focus();
        }
    }
}