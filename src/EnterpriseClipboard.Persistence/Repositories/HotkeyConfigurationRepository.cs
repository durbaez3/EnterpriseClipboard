using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnterpriseClipboard.Application.Interfaces;
using EnterpriseClipboard.Domain.Entities;
using EnterpriseClipboard.Persistence.Context;

namespace EnterpriseClipboard.Persistence.Repositories;

public class HotkeyConfigurationRepository : IHotkeyConfigurationRepository
{
    private readonly ClipboardDbContext _context;

    public HotkeyConfigurationRepository(ClipboardDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HotkeyConfiguration>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.HotkeyConfigurations
            .Where(h => h.IsEnabled)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<HotkeyConfiguration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.HotkeyConfigurations.ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(HotkeyConfiguration config, CancellationToken cancellationToken = default)
    {
        _context.Entry(config).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAsync(HotkeyConfiguration config, CancellationToken cancellationToken = default)
    {
        await _context.HotkeyConfigurations.AddAsync(config, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
