using System;
using System.Drawing;
using System.Windows.Forms;

namespace RetroTerm.NET.Forms
{
    public class ThemedTabControl : TabControl
    {
        private float _fontSize = 8.0f;
        public ThemedTabControl()
        {
            // These styles are critical for owner-drawing to work
            SetStyle(ControlStyles.UserPaint | 
                     ControlStyles.DoubleBuffer | 
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            
            // Set owner draw mode
            this.DrawMode = TabDrawMode.OwnerDrawFixed;
        }
        
        
protected override void OnPaint(PaintEventArgs e)
{
    // Use the parent's background color instead of this.BackColor
    Color backgroundColor = this.Parent != null ? this.Parent.BackColor : this.BackColor;
        Color borderColor = this.Parent != null ? this.Parent.ForeColor : this.BackColor;

            

    // Fill the ENTIRE control background with the parent's theme color
    using (SolidBrush brush = new SolidBrush(backgroundColor))
    {
        e.Graphics.FillRectangle(brush, this.ClientRectangle);
    }
    
    // Calculate and fill the tab strip area with the same color
    Rectangle stripRect = new Rectangle(
        0, 0, 
        this.Width, 
        this.ItemSize.Height + 4);
        
    using (SolidBrush brush = new SolidBrush(backgroundColor))
    {
        e.Graphics.FillRectangle(brush, stripRect);
    }
    
    // Draw each tab header
    for (int i = 0; i < this.TabCount; i++)
    {
        DrawItemEventArgs args = new DrawItemEventArgs(
            e.Graphics, 
            this.Font, 
            this.GetTabRect(i), 
            i,
            (i == this.SelectedIndex) ? DrawItemState.Selected : DrawItemState.Default,
            Color.White, // Text color - change as needed
            backgroundColor); // Use consistent background color
        
        this.OnDrawItem(args);
    }
    
    // Calculate content area rectangle
    if (this.SelectedIndex != -1)
    {
        TabPage selected = this.TabPages[this.SelectedIndex];
        Rectangle contentRect = new Rectangle(
            2, this.ItemSize.Height + 2,
            this.Width - 4, this.Height - this.ItemSize.Height - 4);
        
        // Use the selected tab's background color or fall back to parent color
        Color tabBackColor = selected.BackColor;
        
        // Fill content area with background color
        using (SolidBrush brush = new SolidBrush(tabBackColor))
        {
            e.Graphics.FillRectangle(brush, contentRect);
        }
        
        // Draw a border around the tab content
        using (Pen pen = new Pen(borderColor))
            {
                e.Graphics.DrawRectangle(pen, contentRect);
            }
    }
}

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }
    }
}