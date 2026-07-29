using System;

namespace EnterpriseClipboard.Domain.Entities;

public class ApplicationExclusion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ExecutableName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string WindowTitlePattern { get; set; } = string.Empty;
    public string WindowClass { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
