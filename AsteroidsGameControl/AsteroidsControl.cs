using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer; 

public enum GameState
{
    WAITING,
    RUNNING,
    PAUSED,
    GAME_OVER
}

public class AsteroidsControl : UserControl
{
    // Game objects
    private Ship ship;
    private List<Asteroid> asteroids = new List<Asteroid>();
    private List<Bullet> bullets = new List<Bullet>();
    
    // Game state
    private GameState state = GameState.WAITING;
    private Timer gameTimer;
    private int score = 0;
    private int lives = 3;
    private int level = 1;
    
    // Input tracking
    private bool leftPressed = false;
    private bool rightPressed = false;
    private bool thrustPressed = false;
    private bool spacePressed = false;
    
    // Graphics and UI
    private Color gameColor = Color.Cyan;
    private Font gameFont;
    
    // Events
    public event EventHandler<int> ScoreChanged;
    public event EventHandler<int> LivesChanged;
    public event EventHandler<int> LevelChanged;
    public event EventHandler<GameState> GameStateChanged;
    
    public AsteroidsControl()
    {
        this.DoubleBuffered = true;
        this.BackColor = Color.Black;
        this.Size = new Size(800, 600);
            InitializeComponent();

        // Initialize font
        gameFont = new Font("Consolas", 14);
        
        // Create ship (initially invisible)
        ship = new Ship(this.Width / 2, this.Height / 2, gameColor);
        ship.Visible = false;
        
        // Initialize timer
        gameTimer = new Timer();
        gameTimer.Interval = 16;
        gameTimer.Tick += (s, e) => { UpdateGame(); this.Invalidate(); };
        
        // Set up input handling
        this.KeyDown += OnKeyDown;
        this.KeyUp += OnKeyUp;
        this.SetStyle(ControlStyles.Selectable, true);
        this.TabStop = true;
    }
    
    private void InitializeComponent()
{
    // Set control properties
    this.DoubleBuffered = true;
    this.BackColor = Color.Black;
    this.Size = new Size(800, 600);
    
    // Create and configure the game timer
    gameTimer = new System.Windows.Forms.Timer();
    gameTimer.Interval = 16;
    gameTimer.Tick += (s, e) => { UpdateGame(); this.Invalidate(); };
    
    // Set up input handling
    this.KeyDown += OnKeyDown;
    this.KeyUp += OnKeyUp;
    this.SetStyle(ControlStyles.Selectable, true);
    this.TabStop = true;
}
protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
{
    // Capture arrow keys and prevent them from being used for form navigation
    if (keyData == Keys.Left || keyData == Keys.Right || 
        keyData == Keys.Up || keyData == Keys.Down)
    {
        // Handle the key as if it were a normal key press
        switch (keyData)
        {
            case Keys.Left:
                leftPressed = true;
                break;
            case Keys.Right:
                rightPressed = true;
                break;
            case Keys.Up:
                thrustPressed = true;
                break;
            case Keys.Down:
                // Optional - if you want to add a brake or something
                break;
        }
        
        return true; // Tell the system we handled this key
    }
    
    return base.ProcessCmdKey(ref msg, keyData);
}

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        
        // Draw border
        g.DrawRectangle(new Pen(gameColor, 2), 2, 2, Width - 4, Height - 4);
        
        // Draw game objects
        if (ship.Visible)
            ship.Draw(g);
            
        foreach (var asteroid in asteroids)
            asteroid.Draw(g);
            
        foreach (var bullet in bullets)
            bullet.Draw(g);
        
        // Draw UI text
        using (Brush textBrush = new SolidBrush(gameColor))
        {
            g.DrawString($"{score:00000}", gameFont, textBrush, 10, 10);
            g.DrawString($"SHIPS {lives}", gameFont, textBrush, 10, 50);
            
            // Draw messages based on game state
            if (state == GameState.WAITING)
            {
                DrawCenteredText(g, "PUSH START", textBrush, 250);
            }
            else if (state == GameState.PAUSED)
            {
                DrawCenteredText(g, "PAUSED", textBrush, 250);
            }
            else if (state == GameState.GAME_OVER)
            {
                DrawCenteredText(g, "GAME OVER", textBrush, 250);
                DrawCenteredText(g, "PUSH START", textBrush, 300);
            }
        }
    }

    private void DrawCenteredText(Graphics g, string text, Brush brush, int y)
    {
        SizeF textSize = g.MeasureString(text, gameFont);
        g.DrawString(text, gameFont, brush, (Width - textSize.Width) / 2, y);
    }
    
    private void UpdateGame()
    {
        if (state != GameState.RUNNING)
            return;
            
        // Update ship
        if (ship.Visible)
        {
            ship.Update(leftPressed, rightPressed, thrustPressed);
            WrapObject(ship);
            
            // Check for ship-asteroid collisions
            foreach (var asteroid in asteroids)
            {
                if (ship.Bounds.IntersectsWith(asteroid.Bounds))
                {
                    HandleShipCollision();
                    break;
                }
            }
        }
        
        // Update bullets
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            Bullet bullet = bullets[i];
            bullet.Update();
            
            // Remove bullets that are out of bounds
            if (!IsInBounds(bullet.Position))
            {
                bullets.RemoveAt(i);
                continue;
            }
            
            // Check for bullet-asteroid collisions
            bool bulletHit = false;
            for (int j = asteroids.Count - 1; j >= 0; j--)
            {
                Asteroid asteroid = asteroids[j];
                if (bullet.Bounds.IntersectsWith(asteroid.Bounds))
                {
                    HandleAsteroidDestruction(asteroid, j);
                    bullets.RemoveAt(i);
                    bulletHit = true;
                    break;
                }
            }
            
            if (bulletHit) continue;
        }
        
        // Update asteroids
        foreach (var asteroid in asteroids)
        {
            asteroid.Update();
            WrapObject(asteroid);
        }
        
        // Fire bullet if space was pressed
        if (spacePressed)
        {
            spacePressed = false;
            if (ship.Visible)
                bullets.Add(ship.Fire());
        }
        
        // Check if level is complete
        if (asteroids.Count == 0)
        {
            StartNewLevel();
        }
    }
    
    private void HandleShipCollision()
    {
        lives--;
        OnLivesChanged();
        
        if (lives <= 0)
        {
            GameOver();
            return;
        }
        
        // Reset ship and make it temporarily invulnerable
        ship.Reset(Width / 2, Height / 2);
        ship.Visible = false;
        
        // Use timer to respawn ship after delay
        Timer respawnTimer = new Timer();
        respawnTimer.Interval = 2000;
        respawnTimer.Tick += (s, e) =>
        {
            if (state == GameState.RUNNING)
                ship.Visible = true;
            respawnTimer.Stop();
            respawnTimer.Dispose();
        };
        respawnTimer.Start();
    }
    
    private void HandleAsteroidDestruction(Asteroid asteroid, int index)
    {
        int size = asteroid.Size;
        PointF position = asteroid.Position;
        
        // Remove the hit asteroid
        asteroids.RemoveAt(index);
        
        // Increase score based on asteroid size
        score += (4 - size) * 100;
        OnScoreChanged();
        
        // Split into smaller asteroids if not the smallest size
        if (size > 1)
        {
            for (int i = 0; i < 2; i++)
            {
                Asteroid newAsteroid = new Asteroid(Width, Height, size - 1, gameColor);
                newAsteroid.Position = position;
                asteroids.Add(newAsteroid);
            }
        }
    }
    
    private void StartNewLevel()
    {
        level++;
        OnLevelChanged();
        
        // Reset ship position but keep it visible
        ship.Reset(Width / 2, Height / 2);
        
        // Calculate number of asteroids based on level (with a cap)
        int numAsteroids = Math.Min(3 + level, 11);
        
        // Spawn new asteroids (away from the ship)
        asteroids.Clear();
        Random rand = new Random();
        
        for (int i = 0; i < numAsteroids; i++)
        {
            Asteroid asteroid;
            do
            {
                asteroid = new Asteroid(Width, Height, 3, gameColor);
            } while (Distance(asteroid.Position, ship.Position) < 100);
            
            asteroids.Add(asteroid);
        }
    }
    
    private double Distance(PointF a, PointF b)
    {
        return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }
    
    public void StartGame()
    {
        // Reset game state
        state = GameState.RUNNING;
        OnGameStateChanged();
        score = 0;
        OnScoreChanged();
        lives = 3;
        OnLivesChanged();
        level = 1;
        OnLevelChanged();
        
        // Clear objects
        bullets.Clear();
        asteroids.Clear();
        
        // Reset and show ship
        ship.Reset(Width / 2, Height / 2);
        ship.Visible = true;
        
        // Start level
        StartNewLevel();
        
        // Start game timer
        gameTimer.Start();
    }
    
    public void PauseGame()
    {
        if (state == GameState.RUNNING)
        {
            state = GameState.PAUSED;
            OnGameStateChanged();
            gameTimer.Stop();
        }
    }
    
    public void ResumeGame()
    {
        if (state == GameState.PAUSED)
        {
            state = GameState.RUNNING;
            OnGameStateChanged();
            gameTimer.Start();
        }
    }
    
    private void GameOver()
    {
        state = GameState.GAME_OVER;
        OnGameStateChanged();
        gameTimer.Stop();
        ship.Visible = false;
    }
    
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            if (state == GameState.WAITING || state == GameState.GAME_OVER)
            {
                StartGame();
                return;
            }
            else if (state == GameState.RUNNING)
            {
                spacePressed = true;
            }
        }
        
        if (state == GameState.RUNNING)
        {
            if (e.KeyCode == Keys.Left) leftPressed = true;
            if (e.KeyCode == Keys.Right) rightPressed = true;
            if (e.KeyCode == Keys.Up) thrustPressed = true;
            if (e.KeyCode == Keys.P)
            {
                PauseGame();
            }
        }
        else if (state == GameState.PAUSED)
        {
            if (e.KeyCode == Keys.P)
            {
                ResumeGame();
            }
        }
    }
    
    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Left) leftPressed = false;
        if (e.KeyCode == Keys.Right) rightPressed = false;
        if (e.KeyCode == Keys.Up) thrustPressed = false;
    }
    
    // Helper methods for position wrapping
    private void WrapObject(GameObject obj)
    {
        float x = obj.Position.X;
        float y = obj.Position.Y;
        bool changed = false;
        
        // Add buffer for wrapping
        if (x < -10)
        {
            x = Width + 10;
            changed = true;
        }
        else if (x > Width + 10)
        {
            x = -10;
            changed = true;
        }
        
        if (y < -10)
        {
            y = Height + 10;
            changed = true;
        }
        else if (y > Height + 10)
        {
            y = -10;
            changed = true;
        }
        
        if (changed)
        {
            obj.Position = new PointF(x, y);
        }
    }
    
    private bool IsInBounds(PointF position)
    {
        return position.X >= -10 && position.X <= Width + 10 &&
               position.Y >= -10 && position.Y <= Height + 10;
    }
    
    // Event raising methods
    private void OnScoreChanged()
    {
        ScoreChanged?.Invoke(this, score);
    }
    
    private void OnLivesChanged()
    {
        LivesChanged?.Invoke(this, lives);
    }
    
    private void OnLevelChanged()
    {
        LevelChanged?.Invoke(this, level);
    }
    
    private void OnGameStateChanged()
    {
        GameStateChanged?.Invoke(this, state);
    }
    
    // Public properties and methods
    public Color GameColor
    {
        get { return gameColor; }
        set
        {
            gameColor = value;
            ship.Color = value;
            foreach (var asteroid in asteroids)
                asteroid.Color = value;
            foreach (var bullet in bullets)
                bullet.Color = value;
            Invalidate();
        }
    }
    
    public int Score => score;
    public int Lives => lives;
    public int Level => level;
    public GameState State => state;
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            gameTimer?.Dispose();
            gameFont?.Dispose();
        }
        base.Dispose(disposing);
    }
}

// Base class for all game objects
public abstract class GameObject
{
    public PointF Position { get; set; }
    public PointF Velocity { get; set; }
    public float Angle { get; set; }
    public Color Color { get; set; }
    public bool Visible { get; set; } = true;
    
    public abstract Rectangle Bounds { get; }
    public abstract void Draw(Graphics g);
    public abstract void Update();
}

public class Ship : GameObject
{
    private const float ShipRadius = 10f;
    private const float RotationSpeed = 5f;
    private const float ThrustForce = 0.2f;
    private const float Drag = 0.99f;
    
    private bool thrustActive;
    
    public Ship(float x, float y, Color color)
    {
        Position = new PointF(x, y);
        Velocity = new PointF(0, 0);
        Angle = 270; // Pointing up
        Color = color;
    }
    
    public override Rectangle Bounds
    {
        get
        {
            int size = (int)(ShipRadius * 2);
            return new Rectangle(
                (int)(Position.X - ShipRadius),
                (int)(Position.Y - ShipRadius),
                size, size);
        }
    }
    
    public override void Draw(Graphics g)
    {
        if (!Visible) return;
        
        using (var pen = new Pen(Color, 1.5f))
        {
            // Save the graphics state
            var state = g.Save();
            
            // Translate and rotate to draw ship
            g.TranslateTransform(Position.X, Position.Y);
            g.RotateTransform(Angle);
            
            // Draw ship
            PointF[] shipPoints = new PointF[]
            {
                new PointF(0, -ShipRadius),         // Nose
                new PointF(-ShipRadius * 0.7f, ShipRadius), // Left corner
                new PointF(0, ShipRadius * 0.7f),   // Back center
                new PointF(ShipRadius * 0.7f, ShipRadius)  // Right corner
            };
            
            g.DrawLine(pen, shipPoints[0], shipPoints[1]);
            g.DrawLine(pen, shipPoints[1], shipPoints[2]);
            g.DrawLine(pen, shipPoints[2], shipPoints[3]);
            g.DrawLine(pen, shipPoints[3], shipPoints[0]);
            
            // Draw thrust if active
            if (thrustActive)
            {
                PointF[] thrustPoints = new PointF[]
                {
                    new PointF(-3, ShipRadius * 0.8f),
                    new PointF(0, ShipRadius * 1.3f),
                    new PointF(3, ShipRadius * 0.8f)
                };
                
                g.DrawLine(pen, thrustPoints[0], thrustPoints[1]);
                g.DrawLine(pen, thrustPoints[1], thrustPoints[2]);
            }
            
            // Restore graphics state
            g.Restore(state);
        }
    }
    
    public void Update(bool rotateLeft, bool rotateRight, bool thrust)
    {
        if (!Visible) return;
        
        // Update rotation
        if (rotateLeft)
            Angle -= RotationSpeed;
        if (rotateRight)
            Angle += RotationSpeed;
        
        // Apply thrust
        thrustActive = thrust;
        if (thrust)
        {
            float radians = (float)(Math.PI * Angle / 180.0);
            Velocity = new PointF(
                Velocity.X + (float)(Math.Sin(radians) * ThrustForce),
                Velocity.Y - (float)(Math.Cos(radians) * ThrustForce)
            );
        }
        
        // Apply drag
        Velocity = new PointF(Velocity.X * Drag, Velocity.Y * Drag);
        
        // Update position
        Position = new PointF(
            Position.X + Velocity.X,
            Position.Y + Velocity.Y
        );
    }
    
    public Bullet Fire()
    {
        float radians = (float)(Math.PI * Angle / 180.0);
        
        // Calculate bullet starting position (at ship's nose)
        PointF bulletPos = new PointF(
            Position.X + (float)(Math.Sin(radians) * ShipRadius),
            Position.Y - (float)(Math.Cos(radians) * ShipRadius)
        );
        
        // Create and return bullet
        return new Bullet(bulletPos, Angle, Color);
    }
    
    public void Reset(float x, float y)
    {
        Position = new PointF(x, y);
        Velocity = new PointF(0, 0);
        Angle = 270; // Pointing up
        thrustActive = false;
    }
    
    public override void Update()
    {
        // This is handled by the other Update method
    }
}

public class Bullet : GameObject
{
    private const float BulletSpeed = 10f;
    private const float BulletSize = 2f;
    
    public Bullet(PointF position, float angle, Color color)
    {
        Position = position;
        Angle = angle;
        Color = color;
        
        // Calculate velocity based on angle
        float radians = (float)(Math.PI * angle / 180.0);
        Velocity = new PointF(
            (float)(Math.Sin(radians) * BulletSpeed),
            -(float)(Math.Cos(radians) * BulletSpeed)
        );
    }
    
    public override Rectangle Bounds
    {
        get
        {
            return new Rectangle(
                (int)(Position.X - BulletSize),
                (int)(Position.Y - BulletSize),
                (int)(BulletSize * 2),
                (int)(BulletSize * 2));
        }
    }
    
    public override void Draw(Graphics g)
    {
        using (var brush = new SolidBrush(Color))
        {
            g.FillEllipse(brush, 
                Position.X - BulletSize, 
                Position.Y - BulletSize, 
                BulletSize * 2, 
                BulletSize * 2);
        }
    }
    
    public override void Update()
    {
        Position = new PointF(
            Position.X + Velocity.X,
            Position.Y + Velocity.Y
        );
    }
}

public class Asteroid : GameObject
{
    private const float BaseRadius = 10f;
    private readonly PointF[] points;
    private readonly float rotationSpeed;
    private readonly int size;
    
    public int Size => size;
    
    public Asteroid(int screenWidth, int screenHeight, int size, Color color)
    {
        this.size = size;
        Color = color;
        
        // Random position
        Random rand = new Random();
        Position = new PointF(
            rand.Next(screenWidth),
            rand.Next(screenHeight)
        );
        
        // Random velocity
        float speed = (float)rand.NextDouble() * 1.5f + 0.5f;
        float angle = (float)(rand.NextDouble() * Math.PI * 2);
        Velocity = new PointF(
            (float)(Math.Cos(angle) * speed),
            (float)(Math.Sin(angle) * speed)
        );
        
        // Random rotation
        rotationSpeed = (float)(rand.NextDouble() * 2 - 1);
        Angle = 0;
        
        // Generate asteroid shape
        int numPoints = rand.Next(6, 9);
        points = new PointF[numPoints];
        
        for (int i = 0; i < numPoints; i++)
        {
            float pointAngle = (float)(i * 2 * Math.PI / numPoints);
            float radius = BaseRadius * size * (1 + (float)(rand.NextDouble() * 0.4 - 0.2));
            points[i] = new PointF(
                (float)(Math.Cos(pointAngle) * radius),
                (float)(Math.Sin(pointAngle) * radius)
            );
        }
    }
    
    public override Rectangle Bounds
    {
        get
        {
            float radius = BaseRadius * size;
            return new Rectangle(
                (int)(Position.X - radius),
                (int)(Position.Y - radius),
                (int)(radius * 2),
                (int)(radius * 2));
        }
    }
    
    public override void Draw(Graphics g)
    {
        using (var pen = new Pen(Color, 1.5f))
        {
            // Save graphics state
            var state = g.Save();
            
            // Translate and rotate to draw asteroid
            g.TranslateTransform(Position.X, Position.Y);
            g.RotateTransform(Angle);
            
            // Draw asteroid shape
            for (int i = 0; i < points.Length; i++)
            {
                int nextIndex = (i + 1) % points.Length;
                g.DrawLine(pen, points[i], points[nextIndex]);
            }
            
            // Restore graphics state
            g.Restore(state);
        }
    }
    
    public override void Update()
    {
        // Update position
        Position = new PointF(
            Position.X + Velocity.X,
            Position.Y + Velocity.Y
        );
        
        // Update rotation
        Angle += rotationSpeed;
    }
}