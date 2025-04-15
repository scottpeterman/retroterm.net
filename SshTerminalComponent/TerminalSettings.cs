using System;
using System.ComponentModel;

namespace SshTerminalComponent
{
    /// <summary>
    /// Settings for the terminal appearance and behavior
    /// </summary>
    public class TerminalSettings : INotifyPropertyChanged
    {
        private int fontSize = 14;
        private TerminalTheme theme = TerminalTheme.Dark;
        private string fontFamily = "Consolas, monospace";
        private bool cursorBlink = true;
        private int scrollbackSize = 1000;
        
        /// <summary>
        /// Event raised when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// Event raised when settings are changed
        /// </summary>
        public event EventHandler SettingsChanged;
        
        /// <summary>
        /// Gets or sets the font size
        /// </summary>
        public int FontSize
        {
            get => fontSize;
            set
            {
                if (value < 8) value = 8;
                if (value > 24) value = 24;
                
                if (fontSize != value)
                {
                    fontSize = value;
                    OnPropertyChanged(nameof(FontSize));
                    OnSettingsChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the terminal theme
        /// </summary>
        public TerminalTheme Theme
        {
            get => theme;
            set
            {
                if (theme != value)
                {
                    theme = value;
                    OnPropertyChanged(nameof(Theme));
                    OnSettingsChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the font family
        /// </summary>
        public string FontFamily
        {
            get => fontFamily;
            set
            {
                if (fontFamily != value)
                {
                    fontFamily = value;
                    OnPropertyChanged(nameof(FontFamily));
                    OnSettingsChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets whether the cursor blinks
        /// </summary>
        public bool CursorBlink
        {
            get => cursorBlink;
            set
            {
                if (cursorBlink != value)
                {
                    cursorBlink = value;
                    OnPropertyChanged(nameof(CursorBlink));
                    OnSettingsChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the scrollback size (number of lines)
        /// </summary>
        public int ScrollbackSize
        {
            get => scrollbackSize;
            set
            {
                if (value < 100) value = 100;
                if (value > 10000) value = 10000;
                
                if (scrollbackSize != value)
                {
                    scrollbackSize = value;
                    OnPropertyChanged(nameof(ScrollbackSize));
                    OnSettingsChanged();
                }
            }
        }
        
        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        /// <summary>
        /// Raises the SettingsChanged event
        /// </summary>
        protected virtual void OnSettingsChanged()
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    /// <summary>
    /// Terminal theme options
    /// </summary>
    public enum TerminalTheme
    {
        /// <summary>
        /// Light theme (light background, dark text)
        /// </summary>
        Light,
        
        /// <summary>
        /// Dark theme (dark background, light text)
        /// </summary>
        Dark
    }
}