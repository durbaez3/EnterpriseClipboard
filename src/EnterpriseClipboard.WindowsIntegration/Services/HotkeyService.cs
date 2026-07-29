using System;
using System.Collections.Generic;
using EnterpriseClipboard.Application.Interfaces;
using EnterpriseClipboard.WindowsIntegration.Native;

namespace EnterpriseClipboard.WindowsIntegration.Services;

public class HotkeyService : IHotkeyService
{
    private IntPtr _hwnd = IntPtr.Zero;
    private readonly Dictionary<int, Action> _actions = new();

    public void SetWindowHandle(IntPtr windowHandle)
    {
        _hwnd = windowHandle;
    }

    public bool RegisterHotkey(int id, int modifiers, int key, Action action)
    {
        if (_hwnd == IntPtr.Zero)
            return false;

        // Unregister existing if any
        UnregisterHotkey(id);

        bool success = NativeMethods.RegisterHotKey(_hwnd, id, (uint)modifiers, (uint)key);
        if (success)
        {
            _actions[id] = action;
        }
        return success;
    }

    public void UnregisterHotkey(int id)
    {
        if (_hwnd != IntPtr.Zero && _actions.ContainsKey(id))
        {
            NativeMethods.UnregisterHotKey(_hwnd, id);
            _actions.Remove(id);
        }
    }

    public void UnregisterAll()
    {
        if (_hwnd != IntPtr.Zero)
        {
            var ids = new List<int>(_actions.Keys);
            foreach (var id in ids)
            {
                NativeMethods.UnregisterHotKey(_hwnd, id);
            }
        }
        _actions.Clear();
    }

    public void HandleMessage(int msg, IntPtr wParam)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_actions.TryGetValue(id, out var action))
            {
                action.Invoke();
            }
        }
    }
}
