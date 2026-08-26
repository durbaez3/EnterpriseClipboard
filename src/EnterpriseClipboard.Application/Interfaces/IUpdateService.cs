namespace EnterpriseClipboard.Application.Interfaces;

public interface IUpdateService
{
    /// <summary>Checks GitHub Releases for a newer version. Returns (isUpdateAvailable, latestVersion, downloadUrl).</summary>
    Task<(bool Available, string LatestVersion, string DownloadUrl)> CheckForUpdatesAsync();

    /// <summary>Downloads and applies the update from the given URL, replacing the running executable.</summary>
    Task DownloadAndApplyUpdateAsync(string downloadUrl, Action<int> progressCallback);
}
