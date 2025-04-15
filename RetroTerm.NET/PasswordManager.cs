using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using YamlDotNet.Serialization;


namespace RetroTerm.NET
{
    /// <summary>
    /// Handles the encryption and decryption of sensitive information like passwords
    /// </summary>
    /// 
    /// <summary>
/// Handles the encryption and decryption of sensitive information like passwords
/// </summary>
public static class PasswordManager
{
    // Path to store the encryption key - can still store the key, but will require password to use it
    private static readonly string KeyPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RetroTerm.NET", "security", "encryption.key");

    // Flag to track if we're initialized
    public static bool _isInitialized = false;
    
    // Cache the key in memory during program execution
    private static byte[] _encryptionKey;

    /// <summary>
    /// Check if an encryption key has been set up
    /// </summary>
    public static bool IsKeySetup()
    {
        return File.Exists(KeyPath);
    }

    /// <summary>
    /// Initialize the encryption system - will always prompt for password
    /// </summary>
    /// <param name="form">Parent form for dialog</param>
    /// <returns>True if initialization was successful</returns>
    
    // Update Initialize method in PasswordManager.cs
    
    public static bool Initialize(Form parentForm = null)
{
    try
    {
        Console.WriteLine("PasswordManager.Initialize called");
        
        // If we're already initialized in this session, no need to prompt again
        if (_isInitialized && _encryptionKey != null && _encryptionKey.Length > 0)
        {
            Console.WriteLine("Already initialized, returning true");
            return true;
        }

        // Check if there's a key file
        bool isFirstRun = !IsKeySetup();
        Console.WriteLine($"Is first run: {isFirstRun}");
        
        // Always prompt for the password
        if (parentForm != null)
        {
            Console.WriteLine("Parent form is not null, creating dialog");
            MasterPasswordDialog dialog = null;
            
            try
            {
                dialog = new MasterPasswordDialog(isFirstRun);
                Console.WriteLine("Dialog created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating dialog: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to be caught by outer try-catch
            }
        using (dialog)
{
    Console.WriteLine("Showing dialog");
    DialogResult result = DialogResult.None;
    
    try
    {
        result = dialog.ShowDialog(parentForm);
        Console.WriteLine($"Dialog result: {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error showing dialog: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        throw; // Re-throw to be caught by outer try-catch
    }
    
    if (result == DialogResult.OK)
    {
        // Process the entered password
        string masterPassword = dialog.Password;
        if (!string.IsNullOrEmpty(masterPassword))
        {
            // Derive a key from the password
            _encryptionKey = DeriveKeyFromPassword(masterPassword);
            
            // If this is first run, save the key
            if (isFirstRun)
            {
                SaveEncryptionKey(_encryptionKey);
            }
            // Otherwise, verify the key against the stored one
            else if (!VerifyKey(_encryptionKey))
            {
                MessageBox.Show(
                    parentForm,
                    "The password you entered is incorrect.",
                    "Authentication Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
            
            // Mark as initialized
            _isInitialized = true;
            return true;
        }
        return false; // Password was empty despite OK dialog result
    }
    else if (result == DialogResult.Retry)
    {
        // Handle reset action by calling ResetEncryption
        return ResetEncryption(parentForm);
    }
    
    // User canceled - explicitly return false
    Console.WriteLine("User canceled or dialog did not return OK/Retry");
    return false;
}

        }
        
        // No parent form - can't prompt
        Console.WriteLine("Parent form is null, returning false");
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in PasswordManager.Initialize: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        
        if (parentForm != null)
        {
            MessageBox.Show(
                parentForm,
                $"Error initializing encryption: {ex.Message}",
                "Encryption Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        return false;
    }
}

    // Add to PasswordManager.cs
    // Add to PasswordManager.cs
    public static bool ResetEncryption(Form parentForm = null)
{
    try
    {
        // Check if encryption key exists
        if (!File.Exists(KeyPath))
        {
            return false; // Nothing to reset
        }

        // Delete the existing key file
        try 
        {
            File.Delete(KeyPath);
            Console.WriteLine("Existing encryption key file deleted");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting key file: {ex.Message}");
            // Continue anyway - we'll create a new one
        }

        // Now prompt for new password (always in setup mode)
        if (parentForm != null)
        {
            using (var dialog = new MasterPasswordDialog(true)) // true = setup mode
            {
                dialog.TopMost = true;
                dialog.Text = "Create New Master Password";
                
                DialogResult result = dialog.ShowDialog(parentForm);
                if (result == DialogResult.OK)
                {
                    string newPassword = dialog.Password;
                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        // Create new encryption key
                        _encryptionKey = DeriveKeyFromPassword(newPassword);
                        SaveEncryptionKey(_encryptionKey);
                        
                        // Mark as initialized
                        _isInitialized = true;
                        
                        MessageBox.Show(
                            parentForm,
                            "Master password has been reset. Note that previously encrypted passwords will no longer be accessible and will need to be re-entered.",
                            "Password Reset Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        
                        return true;
                    }
                }
                
                // User canceled or provided invalid input
                // In the ResetEncryption method when a user cancels
MessageBox.Show(
    parentForm,
    "Password reset was canceled or invalid. The application will now exit.",
    "Reset Canceled",
    MessageBoxButtons.OK,
    MessageBoxIcon.Warning);

// Force application shutdown more directly
parentForm.BeginInvoke(new Action(() => {
    // First try to close the form gracefully
    if (parentForm != null)
    {
        parentForm.Close();
    }
    
    // Then force application exit
    Environment.Exit(0);
}));

// Return false, though execution should not reach this point
return false;
            }
        }
        
        // No parent form - can't prompt
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error resetting encryption: {ex.Message}");
        if (parentForm != null)
        {
            MessageBox.Show(
                parentForm,
                $"Error resetting encryption: {ex.Message}\n\nThe application will now exit.",
                "Reset Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            
            // Force application shutdown on error
            Application.Exit();
        }
        return false;
    }
}

    /// <summary>
    /// Verify if the provided key matches the stored one
    /// </summary>
    private static bool VerifyKey(byte[] key)
    {
        try
        {
            // This is a simple approach for verification:
            // 1. We'll load the stored key
            // 2. Hash it and the provided key
            // 3. Compare the hashes
            
            byte[] storedKey = File.ReadAllBytes(KeyPath);
            
            // Use a hash function to compare (to avoid timing attacks)
            using (var sha = SHA256.Create())
            {
                byte[] storedHash = sha.ComputeHash(storedKey);
                byte[] providedHash = sha.ComputeHash(key);
                
                // Compare the hashes
                for (int i = 0; i < storedHash.Length; i++)
                {
                    if (storedHash[i] != providedHash[i])
                        return false;
                }
                
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error verifying key: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Derive a cryptographic key from a user password
    /// </summary>
    private static byte[] DeriveKeyFromPassword(string password)
    {
        // Use a key derivation function with a salt and multiple iterations
        // This makes brute-force attacks much more difficult
        using (var deriveBytes = new Rfc2898DeriveBytes(
            password,
            Encoding.UTF8.GetBytes("RetroTerm.NET_Salt_Value"),  // Fixed salt
            10000))  // Number of iterations (higher = more secure but slower)
        {
            return deriveBytes.GetBytes(32);  // 256-bit key
        }
    }

    /// <summary>
    /// Save the encryption key to a file
    /// </summary>
    private static void SaveEncryptionKey(byte[] key)
    {
        try
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(KeyPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write the key to file
            File.WriteAllBytes(KeyPath, key);

            // Set file as hidden
            File.SetAttributes(KeyPath, FileAttributes.Hidden);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving encryption key: {ex.Message}");
            throw;  // Re-throw to let caller handle
        }
    }

    /// <summary>
    /// Encrypt a password string
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Encrypted password string (Base64)</returns>
    public static string EncryptPassword(string password)
    {
        // Handle null or empty password
        if (string.IsNullOrEmpty(password))
            return password;

        // Ensure we have an encryption key
        if (!_isInitialized || _encryptionKey == null || _encryptionKey.Length == 0)
        {
            // If not initialized, return the original password
            // We'll handle encryption during migration
            return password;
        }

        try
        {
            // Create AES encryption provider
            using (Aes aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.GenerateIV();  // Generate a new IV for each encryption

                // Create an encryptor
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                // Create memory stream for output
                using (MemoryStream ms = new MemoryStream())
                {
                    // Store the IV first so we can retrieve it for decryption
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    // Create crypto stream and writer
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(password);
                        }
                    }

                    // Convert to Base64 string for storage
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error encrypting password: {ex.Message}");
            return password;  // Return original on error
        }
    }

    /// <summary>
    /// Decrypt an encrypted password
    /// </summary>
    /// <param name="encryptedPassword">Encrypted password string (Base64)</param>
    /// <returns>Decrypted password</returns>
    public static string DecryptPassword(string encryptedPassword)
    {
        // Handle null or empty password
        if (string.IsNullOrEmpty(encryptedPassword))
            return encryptedPassword;

        // Check if this appears to be an encrypted password
        if (!IsEncrypted(encryptedPassword))
            return encryptedPassword;  // Return as-is if not encrypted

        // Ensure we have an encryption key
        if (!_isInitialized || _encryptionKey == null || _encryptionKey.Length == 0)
        {
            // If not initialized, return the original encrypted password
            return encryptedPassword;
        }

        try
        {
            // Convert from Base64
            byte[] cipherBytes = Convert.FromBase64String(encryptedPassword);

            // Create AES provider
            using (Aes aes = Aes.Create())
            {
                // Get the IV size
                int ivSize = aes.BlockSize / 8;

                // Check if we have enough bytes for IV + data
                if (cipherBytes.Length <= ivSize)
                    return encryptedPassword;  // Invalid size, return as-is

                // Extract the IV from the beginning of the cipher text
                byte[] iv = new byte[ivSize];
                Array.Copy(cipherBytes, 0, iv, 0, ivSize);
                aes.IV = iv;
                aes.Key = _encryptionKey;

                // Create a decryptor
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                // Create memory stream with cipher bytes (skipping IV)
                using (MemoryStream ms = new MemoryStream(cipherBytes, ivSize, cipherBytes.Length - ivSize))
                {
                    // Create crypto stream and reader
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error decrypting password: {ex.Message}");
            return encryptedPassword;  // Return original on error
        }
    }

    /// <summary>
    /// Check if a string appears to be encrypted
    /// </summary>
    /// <param name="text">Text to check</param>
    /// <returns>True if the text appears to be encrypted</returns>
    public static bool IsEncrypted(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // Try to decode as Base64 - if it fails, it's not encrypted
        try
        {
            Convert.FromBase64String(text);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Migrate unencrypted passwords in a session file to encrypted format
    /// </summary>
    /// <param name="filePath">Path to the sessions YAML file</param>
    /// <returns>True if migration was successful</returns>
    public static bool MigrateSessionPasswords(string filePath)
    {
        try
        {
            // Ensure encryption is initialized
            if (!_isInitialized || _encryptionKey == null || _encryptionKey.Length == 0)
            {
                Console.WriteLine("Encryption not initialized, cannot migrate passwords");
                return false;
            }
            
            // Check if the file exists
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Session file not found: {filePath}");
                return false;
            }
            
            // Read the session file
            string yaml = File.ReadAllText(filePath);
            
            // Parse YAML using YamlDotNet
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.NullNamingConvention.Instance)
                .Build();
            
            var folders = deserializer.Deserialize<List<SessionFolder>>(yaml);
            
            // Flag to track if we made any changes
            bool madeChanges = false;
            
            // Process each session in each folder
            foreach (var folder in folders)
            {
                if (folder.Sessions != null)
                {
                    foreach (var session in folder.Sessions)
                    {
                        // Check if password is unencrypted
                        if (!string.IsNullOrEmpty(session.Password) && !IsEncrypted(session.Password))
                        {
                            // Encrypt the password
                            string originalPassword = session.Password;
                            session.Password = EncryptPassword(originalPassword);
                            Console.WriteLine($"Encrypted password for session: {session.DisplayName ?? session.Host}");
                            madeChanges = true;
                        }
                    }
                }
            }
            
            // If we made changes, save the file
            if (madeChanges)
            {
                Console.WriteLine($"Saving {filePath} with encrypted passwords");
                
                // Make a backup of the original file first
                string backupPath = filePath + ".bak";
                File.Copy(filePath, backupPath, true);
                
                // Serialize back to YAML
                var serializer = new YamlDotNet.Serialization.SerializerBuilder()
                    .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.NullNamingConvention.Instance)
                    .Build();
                
                string updatedYaml = serializer.Serialize(folders);
                
                // Save to file
                File.WriteAllText(filePath, updatedYaml);
                
                Console.WriteLine($"Successfully migrated {folders.Count} folders with encrypted passwords");
            }
            else
            {
                Console.WriteLine("No unencrypted passwords found, no changes made");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error migrating session passwords: {ex.Message}");
            return false;
        }
    }
}

}