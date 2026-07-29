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

public class ApplicationExclusionRepository : IApplicationExclusionRepository
{
    private readonly ClipboardDbContext _context;

    public ApplicationExclusionRepository(ClipboardDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ApplicationExclusion>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationExclusions
            .Where(e => e.IsEnabled)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ApplicationExclusion>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationExclusions
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ApplicationExclusion exclusion, CancellationToken cancellationToken = default)
    {
        await _context.ApplicationExclusions.AddAsync(exclusion, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ApplicationExclusion exclusion, CancellationToken cancellationToken = default)
    {
        _context.Entry(exclusion).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exclusion = await _context.ApplicationExclusions.FindAsync(new object[] { id }, cancellationToken);
        if (exclusion != null)
        {
            _context.ApplicationExclusions.Remove(exclusion);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
