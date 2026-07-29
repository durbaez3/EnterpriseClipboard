using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EnterpriseClipboard.Application.Interfaces;

namespace EnterpriseClipboard.WindowsIntegration.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly string _storageDir;

    public ImageStorageService()
    {
        _storageDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "EnterpriseClipboard", 
            "Images"
        );

        if (!Directory.Exists(_storageDir))
        {
            Directory.CreateDirectory(_storageDir);
        }
    }

    public async Task<(string imagePath, string thumbnailPath)> SaveImageAsync(byte[] imageBytes, Guid id, CancellationToken cancellationToken = default)
    {
        string imagePath = Path.Combine(_storageDir, $"{id}.png");
        string thumbnailPath = Path.Combine(_storageDir, $"{id}_thumb.png");

        // Save original image file
        await File.WriteAllBytesAsync(imagePath, imageBytes, cancellationToken);

        // Generate thumbnail on STA thread
        var tcs = new TaskCompletionSource<bool>();
        var thread = new Thread(() =>
        {
            try
            {
                using var ms = new MemoryStream(imageBytes);
                var bitmap = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

                // Target width: 150px (preserving aspect ratio)
                double scale = 150.0 / bitmap.PixelWidth;
                if (scale > 1.0) scale = 1.0; // Don't upscale

                int newWidth = (int)(bitmap.PixelWidth * scale);
                int newHeight = (int)(bitmap.PixelHeight * scale);

                var resized = new TransformedBitmap(bitmap, new ScaleTransform(scale, scale));
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(resized));

                using var thumbFs = new FileStream(thumbnailPath, FileMode.Create, FileAccess.Write);
                encoder.Save(thumbFs);

                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        await tcs.Task;

        return (imagePath, thumbnailPath);
    }

    public void DeleteImage(string imagePath, string thumbnailPath)
    {
        try
        {
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                File.Delete(imagePath);
            }
            if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
            {
                File.Delete(thumbnailPath);
            }
        }
        catch (Exception)
        {
            // Ignore deletion errors during cleanup (log if needed)
        }
    }
}
