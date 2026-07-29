using System;
using System.Diagnostics;
using System.Text;
using EnterpriseClipboard.Application.Interfaces;
using EnterpriseClipboard.WindowsIntegration.Native;

namespace EnterpriseClipboard.WindowsIntegration.Services;

public class ActiveWindowService : IActiveWindowService
{
    public ActiveWindowDetails GetActiveWindowDetails()
    {
        var details = new ActiveWindowDetails();
        IntPtr hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return details;

        // 1. Get window title
        var titleBuilder = new StringBuilder(256);
        if (NativeMethods.GetWindowText(hwnd, titleBuilder, titleBuilder.Capacity) > 0)
        {
            details.WindowTitle = titleBuilder.ToString();
        }

        // 2. Get window class
        var classBuilder = new StringBuilder(256);
        if (NativeMethods.GetClassName(hwnd, classBuilder, classBuilder.Capacity) > 0)
        {
            details.WindowClass = classBuilder.ToString();
        }

        // 3. Get process path and name
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
        if (processId != 0)
        {
            try
            {
                using var process = Process.GetProcessById((int)processId);
                details.ExecutableName = process.ProcessName + ".exe";
                details.ExecutablePath = process.MainModule?.FileName ?? string.Empty;
            }
            catch (Exception)
            {
                // Can happen if the process exits or is high integrity level (admin)
                details.ExecutableName = "Unknown";
                details.ExecutablePath = "Unknown";
            }
        }

        return details;
    }
}
