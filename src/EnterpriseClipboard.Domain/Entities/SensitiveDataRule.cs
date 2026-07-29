using System;

namespace EnterpriseClipboard.Domain.Entities;

public class SensitiveDataRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Action { get; set; } = "Encrypt"; // DoNotSave, Encrypt, Expire, None
    public int ExpirationMinutes { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
