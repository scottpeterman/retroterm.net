using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SshTerminalComponent
{
    /// <summary>
    /// Represents a complete terminal color theme
    /// </summary>
    public class TerminalThemeColors
    {
        [JsonPropertyName("background")]
        public string Background { get; set; } = "#0000AA";

        [JsonPropertyName("foreground")]
        public string Foreground { get; set; } = "#FFFFFF";

        [JsonPropertyName("cursor")]
        public string Cursor { get; set; } = "#FFFFFF";

        [JsonPropertyName("scrollbarBackground")]
        public string ScrollbarBackground { get; set; } = "#0000AA";

        [JsonPropertyName("scrollbarThumb")]
        public string ScrollbarThumb { get; set; } = "#FFFFFF";

        // Standard ANSI colors
        [JsonPropertyName("black")]
        public string Black { get; set; } = "#000000";

        [JsonPropertyName("red")]
        public string Red { get; set; } = "#AA0000";

        [JsonPropertyName("green")]
        public string Green { get; set; } = "#00AA00";

        [JsonPropertyName("yellow")]
        public string Yellow { get; set; } = "#AA5500";

        [JsonPropertyName("blue")]
        public string Blue { get; set; } = "#0000AA";

        [JsonPropertyName("magenta")]
        public string Magenta { get; set; } = "#AA00AA";

        [JsonPropertyName("cyan")]
        public string Cyan { get; set; } = "#00AAAA";

        [JsonPropertyName("white")]
        public string White { get; set; } = "#AAAAAA";

        // Bright ANSI colors
        [JsonPropertyName("brightBlack")]
        public string BrightBlack { get; set; } = "#555555";

        [JsonPropertyName("brightRed")]
        public string BrightRed { get; set; } = "#FF5555";

        [JsonPropertyName("brightGreen")]
        public string BrightGreen { get; set; } = "#55FF55";

        [JsonPropertyName("brightYellow")]
        public string BrightYellow { get; set; } = "#FFFF55";

        [JsonPropertyName("brightBlue")]
        public string BrightBlue { get; set; } = "#5555FF";

        [JsonPropertyName("brightMagenta")]
        public string BrightMagenta { get; set; } = "#FF55FF";

        [JsonPropertyName("brightCyan")]
        public string BrightCyan { get; set; } = "#55FFFF";

        [JsonPropertyName("brightWhite")]
        public string BrightWhite { get; set; } = "#FFFFFF";

        // Convert to dictionary for xterm.js
        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                ["background"] = Background,
                ["foreground"] = Foreground,
                ["cursor"] = Cursor,
                ["scrollbarBackground"] = ScrollbarBackground,
                ["scrollbarThumb"] = ScrollbarThumb,
                ["black"] = Black,
                ["red"] = Red,
                ["green"] = Green,
                ["yellow"] = Yellow,
                ["blue"] = Blue,
                ["magenta"] = Magenta,
                ["cyan"] = Cyan,
                ["white"] = White,
                ["brightBlack"] = BrightBlack,
                ["brightRed"] = BrightRed,
                ["brightGreen"] = BrightGreen,
                ["brightYellow"] = BrightYellow,
                ["brightBlue"] = BrightBlue,
                ["brightMagenta"] = BrightMagenta,
                ["brightCyan"] = BrightCyan,
                ["brightWhite"] = BrightWhite
            };
        }
    }
}