using System;
using EnterpriseClipboard.Domain.Enums;

namespace EnterpriseClipboard.Domain.Entities;

public class ClipboardItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ClipboardContentType ContentType { get; set; }
    public string? PlainText { get; set; }
    public byte[]? EncryptedContent { get; set; }
    public string? PreviewText { get; set; }
    public string? HtmlContent { get; set; }
    public string? RtfContent { get; set; }
    public string? ImagePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? FileListJson { get; set; }
    public string? SourceApplication { get; set; }
    public string? SourceExecutablePath { get; set; }
    public string? SourceWindowTitle { get; set; }
    public string? ContentHash { get; set; }
    public long SizeInBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    public int UseCount { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsPinned { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public Guid? GroupId { get; set; }
    public string? CustomTitle { get; set; }
    public bool IsDeleted { get; set; }
}
