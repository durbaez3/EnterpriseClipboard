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

public class ClipboardRepository : IClipboardRepository
{
    private readonly ClipboardDbContext _context;

    public ClipboardRepository(ClipboardDbContext context)
    {
        _context = context;
    }

    public async Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ClipboardItems
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }

    public async Task<ClipboardItem?> GetByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await _context.ClipboardItems
            .FirstOrDefaultAsync(c => c.ContentHash == hash && !c.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<ClipboardItem>> GetPagedAsync(
        int skip, 
        int take, 
        string? search = null, 
        Guid? groupId = null, 
        bool? isFavorite = null, 
        bool includeDeleted = false, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.ClipboardItems.AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        if (groupId.HasValue)
        {
            query = query.Where(c => c.GroupId == groupId.Value);
        }

        if (isFavorite.HasValue)
        {
            query = query.Where(c => c.IsFavorite == isFavorite.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => 
                (c.PlainText != null && EF.Functions.Like(c.PlainText, $"%{search}%")) ||
                (c.SourceApplication != null && EF.Functions.Like(c.SourceApplication, $"%{search}%")) ||
                (c.CustomTitle != null && EF.Functions.Like(c.CustomTitle, $"%{search}%"))
            );
        }

        // Default order is pinned first, then newest first
        return await query
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        await _context.ClipboardItems.AddAsync(item, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        _context.Entry(item).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _context.ClipboardItems.FindAsync(new object[] { id }, cancellationToken);
        if (item != null)
        {
            item.IsDeleted = true;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetTotalCountAsync(
        string? search = null, 
        Guid? groupId = null, 
        bool? isFavorite = null, 
        bool includeDeleted = false, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.ClipboardItems.AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        if (groupId.HasValue)
        {
            query = query.Where(c => c.GroupId == groupId.Value);
        }

        if (isFavorite.HasValue)
        {
            query = query.Where(c => c.IsFavorite == isFavorite.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => 
                (c.PlainText != null && EF.Functions.Like(c.PlainText, $"%{search}%")) ||
                (c.SourceApplication != null && EF.Functions.Like(c.SourceApplication, $"%{search}%")) ||
                (c.CustomTitle != null && EF.Functions.Like(c.CustomTitle, $"%{search}%"))
            );
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task PurgeOldItemsAsync(int maxItemsToKeep, int retentionDays, CancellationToken cancellationToken = default)
    {
        // 1. Delete items exceeding expiration date (e.g. sensitive items with auto-expiration)
        var now = DateTime.UtcNow;
        var expiredItems = await _context.ClipboardItems
            .Where(c => c.ExpirationDate.HasValue && c.ExpirationDate.Value <= now && !c.IsFavorite && !c.IsPinned)
            .ToListAsync(cancellationToken);

        foreach (var item in expiredItems)
        {
            item.IsDeleted = true;
        }

        // 2. Delete items older than retentionDays (unless favorite or pinned)
        if (retentionDays > 0)
        {
            var retentionCutoff = DateTime.UtcNow.AddDays(-retentionDays);
            var oldItems = await _context.ClipboardItems
                .Where(c => c.CreatedAt < retentionCutoff && !c.IsFavorite && !c.IsPinned && !c.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var item in oldItems)
            {
                item.IsDeleted = true;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 3. Keep database within size limit (maxItemsToKeep) by soft-deleting oldest
        if (maxItemsToKeep > 0)
        {
            int currentCount = await _context.ClipboardItems.CountAsync(c => !c.IsDeleted, cancellationToken);
            if (currentCount > maxItemsToKeep)
            {
                int itemsToSoftDeleteCount = currentCount - maxItemsToKeep;
                var itemsToSoftDelete = await _context.ClipboardItems
                    .Where(c => !c.IsDeleted && !c.IsFavorite && !c.IsPinned)
                    .OrderBy(c => c.CreatedAt)
                    .Take(itemsToSoftDeleteCount)
                    .ToListAsync(cancellationToken);

                foreach (var item in itemsToSoftDelete)
                {
                    item.IsDeleted = true;
                }
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // 4. Hard-delete actual records that are soft-deleted and not pinned/fav
        var hardDeleteCutoff = DateTime.UtcNow.AddDays(-7); // Keep soft-deleted items in DB for 7 days before permanent purge
        var physicalDeleteList = await _context.ClipboardItems
            .Where(c => c.IsDeleted && c.UpdatedAt < hardDeleteCutoff)
            .ToListAsync(cancellationToken);

        if (physicalDeleteList.Any())
        {
            _context.ClipboardItems.RemoveRange(physicalDeleteList);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> OptimizeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        // Executes PRAGMA optimize & VACUUM to reclaim space and optimize indices
        int result = await _context.Database.ExecuteSqlRawAsync("VACUUM;", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("PRAGMA optimize;", cancellationToken);
        return result;
    }

    public async Task BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var items = await _context.ClipboardItems
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.IsDeleted = true;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
