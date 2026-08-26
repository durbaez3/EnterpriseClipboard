using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EnterpriseClipboard.Application.Interfaces;

namespace EnterpriseClipboard.Application.Services;

public class UpdateService : IUpdateService
{
    // GitHub repository API endpoint for latest release
    private const string GitHubApiUrl = "https://api.github.com/repos/durbaez3/EnterpriseClipboard/releases/latest";
    private const string AppName = "EnterpriseClipboard.App.exe";

    public async Task<(bool Available, string LatestVersion, string DownloadUrl)> CheckForUpdatesAsync()
    {
        try
        {
            using var client = new HttpClient();
            // GitHub API requires a User-Agent header
            client.DefaultRequestHeaders.Add("User-Agent", "EnterpriseClipboard-Updater");
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetFromJsonAsync<GitHubRelease>(GitHubApiUrl);
            if (response == null) return (false, string.Empty, string.Empty);

            // Parse version from tag (e.g. "v1.1.0" → "1.1.0")
            var latestVersion = response.TagName?.TrimStart('v') ?? string.Empty;
            var currentVersion = Assembly.GetExecutingAssembly()
                .GetName().Version?.ToString(3) ?? "1.0.0";

            if (string.IsNullOrEmpty(latestVersion))
                return (false, string.Empty, string.Empty);

            if (Version.TryParse(latestVersion, out var latest) &&
                Version.TryParse(currentVersion, out var current) &&
                latest > current)
            {
                // Find the .exe asset in the release
                var exeAsset = response.Assets?.Find(a =>
                    a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

                var downloadUrl = exeAsset?.BrowserDownloadUrl ?? string.Empty;
                return (true, latestVersion, downloadUrl);
            }

            return (false, latestVersion, string.Empty);
        }
        catch
        {
            // Silently fail if no network, GitHub down, etc.
            return (false, string.Empty, string.Empty);
        }
    }

    public async Task DownloadAndApplyUpdateAsync(string downloadUrl, Action<int> progressCallback)
    {
        if (string.IsNullOrEmpty(downloadUrl)) return;

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "EnterpriseClipboard-Updater");

        var currentExePath = Process.GetCurrentProcess().MainModule?.FileName
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppName);

        var tempPath = currentExePath + ".update";

        // Download new version to temp file with progress
        using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0L;
        var downloadedBytes = 0L;

        await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var contentStream = await response.Content.ReadAsStreamAsync();

        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            downloadedBytes += bytesRead;
            if (totalBytes > 0)
            {
                var percent = (int)(downloadedBytes * 100 / totalBytes);
                progressCallback?.Invoke(percent);
            }
        }

        fileStream.Close();

        // Write a small batch launcher that replaces the exe and restarts
        var updaterBat = Path.Combine(Path.GetTempPath(), "ecm_update.bat");
        await File.WriteAllTextAsync(updaterBat,
            $"""
            @echo off
            timeout /t 2 /nobreak > nul
            move /y "{tempPath}" "{currentExePath}"
            start "" "{currentExePath}"
            del "%~f0"
            """);

        // Launch batch and exit current instance
        Process.Start(new ProcessStartInfo
        {
            FileName = updaterBat,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });

        // Give the batch a moment to start, then exit
        await Task.Delay(500);
        Environment.Exit(0);
    }
}

// GitHub API response models
internal class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubAsset>? Assets { get; set; }
}

internal class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = string.Empty;
}
