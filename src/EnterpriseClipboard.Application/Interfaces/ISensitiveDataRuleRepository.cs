using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseClipboard.Domain.Entities;

namespace EnterpriseClipboard.Application.Interfaces;

public interface ISensitiveDataRuleRepository
{
    Task<IEnumerable<SensitiveDataRule>> GetAllEnabledAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SensitiveDataRule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(SensitiveDataRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(SensitiveDataRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
