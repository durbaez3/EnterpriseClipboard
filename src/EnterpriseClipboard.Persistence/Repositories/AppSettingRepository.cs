using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseClipboard.Application.Interfaces;
using EnterpriseClipboard.Domain.Entities;
using EnterpriseClipboard.Persistence.Context;

namespace EnterpriseClipboard.Persistence.Repositories;

public class AppSettingRepository : IAppSettingRepository
{
    private readonly ClipboardDbContext _context;

    public AppSettingRepository(ClipboardDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _context.AppSettings.FindAsync(new object[] { key }, cancellationToken);
        return setting?.Value;
    }

    public async Task SetValueAsync(string key, string value, string dataType = "String", CancellationToken cancellationToken = default)
    {
        var setting = await _context.AppSettings.FindAsync(new object[] { key }, cancellationToken);
        if (setting == null)
        {
            setting = new AppSetting
            {
                Key = key,
                Value = value,
                DataType = dataType,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.AppSettings.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.Value = value;
            setting.DataType = dataType;
            setting.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AppSettings.ToListAsync(cancellationToken);
    }
}
