using System;

namespace EnterpriseClipboard.Domain.Entities;

public class HotkeyConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Action { get; set; } = string.Empty;
    public int Modifiers { get; set; } // Key modifiers like Ctrl, Shift, Alt, Windows
    public int Key { get; set; } // Virtual Key Code
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
