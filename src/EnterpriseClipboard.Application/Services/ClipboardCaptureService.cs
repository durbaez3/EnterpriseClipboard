using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseClipboard.Application.Interfaces;
using EnterpriseClipboard.Domain.Entities;
using EnterpriseClipboard.Domain.Enums;

namespace EnterpriseClipboard.Application.Services;

public class ClipboardCaptureService : IClipboardCaptureService
{
    private readonly IClipboardListener _listener;
    private readonly IClipboardReader _reader;
    private readonly IClipboardRepository _repository;
    private readonly IActiveWindowService _windowService;
    private readonly IApplicationExclusionRepository _exclusionRepository;
    private readonly ISensitiveDataRuleRepository _ruleRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IImageStorageService _imageStorageService;

    private bool _isPaused = false;
    private string? _lastHash = null;

    public event EventHandler? ClipboardItemAdded;

    public ClipboardCaptureService(
        IClipboardListener listener,
        IClipboardReader reader,
        IClipboardRepository repository,
        IActiveWindowService windowService,
        IApplicationExclusionRepository exclusionRepository,
        ISensitiveDataRuleRepository ruleRepository,
        IEncryptionService encryptionService,
        IImageStorageService imageStorageService)
    {
        _listener = listener;
        _reader = reader;
        _repository = repository;
        _windowService = windowService;
        _exclusionRepository = exclusionRepository;
        _ruleRepository = ruleRepository;
        _encryptionService = encryptionService;
        _imageStorageService = imageStorageService;

        _listener.ClipboardChanged += OnClipboardChanged;
    }

    public void StartListening(IntPtr hwnd)
    {
        _isPaused = false;
        _listener.Start(hwnd);
    }

    public void StopListening()
    {
        _listener.Stop();
    }

    public void PauseCapture()
    {
        _isPaused = true;
    }

    public void ResumeCapture()
    {
        _isPaused = false;
    }

    private void OnClipboardChanged(object? sender, EventArgs e)
    {
        if (_isPaused) return;

        // Execute capture asynchronously without blocking the UI thread
        _ = Task.Run(async () =>
        {
            try
            {
                await CaptureClipboardAsync();
            }
            catch (Exception)
            {
                // In production, log error silently via Serilog to avoid showing crash screen
            }
        });
    }

    public async Task CaptureClipboardAsync(CancellationToken cancellationToken = default)
    {
        // 1. Get Active Window details
        var windowDetails = _windowService.GetActiveWindowDetails();

        // 2. Check exclusions
        var exclusions = await _exclusionRepository.GetAllEnabledAsync(cancellationToken);
        foreach (var exclusion in exclusions)
        {
            if (!string.IsNullOrEmpty(exclusion.ExecutableName) && 
                windowDetails.ExecutableName.Equals(exclusion.ExecutableName, StringComparison.OrdinalIgnoreCase))
            {
                return; // Excluded app!
            }

            if (!string.IsNullOrEmpty(exclusion.WindowTitlePattern) && 
                Regex.IsMatch(windowDetails.WindowTitle, exclusion.WindowTitlePattern, RegexOptions.IgnoreCase))
            {
                return; // Excluded title!
            }
        }

        // 3. Read current clipboard data
        var data = _reader.ReadClipboard();
        if (data == null) return;

        // 4. Compute Hash for deduplication
        string hash = ComputeHash(data);
        if (string.IsNullOrEmpty(hash)) return;

        // Check if consecutive duplicate
        if (_lastHash == hash) return;

        // Check DB for existing hash (to merge/update counts instead of inserting duplicate)
        var existingItem = await _repository.GetByHashAsync(hash, cancellationToken);
        if (existingItem != null)
        {
            existingItem.UseCount++;
            existingItem.LastUsedAt = DateTime.UtcNow;
            existingItem.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(existingItem, cancellationToken);
            _lastHash = hash;
            ClipboardItemAdded?.Invoke(this, EventArgs.Empty);
            return;
        }

        // 5. Create new ClipboardItem
        var item = new ClipboardItem
        {
            Id = Guid.NewGuid(),
            ContentType = data.ContentType,
            PlainText = data.PlainText,
            HtmlContent = data.HtmlContent,
            RtfContent = data.RtfContent,
            SourceApplication = windowDetails.ExecutableName,
            SourceExecutablePath = windowDetails.ExecutablePath,
            SourceWindowTitle = windowDetails.WindowTitle,
            ContentHash = hash,
            SizeInBytes = CalculateSize(data),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
            UseCount = 1
        };

        // 6. Handle Images (save to disk and reference)
        if (data.ContentType == ClipboardContentType.Image && data.ImageBytes != null)
        {
            var (imagePath, thumbnailPath) = await _imageStorageService.SaveImageAsync(data.ImageBytes, item.Id, cancellationToken);
            item.ImagePath = imagePath;
            item.ThumbnailPath = thumbnailPath;
        }

        // 7. Handle Files (serialize paths to JSON)
        if (data.ContentType == ClipboardContentType.Files && data.FileList != null)
        {
            item.FileListJson = System.Text.Json.JsonSerializer.Serialize(data.FileList);
        }

        // 8. Run regex-based sensitive data scanner rules
        if (data.PlainText != null)
        {
            var rules = await _ruleRepository.GetAllEnabledAsync(cancellationToken);
            foreach (var rule in rules)
            {
                if (Regex.IsMatch(data.PlainText, rule.Pattern, RegexOptions.IgnoreCase))
                {
                    item.IsSensitive = true;
                    if (rule.Action == "DoNotSave")
                    {
                        // Discard entirely
                        return;
                    }
                    else if (rule.Action == "Encrypt")
                    {
                        item.IsEncrypted = true;
                        // Encrypt plaintext, HTML, RTF
                        if (item.PlainText != null)
                        {
                            item.EncryptedContent = _encryptionService.Encrypt(item.PlainText, userScope: true);
                            item.PlainText = null; // Remove plain text from DB
                        }
                        item.PreviewText = $"[CONTENIDO SENSIBLE CIFRADO: {rule.Name}]";
                        break;
                    }
                    else if (rule.Action == "Expire")
                    {
                        int minutes = rule.ExpirationMinutes > 0 ? rule.ExpirationMinutes : 15;
                        item.ExpirationDate = DateTime.UtcNow.AddMinutes(minutes);
                        item.PreviewText = data.PlainText.Length > 200 ? data.PlainText[..200] + "..." : data.PlainText;
                        break;
                    }
                }
            }
        }

        if (!item.IsSensitive && item.PlainText != null)
        {
            item.PreviewText = item.PlainText.Length > 200 ? item.PlainText[..200] + "..." : item.PlainText;
        }

        // 9. Persist item to database
        await _repository.AddAsync(item, cancellationToken);
        _lastHash = hash;

        // 10. Fire addition event
        ClipboardItemAdded?.Invoke(this, EventArgs.Empty);
    }

    private string ComputeHash(ClipboardData data)
    {
        using var sha256 = SHA256.Create();
        byte[] inputBytes = Array.Empty<byte>();

        if (data.ContentType == ClipboardContentType.Image && data.ImageBytes != null)
        {
            inputBytes = data.ImageBytes;
        }
        else if (data.ContentType == ClipboardContentType.Files && data.FileList != null)
        {
            var pathStr = string.Join(";", data.FileList);
            inputBytes = Encoding.UTF8.GetBytes(pathStr);
        }
        else if (data.PlainText != null)
        {
            inputBytes = Encoding.UTF8.GetBytes(data.PlainText);
        }

        if (inputBytes.Length == 0)
            return string.Empty;

        byte[] hashBytes = sha256.ComputeHash(inputBytes);
        var sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private long CalculateSize(ClipboardData data)
    {
        if (data.ContentType == ClipboardContentType.Image && data.ImageBytes != null)
        {
            return data.ImageBytes.Length;
        }
        if (data.ContentType == ClipboardContentType.Files && data.FileList != null)
        {
            return data.FileList.Count * 256; // estimated metadatos size
        }
        long size = 0;
        if (data.PlainText != null) size += data.PlainText.Length * 2;
        if (data.HtmlContent != null) size += data.HtmlContent.Length * 2;
        if (data.RtfContent != null) size += data.RtfContent.Length * 2;
        return size;
    }
}
