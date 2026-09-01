using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using EnterpriseClipboard.Application.Interfaces;
using EnterpriseClipboard.Domain.Entities;
using EnterpriseClipboard.Domain.Enums;
using EnterpriseClipboard.WindowsIntegration.Native;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;

namespace EnterpriseClipboard.WindowsIntegration.Services;

public class PasteService : IPasteService
{
    public async Task PasteAsync(ClipboardItem item, bool autoPasteEnabled, CancellationToken cancellationToken = default)
    {
        // 1. Write the selected item to the clipboard
        bool success = false;
        int retries = 3;
        while (!success && retries > 0)
        {
            try
            {
                // Run on STA thread as required by WPF Clipboard
                var tcs = new TaskCompletionSource<bool>();
                var thread = new Thread(() =>
                {
                    try
                    {
                        var dataObject = new System.Windows.DataObject();
                        bool hasData = false;

                        // Always set plain text if available (fallback for apps that don't support rich formats)
                        if (item.PlainText != null)
                        {
                            dataObject.SetText(item.PlainText);
                            hasData = true;
                        }

                        if (item.ContentType == ClipboardContentType.Html && item.HtmlContent != null)
                        {
                            dataObject.SetData(DataFormats.Html, item.HtmlContent);
                            hasData = true;
                        }
                        else if (item.ContentType == ClipboardContentType.Rtf && item.RtfContent != null)
                        {
                            dataObject.SetData(DataFormats.Rtf, item.RtfContent);
                            hasData = true;
                        }
                        else if (item.ContentType == ClipboardContentType.Image && item.ImagePath != null && File.Exists(item.ImagePath))
                        {
                            // Load image and set it to clipboard
                            var imageBytes = File.ReadAllBytes(item.ImagePath);
                            using (var ms = new MemoryStream(imageBytes))
                            {
                                var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
                                    ms, 
                                    System.Windows.Media.Imaging.BitmapCreateOptions.None, 
                                    System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                                if (decoder.Frames.Count > 0)
                                {
                                    dataObject.SetImage(decoder.Frames[0]);
                                    hasData = true;
                                }
                            }
                        }
                        else if (item.ContentType == ClipboardContentType.Files && item.FileListJson != null)
                        {
                            // Deserialize list of files
                            var files = System.Text.Json.JsonSerializer.Deserialize<List<string>>(item.FileListJson);
                            if (files != null && files.Count > 0)
                            {
                                var fileCollection = new System.Collections.Specialized.StringCollection();
                                foreach (var file in files)
                                {
                                    fileCollection.Add(file);
                                }
                                dataObject.SetFileDropList(fileCollection);
                                hasData = true;
                            }
                        }

                        if (hasData)
                        {
                            Clipboard.SetDataObject(dataObject, true);
                        }

                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                success = await tcs.Task;
            }
            catch (Exception)
            {
                retries--;
                if (retries == 0)
                    throw;
                await Task.Delay(100, cancellationToken); // delay before retry
            }
        }

        // 2. Perform auto-paste simulation (Ctrl + V) if enabled
        if (autoPasteEnabled)
        {
            // Give window a short moment to recover focus
            await Task.Delay(150, cancellationToken);
            SimulateCtrlV();
        }
    }

    private void SimulateCtrlV()
    {
        const ushort VK_CONTROL = 0x11;
        const ushort VK_V = 0x56;

        var inputs = new NativeMethods.INPUT[4];

        // 1. Control Key Down
        inputs[0] = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = VK_CONTROL,
                    wScan = 0,
                    dwFlags = 0, // 0 for Key Down
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // 2. V Key Down
        inputs[1] = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = VK_V,
                    wScan = 0,
                    dwFlags = 0, // 0 for Key Down
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // 3. V Key Up
        inputs[2] = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = VK_V,
                    wScan = 0,
                    dwFlags = NativeMethods.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // 4. Control Key Up
        inputs[3] = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = VK_CONTROL,
                    wScan = 0,
                    dwFlags = NativeMethods.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
    }
}
