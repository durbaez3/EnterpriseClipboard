using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseClipboard.Domain.Entities;

namespace EnterpriseClipboard.Application.Interfaces;

public interface IClipboardRepository
{
    Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClipboardItem?> GetByHashAsync(string hash, CancellationToken cancellationToken = default);
    Task<IEnumerable<ClipboardItem>> GetPagedAsync(int skip, int take, string? search = null, Guid? groupId = null, bool? isFavorite = null, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(string? search = null, Guid? groupId = null, bool? isFavorite = null, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task PurgeOldItemsAsync(int maxItemsToKeep, int retentionDays, CancellationToken cancellationToken = default);
    Task<int> OptimizeDatabaseAsync(CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
