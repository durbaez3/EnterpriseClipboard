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

public class SensitiveDataRuleRepository : ISensitiveDataRuleRepository
{
    private readonly ClipboardDbContext _context;

    public SensitiveDataRuleRepository(ClipboardDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SensitiveDataRule>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SensitiveDataRules
            .Where(r => r.IsEnabled)
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SensitiveDataRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SensitiveDataRules
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SensitiveDataRule rule, CancellationToken cancellationToken = default)
    {
        await _context.SensitiveDataRules.AddAsync(rule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SensitiveDataRule rule, CancellationToken cancellationToken = default)
    {
        _context.Entry(rule).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _context.SensitiveDataRules.FindAsync(new object[] { id }, cancellationToken);
        if (rule != null)
        {
            _context.SensitiveDataRules.Remove(rule);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
