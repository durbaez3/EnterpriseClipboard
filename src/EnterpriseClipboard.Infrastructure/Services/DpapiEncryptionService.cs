using System;
using System.Security.Cryptography;
using System.Text;
using EnterpriseClipboard.Application.Interfaces;

namespace EnterpriseClipboard.Infrastructure.Services;

public class DpapiEncryptionService : IEncryptionService
{
    private static readonly byte[] Entropy = { 101, 110, 116, 101, 114, 112, 114, 105, 115, 101, 99, 108, 105, 112 }; // "enterpriseclip"

    public byte[] Encrypt(string plainText, bool userScope = true)
    {
        if (string.IsNullOrEmpty(plainText))
            return Array.Empty<byte>();

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        DataProtectionScope scope = userScope ? DataProtectionScope.CurrentUser : DataProtectionScope.LocalMachine;
        return ProtectedData.Protect(plainBytes, Entropy, scope);
    }

    public string Decrypt(byte[] encryptedBytes, bool userScope = true)
    {
        if (encryptedBytes == null || encryptedBytes.Length == 0)
            return string.Empty;

        DataProtectionScope scope = userScope ? DataProtectionScope.CurrentUser : DataProtectionScope.LocalMachine;
        byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, scope);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
