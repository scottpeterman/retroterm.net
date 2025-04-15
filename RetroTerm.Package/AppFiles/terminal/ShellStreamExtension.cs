using System.Reflection;
using Renci.SshNet;

namespace SshTerminalComponent
{
    /// <summary>
    /// Extension methods for ShellStream to add window resize functionality
    /// </summary>
    public static class ShellStreamExtension
    {
        /// <summary>
        /// Sends a window change request to the SSH server to resize the terminal.
        /// This uses reflection to access the private channel field and invoke the SendWindowChangeRequest method.
        /// </summary>
        /// <param name="stream">The ShellStream to resize</param>
        /// <param name="cols">Terminal width in columns</param>
        /// <param name="rows">Terminal height in rows</param>
        /// <param name="width">Terminal width in pixels (optional)</param>
        /// <param name="height">Terminal height in pixels (optional)</param>
        /// <returns>True if the request was successful, false otherwise</returns>
        public static bool SendWindowChangeRequest(this ShellStream stream, uint cols, uint rows, uint width = 0, uint height = 0)
        {
            try
            {
                // Get the private _channel field from ShellStream
                var channelField = typeof(ShellStream).GetField("_channel", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (channelField == null)
                {
                    // Field name might be different in some versions
                    // Try alternative naming patterns
                    string[] possibleFieldNames = { "_channel", "channel", "_channelSession", "channelSession" };
                    
                    foreach (var fieldName in possibleFieldNames)
                    {
                        channelField = typeof(ShellStream).GetField(fieldName, 
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        
                        if (channelField != null)
                            break;
                    }
                    
                    if (channelField == null)
                        return false; // Could not find the channel field
                }
                
                // Get the channel object instance from the ShellStream
                var channel = channelField.GetValue(stream);
                if (channel == null)
                    return false;
                
                // Try to find a method to send window change requests
                // Implementations may vary across SSH.NET versions
                string[] possibleMethodNames = { 
                    "SendWindowChangeRequest",
                    "SendWindowChange", 
                    "SendChannelWindowChangeRequest",
                    "SendTerminalWindowChangeRequest"
                };
                
                MethodInfo? methodInfo = null;
                
                foreach (var methodName in possibleMethodNames)
                {
                    // Try both public and non-public methods
                    methodInfo = channel.GetType().GetMethod(methodName, 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (methodInfo != null)
                        break;
                }
                
                if (methodInfo == null)
                    return false; // Could not find an appropriate method
                
                // Invoke the method
                methodInfo.Invoke(channel, new object[] { cols, rows, width, height });
                return true;
            }
            catch
            {
                // If any errors occur, just return false
                return false;
            }
        }
    }
}