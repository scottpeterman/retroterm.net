# MSIX Package Signing Guide

This guide outlines the process for creating a self-signed certificate and using it to sign an MSIX package. This is particularly useful when the Publisher in your MSIX manifest doesn't match your existing certificate.

## The Problem

When signing an MSIX package, you may encounter this error:

```
SignTool Error: An unexpected internal error has occurred.
Error information: "Error: SignerSign() failed." (-2147024885/0x8007000b)
```

This typically happens when the certificate's subject name doesn't match the Publisher identity in your app manifest.

## Solution

### 1. Create a new self-signed certificate

This PowerShell command creates a certificate where the subject name matches your app manifest's Publisher attribute:

```powershell
New-SelfSignedCertificate -Type Custom -Subject "CN=Scott Peterman" -KeyUsage DigitalSignature -FriendlyName "RetroTerm App Signing" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
```

When successful, this will output a thumbprint:

```
Thumbprint                                Subject
----------                                -------
FE1E727BAC26859D4F783DB9B545AF27C59F469F  CN=Scott Peterman
```

### 2. Export the certificate to a PFX file

Use the thumbprint from the previous step to export the certificate with its private key:

```powershell
$password = ConvertTo-SecureString -String "P@ssword123!" -Force -AsPlainText
Export-PfxCertificate -cert "Cert:\CurrentUser\My\FE1E727BAC26859D4F783DB9B545AF27C59F469F" -FilePath "C:\Users\speterman\Desktop\retroterm_new.pfx" -Password $password
```

### 3. Sign the MSIX package

Using the newly created certificate:

```powershell
& "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe" sign /f "C:\Users\speterman\Desktop\retroterm_new.pfx" /p "P@ssword123!" /fd SHA256 "C:\Users\speterman\Desktop\retroterm.net.install\Desktop.msix"
```

When successful, you'll see:

```
Done Adding Additional Store
Successfully signed: C:\Users\speterman\Desktop\retroterm.net.install\Desktop.msix
```

### 4. Verify the signature (optional)

```powershell
& "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe" verify /pa "C:\Users\speterman\Desktop\retroterm.net.install\Desktop.msix"
```

## Common Issues and Solutions

1. **Certificate/Manifest Mismatch**: The Publisher in your app manifest must exactly match the subject name of your certificate. For example, if your manifest has `Publisher="CN=Scott Peterman"`, your certificate must have the subject name `CN=Scott Peterman`.

2. **PowerShell Command Execution**: When running signtool in PowerShell, use the `&` operator at the beginning of the command to handle spaces correctly.

3. **File Paths**: Make sure to use the correct paths to your certificate and MSIX package.

4. **Password Handling**: In PowerShell, enclose the password in quotes if it contains special characters.

## Additional Notes

- For production use, you should use a certificate from a trusted certificate authority rather than a self-signed one.
- Make sure the thumbprint in the export command matches the one generated when creating your certificate.
- The certificate must have the code signing extended key usage (EKU) to be valid for signing packages.