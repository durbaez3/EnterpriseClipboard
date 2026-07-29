using System;

namespace EnterpriseClipboard.Application.Interfaces;

public interface IClipboardListener
{
    event EventHandler ClipboardChanged;
    void Start(IntPtr windowHandle);
    void Stop();
}
