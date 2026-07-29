using System;
using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseClipboard.Application.Interfaces;

public interface IImageStorageService
{
    Task<(string imagePath, string thumbnailPath)> SaveImageAsync(byte[] imageBytes, Guid id, CancellationToken cancellationToken = default);
    void DeleteImage(string imagePath, string thumbnailPath);
}
