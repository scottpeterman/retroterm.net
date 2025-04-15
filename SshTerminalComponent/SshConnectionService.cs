using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;
using SshTerminalComponent; // This might be redundant if you're already in this namespace


namespace SshTerminalComponent
{
    /// <summary>
    /// Service for managing SSH connections
    /// </summary>
    internal class SshConnectionService : IDisposable
    {
        // Add tab ID property
        private readonly string parentTabId;
        
        private SshClient sshClient;
        private ShellStream shellStream;
        private bool disposed;
        private CancellationTokenSource readCancellationTokenSource;
        private Task shellReadTask;

        /// <summary>
        /// Gets whether the SSH client is connected
        /// </summary>
        public bool IsConnected => sshClient?.IsConnected ?? false;

        /// <summary>
        /// Event raised when data is received from the SSH server
        /// </summary>
        public event EventHandler<SshTerminalComponent.SshDataEventArgs> DataReceived;

        /// <summary>
        /// Event raised when connection state changes
        /// </summary>
        public event EventHandler<SshConnectionEventArgs> ConnectionStateChanged;
        
        /// <summary>
        /// Initializes a new instance of the SshConnectionService with the specified tab ID
        /// </summary>
        /// <param name="tabId">The ID of the parent tab</param>
        public SshConnectionService(string tabId = null)
        {
            parentTabId = tabId ?? "unknown";
            Log($"Created new SSH connection service for tab {parentTabId}");
        }

        /// <summary>
        /// Connects to an SSH server using password authentication
        /// </summary>
        public void ConnectWithPassword(string host, int port, string username, string password)
        {
            ThrowIfDisposed();
            Disconnect();
    
            try
            {
                Log($"Connecting to SSH server: {host}:{port} with username {username}");
                
                // Set connection info with appropriate timeout
                var connectionInfo = new ConnectionInfo(host, port, username,
                    new PasswordAuthenticationMethod(username, password))
                {
                    Timeout = TimeSpan.FromSeconds(10),
                    RetryAttempts = 3
                };
                
                sshClient = new SshClient(connectionInfo);
                
                // Set keepalive interval to prevent disconnections
                sshClient.KeepAliveInterval = TimeSpan.FromSeconds(5);
                
                try
                {
                    Log("Attempting SSH connection...");
                    sshClient.Connect();
                    Log($"SSH connection status: {sshClient.IsConnected}");
                }
                catch (SshAuthenticationException authEx)
                {
                    Log($"SSH authentication failed: {authEx.Message}");
                    throw new CustomSshConnectionException("Authentication failed. Please check your username and password.", authEx);
                }
                catch (SshConnectionException connEx)
                {
                    Log($"SSH connection failed: {connEx.Message}");
                    throw new SshConnectionException($"Connection to {host}:{port} failed. Please check server availability and network connectivity.", connEx.DisconnectReason);
                }
                catch (Exception ex)
                {
                    Log($"Unexpected SSH error: {ex.GetType().Name}: {ex.Message}");
                    throw new CustomSshConnectionException($"Failed to connect to {host}:{port}", ex);
                }
            }
            catch (Exception ex)
            {
                Log($"Connection error: {ex.GetType().Name}: {ex.Message}");
                throw;
            }   
        }

        /// <summary>
        /// Raises the ConnectionStateChanged event
        /// </summary>
        protected virtual void OnConnectionStateChanged(bool isConnected, string host = "", int port = 0, string username = "")
        {
            ConnectionStateChanged?.Invoke(this, new SshConnectionEventArgs(isConnected, host, port, username));
        }
        
        /// <summary>
        /// Connects to an SSH server using private key authentication
        /// </summary>
        public void ConnectWithPrivateKey(string host, int port, string username, string privateKeyFile, string passphrase = null)
        {
            ThrowIfDisposed();
            Disconnect();
            
            try 
            {
                Log($"Connecting to SSH server: {host}:{port} with username {username} using private key");
                
                // Create private key file instance
                PrivateKeyFile keyFile;
                try
                {
                    keyFile = string.IsNullOrEmpty(passphrase) 
                        ? new PrivateKeyFile(privateKeyFile)
                        : new PrivateKeyFile(privateKeyFile, passphrase);
                        
                    Log("Private key file loaded successfully");
                }
                catch (Exception ex)
                {
                    Log($"Error loading private key file: {ex.Message}");
                    throw new CustomSshConnectionException("Failed to load private key file. Make sure the file exists and the passphrase is correct.", ex);
                }
                
                // Set connection info with appropriate timeout
                var connectionInfo = new ConnectionInfo(host, port, username,
                    new PrivateKeyAuthenticationMethod(username, keyFile))
                {
                    Timeout = TimeSpan.FromSeconds(10),
                    RetryAttempts = 3
                };
                
                sshClient = new SshClient(connectionInfo);
                
                // Set keepalive interval to prevent disconnections
                sshClient.KeepAliveInterval = TimeSpan.FromSeconds(30);
                
                try
                {
                    Log("Attempting SSH connection...");
                    sshClient.Connect();
                    sshClient.KeepAliveInterval = TimeSpan.FromSeconds(5); // Check every 5 seconds

                    Log($"SSH connection status: {sshClient.IsConnected}");
                }
                catch (SshAuthenticationException authEx)
                {
                    Log($"SSH authentication failed: {authEx.Message}");
                    throw new CustomSshConnectionException("Authentication failed. Please check your key file and passphrase.", authEx);
                }
                catch (SshConnectionException connEx)
                {
                    Log($"SSH connection failed: {connEx.Message}");
                    throw new CustomSshConnectionException($"Connection to {host}:{port} failed. Please check server availability and network connectivity.", connEx);
                }
                catch (Exception ex)
                {
                    Log($"Unexpected SSH error: {ex.GetType().Name}: {ex.Message}");
                    throw new CustomSshConnectionException($"Failed to connect to {host}:{port}", ex);
                }
            }
            catch (Exception ex)
            {
                Log($"Connection error: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a shell stream with the specified dimensions
        /// </summary>
        public void CreateShell(uint columns, uint rows)
        {
            ThrowIfDisposed();
            
            if (sshClient == null || !sshClient.IsConnected)
                throw new InvalidOperationException("SSH client is not connected");
            
            try
            {
                Log($"Creating shell with dimensions: {columns}x{rows}");
                
                var terminalModes = new Dictionary<TerminalModes, uint>
                {
                    { TerminalModes.ECHO, 1 },
                    { TerminalModes.ICANON, 1 },
                    { TerminalModes.ISIG, 1 }
                };
                
                shellStream = sshClient.CreateShellStream(
                    "xterm-256color", columns, rows, 800, 600, 4096, terminalModes);
                
                Log("Shell stream created successfully");
                
                // Create a new cancellation token source for the read task
                readCancellationTokenSource = new CancellationTokenSource();
                
                // Start reading data from the shell stream
                Log("Starting shell stream read task");
                shellReadTask = ReadShellStreamAsync(readCancellationTokenSource.Token);
                Log("Shell stream read task started");
            }
            catch (Exception ex)
            {
                Log($"Error creating shell: {ex.GetType().Name}: {ex.Message}");
                throw new CustomSshConnectionException("Failed to create shell", ex);
            }
        }

        /// <summary>
        /// Resizes the shell to the specified dimensions
        /// </summary>
        public bool ResizeShell(uint columns, uint rows)
        {
            ThrowIfDisposed();
            
            if (shellStream == null)
                return false;
            
            Log($"Resizing shell to {columns}x{rows}");
            return shellStream.SendWindowChangeRequest(columns, rows);
        }

        /// <summary>
        /// Sends data to the shell
        /// </summary>
        public void SendData(string data)
        {
            ThrowIfDisposed();
            
            if (shellStream == null || !sshClient?.IsConnected == true)
                return;
            
            try
            {
                // Log data being sent (but be mindful of sensitive info)
                if (data.Length < 3) 
                {
                    Log($"Sending data: {data.Replace("\r", "\\r").Replace("\n", "\\n")}");
                }
                else 
                {
                    Log($"Sending data of length {data.Length}");
                }
                
                shellStream.Write(data);
                shellStream.Flush();
            }
            catch (ObjectDisposedException ex)
            {
                Log($"Error sending data: {ex.Message}");
                
                // This indicates that the connection was closed by the server
                Log("Detected server disconnection via disposed stream");
                
                // Notify about disconnection
                DataReceived?.Invoke(this, new SshDataEventArgs("\r\n\x1B[1;3;33mConnection closed by remote server\x1B[0m\r\n"));
                
                // Force a disconnect to clean up resources and notify the UI
                Disconnect();
            }
            catch (Exception ex)
            {
                Log($"Error sending data: {ex.Message}");
                
                // Check if this is a connection-related exception
                if (ex.Message.Contains("closed") || ex.Message.Contains("termina") || 
                    ex.Message.Contains("connect") || ex.Message.Contains("reset"))
                {
                    Log("Detected potential connection issue: " + ex.Message);
                    
                    // Notify about disconnection
                    DataReceived?.Invoke(this, new SshDataEventArgs("\r\n\x1B[1;3;33mConnection closed by remote server\x1B[0m\r\n"));
                    
                    // Force a disconnect to clean up resources
                    Disconnect();
                }
            }
        }

        /// <summary>
        /// Disconnects from the SSH server and cleans up resources
        /// </summary>
        public void Disconnect()
        {
            if (disposed) return;
            
            Log("Disconnecting SSH session");
            
            // Store current connection state before disconnecting
            bool wasConnected = sshClient?.IsConnected == true;
            
            // Cancel the read task
            DisposeCancellationToken();
            
            // Close and dispose shell stream
            CloseShellStream();
            
            // Disconnect SSH client
            DisconnectSshClient();
            
            Log("SSH session disconnected");
            
            // Raise connection state changed event if we were previously connected
            if (wasConnected)
            {
                Log("Raising connection state changed event (disconnected)");
                OnConnectionStateChanged(false);
            }
        }

        /// <summary>
        /// Cancels and disposes the read task cancellation token
        /// </summary>
        private void DisposeCancellationToken()
        {
            try
            {
                if (readCancellationTokenSource != null)
                {
                    Log("Cancelling shell read task");
                    readCancellationTokenSource.Cancel();
                    readCancellationTokenSource.Dispose();
                    readCancellationTokenSource = null;
                }
            }
            catch (Exception ex)
            {
                Log($"Error cancelling read task: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes and disposes the shell stream
        /// </summary>
        private void CloseShellStream()
        {
            try
            {
                if (shellStream != null)
                {
                    Log("Closing shell stream");
                    shellStream.Close();
                    shellStream.Dispose();
                    shellStream = null;
                }
            }
            catch (Exception ex)
            {
                Log($"Error closing shell stream: {ex.Message}");
            }
        }

        /// <summary>
        /// Disconnects the SSH client
        /// </summary>
        private void DisconnectSshClient()
        {
            try
            {
                if (sshClient != null && sshClient.IsConnected)
                {
                    Log("Disconnecting SSH client");
                    sshClient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Log($"Error disconnecting SSH client: {ex.Message}");
            }
        }

        /// <summary>
        /// Continuously reads data from the shell stream asynchronously
        /// </summary>
        private async Task ReadShellStreamAsync(CancellationToken cancellationToken)
        {
            try
            {
                byte[] buffer = new byte[4096];
                Log("ReadShellStreamAsync started");
                
                // Add a counter for connection checking
                int noDataAvailableCount = 0;
                
                while (!cancellationToken.IsCancellationRequested && 
                      sshClient != null && 
                      sshClient.IsConnected && 
                      shellStream != null)
                {
                    // Check if data is available
                    if (shellStream.DataAvailable)
                    {
                        noDataAvailableCount = 0; // Reset counter
                        int bytesRead = shellStream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            
                            // Add logging with the first few characters (avoid logging too much)
                            string logSample = data.Length > 20 ? data.Substring(0, 20) + "..." : data;
                            Log($"SSH data received ({bytesRead} bytes): {logSample.Replace("\r", "\\r").Replace("\n", "\\n")}");
                            
                            // Raise the DataReceived event
                            if (DataReceived != null)
                            {
                                Log($"Invoking DataReceived event with {data.Length} bytes");
                                DataReceived.Invoke(this, new SshDataEventArgs(data));
                            }
                            else
                            {
                                Log("WARNING: DataReceived event has no subscribers");
                            }
                        }
                    }
                    else
                    {
                        // No data available - check if the connection is still usable
                        try
                        {
                            if (!shellStream.CanRead || !sshClient.IsConnected)
                            {
                                Log("Shell stream is no longer readable or SSH client disconnected");
                                break;
                            }
                            
                            // Increment counter when no data is available
                            noDataAvailableCount++;
                            
                            // After several checks with no data, ping the connection
                            if (noDataAvailableCount > 50) // about 500ms
                            {
                                // Try to detect a closed connection by checking the session status
                                if (!sshClient.IsConnected)
                                {
                                    Log("SSH client reports disconnected state");
                                    break;
                                }
                                
                                // Reset counter
                                noDataAvailableCount = 0;
                            }
                        }
                        catch (Exception connEx)
                        {
                            // Exception likely means connection is closed
                            Log($"Connection check error: {connEx.Message}");
                            break;
                        }
                        
                        // Small delay to avoid high CPU usage
                        await Task.Delay(10, cancellationToken);
                    }
                }
                
                // If we get here without cancellation, the connection likely closed
                if (!cancellationToken.IsCancellationRequested && sshClient != null)
                {
                    Log("Shell stream ended - notifying about disconnection");
                    
                    // Send a disconnection notification through the DataReceived event
                    DataReceived?.Invoke(this, new SshDataEventArgs("\r\n\x1B[1;3;33mConnection closed by remote server\x1B[0m\r\n"));
                    
                    // Force a disconnect to clean up resources
                    Disconnect();
                }
                
                Log("ReadShellStreamAsync exited loop: " + 
                    (cancellationToken.IsCancellationRequested ? "Cancellation requested" : 
                    sshClient == null ? "SSH client is null" : 
                    !sshClient.IsConnected ? "SSH client disconnected" : 
                    shellStream == null ? "Shell stream is null" : "Unknown reason"));
            }
            catch (OperationCanceledException)
            {
                Log("ReadShellStreamAsync: Operation cancelled");
            }
            catch (ObjectDisposedException)
            {
                Log("ReadShellStreamAsync: ObjectDisposedException");
            }
            catch (Exception ex)
            {
                if (!disposed)
                {
                    Log($"Error reading from SSH: {ex.GetType().Name}: {ex.Message}");
                    
                    // Send error notification and disconnect if there was an exception
                    DataReceived?.Invoke(this, new SshDataEventArgs($"\r\n\x1B[1;3;31mConnection error: {ex.Message}\x1B[0m\r\n"));
                    Disconnect();
                }
            }
        }

        /// <summary>
        /// Logs a message with the tab ID prefix
        /// </summary>
        private void Log(string message)
        {
            Console.WriteLine($"[Tab {parentTabId}] [SSH] {message}");
        }

        /// <summary>
        /// Disposes of resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            
            if (disposing)
            {
                try
                {
                    Log("Disposing SshConnectionService");
                    Disconnect();
                    
                    if (sshClient != null)
                    {
                        sshClient.Dispose();
                        sshClient = null;
                    }
                    
                    Log("SshConnectionService disposed");
                }
                catch (Exception ex)
                {
                    Log($"Error during dispose: {ex.Message}");
                }
            }
            
            disposed = true;
        }
        
        /// <summary>
        /// Throws an ObjectDisposedException if this object has been disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SshConnectionService));
            }
        }
    }
}