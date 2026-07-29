using System;

namespace EnterpriseClipboard.Domain.Entities;

public class ClipboardItemTag
{
    public Guid ClipboardItemId { get; set; }
    public Guid ClipboardTagId { get; set; }
}
