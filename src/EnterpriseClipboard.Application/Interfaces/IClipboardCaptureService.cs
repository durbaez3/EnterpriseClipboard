using System;
using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseClipboard.Application.Interfaces;

public interface IClipboardCaptureService
{
    event EventHandler ClipboardItemAdded;
    void StartListening(IntPtr hwnd);
    void StopListening();
    Task CaptureClipboardAsync(CancellationToken cancellationToken = default);
}
