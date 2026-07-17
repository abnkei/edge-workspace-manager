using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace EdgeWorkspaceManager;

public sealed class UpdateManifest
{
    public string Version { get; set; } = "";
    public DateTime ReleaseDate { get; set; }
    public string DownloadUrl { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long Size { get; set; }
    public string Priority { get; set; } = "recommended";
    public List<string> ReleaseNotesTh { get; set; } = new();
    public List<string> ReleaseNotesEn { get; set; } = new();
}

public sealed record UpdateProgress(int Percentage, string Message);

public sealed class UpdateService
{
    public const string ManifestUrl = "https://github.com/abnkei/edge-workspace-manager/releases/latest/download/update.json";
    private static readonly HttpClient Client = CreateClient();

    public async Task<UpdateManifest?> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var response = await Client.GetAsync(ManifestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
        return manifest is not null && IsNewer(manifest.Version, AppInfo.Version) ? manifest : null;
    }

    public async Task<string> DownloadAndStageAsync(UpdateManifest manifest, IProgress<UpdateProgress> progress,
        CancellationToken cancellationToken = default)
    {
        var root = Path.Combine(ConfigStore.AppFolder, "Updates", manifest.Version);
        Directory.CreateDirectory(root);
        var packagePath = Path.Combine(root, "update.zip");
        var stagingPath = Path.Combine(root, "staging");
        using var response = await Client.GetAsync(manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? manifest.Size;
        await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var output = new FileStream(packagePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var buffer = new byte[81920];
            long received = 0;
            int read;
            while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                received += read;
                var percent = total > 0 ? (int)Math.Clamp(received * 100 / total, 0, 100) : 0;
                progress.Report(new UpdateProgress(percent, L.T("กำลังดาวน์โหลด...", "Downloading update...")));
            }
        }
        progress.Report(new UpdateProgress(100, L.T("กำลังตรวจสอบไฟล์...", "Verifying package...")));
        await using var packageStream = File.OpenRead(packagePath);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(packageStream, cancellationToken));
        if (!hash.Equals(manifest.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("SHA-256 mismatch. The update package was rejected.");
        if (Directory.Exists(stagingPath)) Directory.Delete(stagingPath, true);
        ZipFile.ExtractToDirectory(packagePath, stagingPath);
        if (!File.Exists(Path.Combine(stagingPath, "EdgeWorkspaceManager.exe")) ||
            !File.Exists(Path.Combine(stagingPath, "EdgeWorkspaceManager.Updater.exe")))
            throw new InvalidDataException("The update package is incomplete.");
        return stagingPath;
    }

    public static void LaunchUpdater(string stagingPath, string version)
    {
        var updater = Path.Combine(stagingPath, "EdgeWorkspaceManager.Updater.exe");
        var install = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var mainExe = Path.Combine(install, "EdgeWorkspaceManager.exe");
        Process.Start(new ProcessStartInfo(updater)
        {
            UseShellExecute = true,
            Arguments = $"--pid {Environment.ProcessId} --source \"{stagingPath}\" --target \"{install}\" --launch \"{mainExe}\" --version \"{version}\" --result \"{Path.Combine(ConfigStore.AppFolder, "Updates", "update-result.txt")}\""
        });
    }

    private static bool IsNewer(string candidate, string current) =>
        Version.TryParse(NumericVersion(candidate), out var next) &&
        Version.TryParse(NumericVersion(current), out var installed) && next > installed;

    private static string NumericVersion(string value)
    {
        var metadata = value.IndexOfAny(['+', '-']);
        return metadata >= 0 ? value[..metadata] : value;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("EdgeWorkspaceManager/" + AppInfo.Version);
        return client;
    }
}
