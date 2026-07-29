using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseClipboard.Application.Interfaces;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
