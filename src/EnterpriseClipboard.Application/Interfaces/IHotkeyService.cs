using System;

namespace EnterpriseClipboard.Application.Interfaces;

public interface IHotkeyService
{
    void SetWindowHandle(IntPtr windowHandle);
    bool RegisterHotkey(int id, int modifiers, int key, Action action);
    void UnregisterHotkey(int id);
    void UnregisterAll();
    void HandleMessage(int msg, IntPtr wParam);
}
