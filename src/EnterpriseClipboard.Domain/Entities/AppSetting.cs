using System;

namespace EnterpriseClipboard.Domain.Entities;

public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
