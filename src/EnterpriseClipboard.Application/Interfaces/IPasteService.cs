using System.Threading;
using System.Threading.Tasks;
using EnterpriseClipboard.Domain.Entities;

namespace EnterpriseClipboard.Application.Interfaces;

public interface IPasteService
{
    Task PasteAsync(ClipboardItem item, bool autoPasteEnabled, CancellationToken cancellationToken = default);
}
