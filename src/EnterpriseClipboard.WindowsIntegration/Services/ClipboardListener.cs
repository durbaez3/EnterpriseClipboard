using System;
using EnterpriseClipboard.Application.Interfaces;
using EnterpriseClipboard.WindowsIntegration.Native;

namespace EnterpriseClipboard.WindowsIntegration.Services;

public class ClipboardListener : IClipboardListener
{
    private IntPtr _hwnd = IntPtr.Zero;
    public event EventHandler? ClipboardChanged;

    public void Start(IntPtr windowHandle)
    {
        if (_hwnd != IntPtr.Zero)
            Stop();

        _hwnd = windowHandle;
        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.AddClipboardFormatListener(_hwnd);
        }
    }

    public void Stop()
    {
        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.RemoveClipboardFormatListener(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }

    public void HandleMessage(int msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
        {
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
