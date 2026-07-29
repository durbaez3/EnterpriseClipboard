using System;
using System.Text.RegularExpressions;
using Xunit;
using EnterpriseClipboard.Infrastructure.Services;

namespace EnterpriseClipboard.UnitTests;

public class ClipboardTests
{
    [Fact]
    public void DpapiEncryption_ShouldEncryptAndDecryptCorrectly()
    {
        // Arrange
        var service = new DpapiEncryptionService();
        string originalText = "MySecretPassword123!";

        // Act
        byte[] encrypted = service.Encrypt(originalText, userScope: true);
        string decrypted = service.Decrypt(encrypted, userScope: true);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.Equal(originalText, decrypted);
    }

    [Theory]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", true)] // JWT prefix
    [InlineData("bearer abcdef1234567890", true)] // Bearer token
    [InlineData("normal clipboard text that has no secrets", false)]
    [InlineData("api_key=AIzaSyA1234567890", true)] // API key pattern
    public void SensitiveDataRule_RegexShouldMatchSecrets(string input, bool expectedMatch)
    {
        // Arrange
        string sensitivePattern = "(eyJhbGciOi|bearer|api[_-]?key|secret[_-]?key|access[_-]?token|auth[_-]?token)";

        // Act
        bool isMatch = Regex.IsMatch(input, sensitivePattern, RegexOptions.IgnoreCase);

        // Assert
        Assert.Equal(expectedMatch, isMatch);
    }

    [Fact]
    public void ContentHash_ShouldBeConsistentForSameContent()
    {
        // Arrange
        string content1 = "Identical clipboard content";
        string content2 = "Identical clipboard content";

        // Act
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        
        byte[] bytes1 = System.Text.Encoding.UTF8.GetBytes(content1);
        byte[] hashBytes1 = sha256.ComputeHash(bytes1);
        string hash1 = BitConverter.ToString(hashBytes1).Replace("-", "").ToLowerInvariant();

        byte[] bytes2 = System.Text.Encoding.UTF8.GetBytes(content2);
        byte[] hashBytes2 = sha256.ComputeHash(bytes2);
        string hash2 = BitConverter.ToString(hashBytes2).Replace("-", "").ToLowerInvariant();

        // Assert
        Assert.Equal(hash1, hash2);
    }
}
