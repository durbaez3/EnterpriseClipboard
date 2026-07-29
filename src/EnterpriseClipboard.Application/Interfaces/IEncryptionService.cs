namespace EnterpriseClipboard.Application.Interfaces;

public interface IEncryptionService
{
    byte[] Encrypt(string plainText, bool userScope = true);
    string Decrypt(byte[] encryptedBytes, bool userScope = true);
}
