using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseClipboard.Domain.Entities;

namespace EnterpriseClipboard.Application.Interfaces;

public interface IAppSettingRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task SetValueAsync(string key, string value, string dataType = "String", CancellationToken cancellationToken = default);
    Task<IEnumerable<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default);
}
