using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseClipboard.Domain.Entities;

namespace EnterpriseClipboard.Application.Interfaces;

public interface IApplicationExclusionRepository
{
    Task<IEnumerable<ApplicationExclusion>> GetAllEnabledAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ApplicationExclusion>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ApplicationExclusion exclusion, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApplicationExclusion exclusion, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
