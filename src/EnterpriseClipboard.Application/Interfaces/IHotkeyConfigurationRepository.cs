using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseClipboard.Domain.Entities;

namespace EnterpriseClipboard.Application.Interfaces;

public interface IHotkeyConfigurationRepository
{
    Task<IEnumerable<HotkeyConfiguration>> GetAllEnabledAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<HotkeyConfiguration>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(HotkeyConfiguration config, CancellationToken cancellationToken = default);
    Task AddAsync(HotkeyConfiguration config, CancellationToken cancellationToken = default);
}
