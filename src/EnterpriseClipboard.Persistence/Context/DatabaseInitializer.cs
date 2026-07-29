using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseClipboard.Application.Interfaces;
using EnterpriseClipboard.Domain.Entities;

namespace EnterpriseClipboard.Persistence.Context;

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly ClipboardDbContext _context;

    public DatabaseInitializer(ClipboardDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // 1. Ensure database is created
        await _context.Database.EnsureCreatedAsync(cancellationToken);

        // 2. Configure performance options for SQLite
        await _context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;", cancellationToken);

        // 3. Seed default exclusions
        if (!await _context.ApplicationExclusions.AnyAsync(cancellationToken))
        {
            await _context.ApplicationExclusions.AddRangeAsync(new[]
            {
                new ApplicationExclusion { ExecutableName = "KeePass.exe", CreatedAt = DateTime.UtcNow },
                new ApplicationExclusion { ExecutableName = "1Password.exe", CreatedAt = DateTime.UtcNow },
                new ApplicationExclusion { ExecutableName = "Bitwarden.exe", CreatedAt = DateTime.UtcNow },
                new ApplicationExclusion { ExecutableName = "KeePassXC.exe", CreatedAt = DateTime.UtcNow },
                new ApplicationExclusion { ExecutableName = "CredentialManager.exe", CreatedAt = DateTime.UtcNow }
            }, cancellationToken);
        }

        // 4. Seed default SensitiveDataRules
        if (!await _context.SensitiveDataRules.AnyAsync(cancellationToken))
        {
            await _context.SensitiveDataRules.AddRangeAsync(new[]
            {
                new SensitiveDataRule 
                { 
                    Name = "API Keys / JWT / Tokens", 
                    Pattern = "(eyJhbGciOi|bearer|api[_-]?key|secret[_-]?key|access[_-]?token|auth[_-]?token)", 
                    Action = "Encrypt", 
                    Priority = 100, 
                    IsEnabled = true, 
                    CreatedAt = DateTime.UtcNow 
                },
                new SensitiveDataRule 
                { 
                    Name = "Private Keys / Certificates", 
                    Pattern = "(-----BEGIN[A-Z ]*PRIVATE KEY-----|-----BEGIN CERTIFICATE-----)", 
                    Action = "Encrypt", 
                    Priority = 90, 
                    IsEnabled = true, 
                    CreatedAt = DateTime.UtcNow 
                },
                new SensitiveDataRule 
                { 
                    Name = "Credit Cards / CVV", 
                    Pattern = "\\b(?:4[0-9]{12}(?:[0-9]{3})?|[25][0-9]{15}|6011[0-9]{12}|3[47][0-9]{13})\\b", 
                    Action = "Encrypt", 
                    Priority = 80, 
                    IsEnabled = true, 
                    CreatedAt = DateTime.UtcNow 
                },
                new SensitiveDataRule 
                { 
                    Name = "Database Connection Strings", 
                    Pattern = "(User ID=\\w+;Password=\\w+|Host=\\w+;Database=\\w+|Server=\\w+;Database=\\w+)", 
                    Action = "Encrypt", 
                    Priority = 70, 
                    IsEnabled = true, 
                    CreatedAt = DateTime.UtcNow 
                }
            }, cancellationToken);
        }

        // 5. Seed default Hotkeys
        if (!await _context.HotkeyConfigurations.AnyAsync(cancellationToken))
        {
            await _context.HotkeyConfigurations.AddRangeAsync(new[]
            {
                // Action: OpenQuickPopup (suggested default: Ctrl + Shift + V)
                // Modifiers: Ctrl = 2, Shift = 4 => 2 + 4 = 6. Key: V = 0x56
                new HotkeyConfiguration { Action = "OpenQuickPopup", Modifiers = 6, Key = 0x56, IsEnabled = true, CreatedAt = DateTime.UtcNow },
                
                // Action: OpenMainWindow (suggested default: Ctrl + Shift + H)
                // Key: H = 0x48
                new HotkeyConfiguration { Action = "OpenMainWindow", Modifiers = 6, Key = 0x48, IsEnabled = true, CreatedAt = DateTime.UtcNow }
            }, cancellationToken);
        }

        // 6. Seed default AppSettings
        if (!await _context.AppSettings.AnyAsync(cancellationToken))
        {
            await _context.AppSettings.AddRangeAsync(new[]
            {
                new AppSetting { Key = "General:Theme", Value = "Dark", DataType = "String", UpdatedAt = DateTime.UtcNow },
                new AppSetting { Key = "General:MinimizeToTray", Value = "True", DataType = "Boolean", UpdatedAt = DateTime.UtcNow },
                new AppSetting { Key = "General:Language", Value = "es", DataType = "String", UpdatedAt = DateTime.UtcNow },
                new AppSetting { Key = "General:AutoPaste", Value = "True", DataType = "Boolean", UpdatedAt = DateTime.UtcNow },
                new AppSetting { Key = "History:MaxClips", Value = "500", DataType = "Integer", UpdatedAt = DateTime.UtcNow },
                new AppSetting { Key = "History:RetentionDays", Value = "30", DataType = "Integer", UpdatedAt = DateTime.UtcNow }
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
