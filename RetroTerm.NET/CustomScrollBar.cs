using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace RetroTerm.NET
{
    /// <summary>
    /// Custom scrollbar control with improved visual appearance and interaction
    /// </summary>
    public class CustomScrollBar : UserControl
    {
        #region Private Fields
        
        // Underlying scrollbar used for value storage
        private VScrollBar vScrollBar;
        
        // Visual customization flags
        private bool isCustomDrawing = true;
        
        public bool EnableEvents { get; set; } = true;
public event EventHandler ValueChanged;


        // Colors
        private Color thumbColor = Color.Gray;
        private Color trackColor = Color.Black;
        private Color borderColor = Color.DarkGray;
        private Color hoverThumbColor;
        private Color dragThumbColor;
        
        // Interaction state
        private bool isThumbHovered = false;
        private bool isDragging = false;
        private int dragStartOffset = 0;
        
        // Debug configuration
        private bool enableLogging = false;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Gets or sets the current scroll position
        /// </summary>
        
        // Then, modify your existing value property to trigger the event
public int Value 
{ 
    get { return vScrollBar.Value; } 
    set 
    { 
        // Ensure value stays within valid range
        int validValue = Math.Max(vScrollBar.Minimum, 
                      Math.Min(vScrollBar.Maximum - vScrollBar.LargeChange + 1, value));
        
        if (vScrollBar.Value != validValue)
        {
            vScrollBar.Value = validValue;
            this.Invalidate();
            
            // Trigger ValueChanged event
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    } 
}
        /// <summary>
        /// Gets or sets the minimum scroll value
        /// </summary>
        public int Minimum 
        { 
            get { return vScrollBar.Minimum; } 
            set { vScrollBar.Minimum = value; } 
        }
        
        /// <summary>
        /// Gets or sets the maximum scroll value
        /// </summary>
        public int Maximum 
        { 
            get { return vScrollBar.Maximum; } 
            set 
            { 
                if (enableLogging)
                    Console.WriteLine($"Setting Maximum: {value} (was {vScrollBar.Maximum})");
                
                // Ensure maximum is at least 1
                vScrollBar.Maximum = Math.Max(1, value); 
                
                // Make sure current Value is still in range
                if (Value > Maximum - LargeChange + 1)
                    Value = Maximum - LargeChange + 1;
                    
                this.Invalidate();
            } 
        }
        
        /// <summary>
        /// Gets or sets the large change value (page size)
        /// </summary>
        public int LargeChange 
        { 
            get { return vScrollBar.LargeChange; } 
            set 
            { 
                if (enableLogging)
                    Console.WriteLine($"Setting LargeChange: {value} (was {vScrollBar.LargeChange})");
                
                // Ensure large change is at least 1
                vScrollBar.LargeChange = Math.Max(1, value);
                
                // Make sure current Value is still in range
                if (Value > Maximum - LargeChange + 1)
                    Value = Maximum - LargeChange + 1;
                    
                this.Invalidate();
            } 
        }
        
        /// <summary>
        /// Gets or sets the small change value (line size)
        /// </summary>
        public int SmallChange 
        { 
            get { return vScrollBar.SmallChange; } 
            set { vScrollBar.SmallChange = Math.Max(1, value); } 
        }
        
        /// <summary>
        /// Gets or sets the scrollbar thumb color
        /// </summary>
        public Color ThumbColor 
        { 
            get { return thumbColor; } 
            set 
            { 
                thumbColor = value;
                // Make hover/drag colors based on the main color
                hoverThumbColor = ControlPaint.Light(value, 0.2f);
                dragThumbColor = ControlPaint.Light(value, 0.3f);
                this.Invalidate(); 
            } 
        }
        
        /// <summary>
        /// Gets or sets the scrollbar track color
        /// </summary>
        public Color TrackColor 
        { 
            get { return trackColor; } 
            set 
            { 
                trackColor = value; 
                this.Invalidate(); 
            } 
        }
        
        /// <summary>
        /// Gets or sets the scrollbar border color
        /// </summary>
        public Color BorderColor 
        { 
            get { return borderColor; } 
            set 
            { 
                borderColor = value; 
                this.Invalidate(); 
            } 
        }
        
        /// <summary>
        /// Gets whether scrolling is needed (content exceeds viewport)
        /// </summary>
        public bool ScrollingNeeded
        {
            get
            {
                return Maximum > Minimum + LargeChange - 1;
            }
        }
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Occurs when the scroll position changes
        /// </summary>
        public event ScrollEventHandler Scroll;
        
        #endregion
        
        #region Constructor and Initialization
        
        /// <summary>
        /// Initializes a new instance of CustomScrollBar
        /// </summary>
        public CustomScrollBar()
        {
            InitializeComponent();
            this.Cursor = Cursors.Hand;
        }
        
        /// <summary>
        /// Initializes the component and configures initial properties
        /// </summary>
        private void InitializeComponent()
        {
            // Configure control styles for better performance
            this.SetStyle(ControlStyles.UserPaint | 
                         ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.OptimizedDoubleBuffer | 
                         ControlStyles.ResizeRedraw |
                         ControlStyles.Selectable, true);
            
            // Create and configure the underlying scrollbar for value tracking
            vScrollBar = new VScrollBar
            {
                Dock = DockStyle.Fill,
                Width = 12,
                Minimum = 0,
                Maximum = 100,
                LargeChange = 20,
                SmallChange = 5
            };
            
            // Connect scroll event
            vScrollBar.Scroll += VScrollBar_Scroll;
            
            // Only add the real scrollbar in non-custom drawing mode
            if (!isCustomDrawing)
            {
                Controls.Add(vScrollBar);
            }
            
            // Set default width and enable keyboard focus
            this.Width = 12;
            this.TabStop = true;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Updates scrollbar metrics based on content and viewport size
        /// </summary>
        /// <param name="totalItems">Total height of content in logical units</param>
        /// <param name="visibleItems">Visible height of viewport in logical units</param>
        /// <param name="currentValue">Current scroll position to maintain (or -1 to keep current)</param>
        public void UpdateMetrics(int totalItems, int visibleItems, int currentValue = -1)
        {
            if (enableLogging)
                Console.WriteLine($"UpdateMetrics: total={totalItems}, visible={visibleItems}, current={currentValue}");
            
            // Store current position as percentage to try to maintain scroll position
            double scrollPercent = 0;
            int oldScrollRange = Maximum - Minimum - LargeChange + 1;
            
            if (oldScrollRange > 0)
                scrollPercent = (double)Value / oldScrollRange;
            
            // Set parameters
            int effectiveMax = Math.Max(visibleItems + 1, totalItems);
            Maximum = effectiveMax;
            LargeChange = Math.Max(1, visibleItems);
            
            // Calculate new position
            int newPosition;
            
            if (currentValue >= 0)
            {
                // Explicit position provided
                newPosition = Math.Min(Maximum - LargeChange + 1, currentValue);
            }
            else
            {
                // Try to maintain relative scroll position
                int newScrollRange = Maximum - Minimum - LargeChange + 1;
                
                if (newScrollRange > 0)
                    newPosition = (int)(scrollPercent * newScrollRange);
                else
                    newPosition = 0;
            }
            
            // Set new position
            Value = newPosition;
            
            // Force repaint
            this.Invalidate();
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Calculates the thumb metrics based on current scroll state
        /// </summary>
        private (int height, int yPosition, Rectangle rect) CalculateThumbMetrics()
        {
            // Calculate ratio of visible content to total content
            int totalRange = Math.Max(1, vScrollBar.Maximum - vScrollBar.Minimum);
            int visibleRange = Math.Min(totalRange, vScrollBar.LargeChange);
            double visibleProportion = Math.Min(1.0, (double)visibleRange / totalRange);
            
            // Determine thumb height with a reasonable minimum
            int minThumbHeight = Math.Max(30, (int)(Height * 0.1));
            int thumbHeight = Math.Max(minThumbHeight, (int)(Height * visibleProportion));
            
            // For almost full visibility, make thumb larger but not 100%
            if (visibleProportion > 0.9)
                thumbHeight = (int)(Height * 0.9);
            
            // Calculate available track height for scrolling
            int trackHeight = Height - thumbHeight;
            
            // Determine effective scroll range (prevent division by zero)
            int effectiveRange = Math.Max(1, vScrollBar.Maximum - vScrollBar.Minimum - vScrollBar.LargeChange + 1);
            
            // Calculate thumb position
            int thumbY = (effectiveRange <= 0) ? 0 : 
                         (int)(((double)vScrollBar.Value / effectiveRange) * trackHeight);
            
            // Ensure thumb position is within the track boundaries
            thumbY = Math.Max(0, Math.Min(trackHeight, thumbY));
            
            // Define thumb rectangle for drawing and hit testing
            Rectangle thumbRect = new Rectangle(1, thumbY, Width - 2, thumbHeight);
            
            return (thumbHeight, thumbY, thumbRect);
        }
        
        /// <summary>
        /// Logs the current scrollbar state for debugging
        /// </summary>
        private void LogScrollState(string context)
        {
            if (!enableLogging) return;
            
            Console.WriteLine($"=== SCROLLBAR STATE ({context}) ===");
            Console.WriteLine($"Value: {Value}, Min: {Minimum}, Max: {Maximum}, LargeChange: {LargeChange}");
            
            var (thumbHeight, thumbY, thumbRect) = CalculateThumbMetrics();
            Console.WriteLine($"Thumb: Height={thumbHeight}, Y={thumbY}, Rect={thumbRect}");
            
            bool needsScrolling = Maximum > Minimum + LargeChange - 1;
            Console.WriteLine($"Needs Scrolling: {needsScrolling}");
            Console.WriteLine("========================");
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handles the Scroll event from the underlying scrollbar
        /// </summary>
   
private void VScrollBar_Scroll(object sender, ScrollEventArgs e)
{
    // Only forward the event if events are enabled
    if (EnableEvents && Scroll != null)
    {
        Scroll(this, e);
    }
    this.Invalidate();
}
        /// <summary>
        /// Handles mouse wheel for scrolling
        /// </summary>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            
            // Skip processing if no scrolling is needed
            if (!ScrollingNeeded)
                return;
            
            // Determine direction and amount based on scroll wheel delta
            int direction = (e.Delta > 0) ? -1 : 1;
            int scrollAmount = Math.Max(SmallChange, LargeChange / 8);
            
            // Calculate new value
            int newValue = Value + (direction * scrollAmount);
            
            // Constrain to valid range
            newValue = Math.Max(Minimum, Math.Min(Maximum - LargeChange + 1, newValue));
            
            // Update if changed
            if (Value != newValue)
            {
                Value = newValue;
                Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.ThumbPosition, Value));
                this.Invalidate();
            }
        }
        
        /// <summary>
        /// Handles mouse down event for dragging or page scrolling
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            this.Focus(); // Get focus for keyboard control
            
            if (!isCustomDrawing || !ScrollingNeeded)
                return;
            
            var (_, thumbY, thumbRect) = CalculateThumbMetrics();
            
            // Check if the thumb was clicked
            if (thumbRect.Contains(e.Location))
            {
                // Start drag operation
                dragStartOffset = e.Y - thumbY;
                isDragging = true;
            }
            else
            {
                // Page up/down when clicking above/below thumb
                int newValue;
                
                if (e.Y < thumbRect.Top) // Click above thumb - page up
                {
                    newValue = Value - LargeChange;
                }
                else // Click below thumb - page down
                {
                    newValue = Value + LargeChange;
                }
                
                // Apply with bounds checking
                newValue = Math.Max(Minimum, Math.Min(Maximum - LargeChange + 1, newValue));
                
                // Update if changed
                if (Value != newValue)
                {
                    Value = newValue;
                    Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.ThumbPosition, Value));
                    this.Invalidate();
                }
            }
        }
        
        /// <summary>
        /// Handles mouse move for dragging and hover effects
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            var (thumbHeight, _, thumbRect) = CalculateThumbMetrics();
            
            // Update hover state for visual feedback
            bool newHoverState = thumbRect.Contains(e.Location);
            if (newHoverState != isThumbHovered)
            {
                isThumbHovered = newHoverState;
                this.Invalidate();
            }
            
            // Handle dragging
            if (isDragging && ScrollingNeeded)
            {
                // Calculate available track height
                int trackHeight = Height - thumbHeight;
                
                // Determine effective scroll range
                int effectiveRange = Math.Max(1, Maximum - Minimum - LargeChange + 1);
                
                // Calculate new position based on drag with offset
                int newThumbY = e.Y - dragStartOffset;
                
                // Constrain to valid track range
                newThumbY = Math.Max(0, Math.Min(trackHeight, newThumbY));
                
                // Convert position to value with better precision
                double scrollPercent = (double)newThumbY / Math.Max(1, trackHeight);
                int newValue = Minimum + (int)(scrollPercent * effectiveRange);
                
                // Constrain to valid value range
                newValue = Math.Max(Minimum, Math.Min(Maximum - LargeChange + 1, newValue));
                
                // Update if changed
                if (Value != newValue)
                {
                    Value = newValue;
                    Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.ThumbTrack, Value));
                    this.Invalidate();
                }
            }
        }
        
        /// <summary>
        /// Handles mouse up event to end dragging
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            
            if (isDragging)
            {
                // End drag and send final notification
                Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.EndScroll, Value));
                isDragging = false;
                this.Invalidate();
            }
        }
        
        /// <summary>
        /// Handles mouse enter for cursor changes
        /// </summary>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.Cursor = Cursors.Hand;
        }
        
        /// <summary>
        /// Handles mouse leave to reset hover state
        /// </summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            
            if (isThumbHovered)
            {
                isThumbHovered = false;
                this.Invalidate();
            }
        }
        
        /// <summary>
        /// Handles keyboard navigation for scrolling
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Skip handling if no scrolling is needed
            if (!ScrollingNeeded)
                return base.ProcessCmdKey(ref msg, keyData);
            
            switch (keyData)
            {
                case Keys.Up:
                    Value = Value - SmallChange;
                    Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.SmallDecrement, Value));
                    return true;
                
                case Keys.Down:
                    Value = Value + SmallChange;
                    Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.SmallIncrement, Value));
                    return true;
                
                case Keys.PageUp:
                    Value = Value - LargeChange;
                    Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.LargeDecrement, Value));
                    return true;
                
                case Keys.PageDown:
                    Value = Value + LargeChange;
                    Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.LargeIncrement, Value));
                    return true;
                
                case Keys.Home:
                    Value = Minimum;
                    Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.First, Value));
                    return true;
                
                case Keys.End:
                    Value = Maximum - LargeChange + 1;
                    Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.Last, Value));
                    return true;
                
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
        
        /// <summary>
        /// Paints the custom scrollbar
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (!isCustomDrawing)
                return;
            
            // Output debug metrics if enabled
            if (enableLogging)
            {
                Console.WriteLine($"Scrollbar dimensions: {Width}x{Height}");
                Console.WriteLine($"Value: {Value}, Min: {Minimum}, Max: {Maximum}, LargeChange: {LargeChange}");
            }
            
            // Calculate thumb metrics
            var (thumbHeight, thumbY, thumbRect) = CalculateThumbMetrics();
            
            if (enableLogging)
                Console.WriteLine($"Thumb: Height={thumbHeight}, Y={thumbY}, Rect={thumbRect}");
            
            // Draw track background
            using (SolidBrush trackBrush = new SolidBrush(TrackColor))
            {
                e.Graphics.FillRectangle(trackBrush, ClientRectangle);
            }
            
            // Draw border
            using (Pen borderPen = new Pen(BorderColor))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
            
            // Only draw thumb if scrolling is needed
            bool needsScrolling = ScrollingNeeded;
            
            if (needsScrolling)
            {
                // Choose color based on interaction state
                Color currentThumbColor;
                
                if (isDragging)
                    currentThumbColor = dragThumbColor;
                else if (isThumbHovered)
                    currentThumbColor = hoverThumbColor;
                else
                    currentThumbColor = ThumbColor;
                
                // Draw thumb with anti-aliasing for smoother appearance
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                using (SolidBrush thumbBrush = new SolidBrush(currentThumbColor))
                {
                    // Create rounded rectangle for the thumb (if size allows)
                    int cornerRadius = 3;
                    
                    if (thumbRect.Width > cornerRadius * 2 && thumbRect.Height > cornerRadius * 2)
                    {
                        // Create rounded rectangle path
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            // Top left corner
                            path.AddArc(
                                thumbRect.X, 
                                thumbRect.Y, 
                                cornerRadius * 2, 
                                cornerRadius * 2, 
                                180, 
                                90
                            );
                            
                            // Top right corner
                            path.AddArc(
                                thumbRect.Right - cornerRadius * 2, 
                                thumbRect.Y, 
                                cornerRadius * 2, 
                                cornerRadius * 2, 
                                270, 
                                90
                            );
                            
                            // Bottom right corner
                            path.AddArc(
                                thumbRect.Right - cornerRadius * 2, 
                                thumbRect.Bottom - cornerRadius * 2, 
                                cornerRadius * 2, 
                                cornerRadius * 2, 
                                0, 
                                90
                            );
                            
                            // Bottom left corner
                            path.AddArc(
                                thumbRect.X, 
                                thumbRect.Bottom - cornerRadius * 2, 
                                cornerRadius * 2, 
                                cornerRadius * 2, 
                                90, 
                                90
                            );
                            
                            // Close the figure
                            path.CloseFigure();
                            
                            // Fill the path
                            e.Graphics.FillPath(thumbBrush, path);
                        }
                    }
                    else
                    {
                        // Fall back to regular rectangle for small thumbs
                        e.Graphics.FillRectangle(thumbBrush, thumbRect);
                    }
                }
                
                // Reset smoothing mode
                e.Graphics.SmoothingMode = SmoothingMode.Default;
            }
            else if (Visible)
            {
                // If no scrolling needed but visible, show inactive state
                using (SolidBrush inactiveBrush = new SolidBrush(Color.FromArgb(80, ThumbColor)))
                {
                    e.Graphics.FillRectangle(inactiveBrush, 1, 1, Width - 2, Height - 2);
                }
            }
        }
        
        #endregion
    }
}