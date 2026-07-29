using System.Collections.Generic;
using EnterpriseClipboard.Domain.Enums;

namespace EnterpriseClipboard.Application.Interfaces;

public class ClipboardData
{
    public ClipboardContentType ContentType { get; set; }
    public string? PlainText { get; set; }
    public string? HtmlContent { get; set; }
    public string? RtfContent { get; set; }
    public byte[]? ImageBytes { get; set; }
    public List<string>? FileList { get; set; }
}

public interface IClipboardReader
{
    ClipboardData? ReadClipboard();
}
