using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using EnterpriseClipboard.Application.Interfaces;
using EnterpriseClipboard.Domain.Enums;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;

namespace EnterpriseClipboard.WindowsIntegration.Services;

public class ClipboardReader : IClipboardReader
{
    public ClipboardData? ReadClipboard()
    {
        ClipboardData? result = null;
        var thread = new Thread(() =>
        {
            try
            {
                var dataObject = Clipboard.GetDataObject();
                if (dataObject == null)
                    return;

                // 1. Files format
                if (dataObject.GetDataPresent(DataFormats.FileDrop))
                {
                    if (dataObject.GetData(DataFormats.FileDrop) is string[] files)
                    {
                        result = new ClipboardData
                        {
                            ContentType = ClipboardContentType.Files,
                            FileList = new List<string>(files),
                            PlainText = string.Join(Environment.NewLine, files)
                        };
                        return;
                    }
                }

                // 2. Image format
                if (dataObject.GetDataPresent(DataFormats.Bitmap))
                {
                    var bitmap = Clipboard.GetImage();
                    if (bitmap != null)
                    {
                        byte[]? imageBytes = null;
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        using (var ms = new MemoryStream())
                        {
                            encoder.Save(ms);
                            imageBytes = ms.ToArray();
                        }

                        result = new ClipboardData
                        {
                            ContentType = ClipboardContentType.Image,
                            ImageBytes = imageBytes,
                            PlainText = $"[Imagen: {bitmap.PixelWidth}x{bitmap.PixelHeight}]"
                        };
                        return;
                    }
                }

                // 3. RTF format
                if (dataObject.GetDataPresent(DataFormats.Rtf))
                {
                    string rtf = (string)dataObject.GetData(DataFormats.Rtf);
                    string text = (string)dataObject.GetData(DataFormats.UnicodeText) ?? (string)dataObject.GetData(DataFormats.Text) ?? string.Empty;
                    result = new ClipboardData
                    {
                        ContentType = ClipboardContentType.Rtf,
                        RtfContent = rtf,
                        PlainText = text
                    };
                    return;
                }

                // 4. HTML format
                if (dataObject.GetDataPresent(DataFormats.Html))
                {
                    string html = (string)dataObject.GetData(DataFormats.Html);
                    string text = (string)dataObject.GetData(DataFormats.UnicodeText) ?? (string)dataObject.GetData(DataFormats.Text) ?? string.Empty;
                    result = new ClipboardData
                    {
                        ContentType = ClipboardContentType.Html,
                        HtmlContent = html,
                        PlainText = text
                    };
                    return;
                }

                // 5. Plain/Unicode Text format
                if (dataObject.GetDataPresent(DataFormats.UnicodeText) || dataObject.GetDataPresent(DataFormats.Text))
                {
                    string text = (string)dataObject.GetData(DataFormats.UnicodeText) ?? (string)dataObject.GetData(DataFormats.Text) ?? string.Empty;
                    if (!string.IsNullOrEmpty(text))
                    {
                        result = new ClipboardData
                        {
                            ContentType = ClipboardContentType.Text,
                            PlainText = text
                        };
                        return;
                    }
                }
            }
            catch (Exception)
            {
                // Clipboard might be locked by another application. Return null so we can retry or ignore.
                result = null;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(2000); // Wait up to 2 seconds

        return result;
    }
}
