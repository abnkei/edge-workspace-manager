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
    public static string UpdateLogPath => Path.Combine(ConfigStore.AppFolder, "Updates", "update.log");
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
        var partialPath = packagePath + ".part";
        var stagingPath = Path.Combine(root, "staging");
        EnsureFreeSpace(root, manifest.Size);
        Exception? lastError = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                if (File.Exists(partialPath)) File.Delete(partialPath);
                await DownloadPackageAsync(manifest, partialPath, progress, cancellationToken);
                File.Move(partialPath, packagePath, true);
                lastError = null;
                break;
            }
            catch (Exception ex) when (IsRetryable(ex) && attempt < 3)
            {
                lastError = ex;
                WriteLog($"Download attempt {attempt} failed for {manifest.Version}: {ex}");
                progress.Report(new UpdateProgress(0, L.T(
                    $"ดาวน์โหลดขัดข้อง กำลังลองใหม่ ({attempt + 1}/3)...",
                    $"Download interrupted. Retrying ({attempt + 1}/3)...")));
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
        }
        if (lastError is not null) throw lastError;

        progress.Report(new UpdateProgress(100, L.T("กำลังตรวจสอบไฟล์...", "Verifying package...")));
        await using var packageStream = File.OpenRead(packagePath);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(packageStream, cancellationToken));
        if (!hash.Equals(manifest.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            WriteLog($"SHA-256 mismatch for {manifest.Version}. Expected {manifest.Sha256}, received {hash}.");
            throw new InvalidDataException("SHA-256 mismatch. The update package was rejected.");
        }
        if (Directory.Exists(stagingPath)) Directory.Delete(stagingPath, true);
        try { ZipFile.ExtractToDirectory(packagePath, stagingPath); }
        catch (InvalidDataException ex)
        {
            WriteLog($"Package extraction failed for {manifest.Version}: {ex}");
            throw new InvalidDataException("The downloaded update package is damaged or incomplete.", ex);
        }
        if (!File.Exists(Path.Combine(stagingPath, "EdgeWorkspaceManager.exe")) ||
            !File.Exists(Path.Combine(stagingPath, "EdgeWorkspaceManager.Updater.exe")))
            throw new InvalidDataException("The update package is incomplete.");
        WriteLog($"Update {manifest.Version} downloaded, verified, and staged successfully.");
        return stagingPath;
    }

    public static void LaunchUpdater(string stagingPath, string version)
    {
        var updater = Path.Combine(stagingPath, "EdgeWorkspaceManager.Updater.exe");
        var install = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var mainExe = Path.Combine(install, "EdgeWorkspaceManager.exe");
        var updateRoot = Path.Combine(ConfigStore.AppFolder, "Updates");
        var backup = Path.Combine(updateRoot, "Backups", $"{AppInfo.Version}-before-{version}");
        var health = Path.Combine(updateRoot, $"health-{version}.ok");
        var result = Path.Combine(updateRoot, "update-result.txt");
        if (File.Exists(health)) File.Delete(health);
        if (File.Exists(result)) File.Delete(result);
        Process.Start(new ProcessStartInfo(updater)
        {
            UseShellExecute = true,
            Arguments = $"--pid {Environment.ProcessId} --source \"{stagingPath}\" --target \"{install}\" --launch \"{mainExe}\" --version \"{version}\" --current-version \"{AppInfo.Version}\" --backup \"{backup}\" --health \"{health}\" --result \"{result}\" --log \"{UpdateLogPath}\""
        });
    }

    public static string FriendlyError(Exception ex) => ex switch
    {
        UnauthorizedAccessException => L.T(
            "ไม่มีสิทธิ์เขียนไฟล์ โปรดลองเปิดโปรแกรมด้วยสิทธิ์ที่เหมาะสมหรือตรวจสอบโฟลเดอร์ติดตั้ง",
            "The updater cannot write files. Check the install folder permissions or run with appropriate access."),
        HttpRequestException => L.T(
            "ดาวน์โหลดไม่สำเร็จ กรุณาตรวจสอบอินเทอร์เน็ต Firewall หรือ Proxy แล้วลองใหม่",
            "The download failed. Check your internet connection, firewall, or proxy and try again."),
        TaskCanceledException => L.T(
            "การดาวน์โหลดใช้เวลานานเกินไป กรุณาตรวจสอบอินเทอร์เน็ตแล้วลองใหม่",
            "The download timed out. Check your internet connection and try again."),
        InvalidDataException when ex.Message.Contains("SHA-256", StringComparison.OrdinalIgnoreCase) => L.T(
            "ไฟล์อัปเดตไม่ผ่านการตรวจสอบ SHA-256 และถูกปฏิเสธเพื่อความปลอดภัย",
            "The update failed SHA-256 verification and was rejected for safety."),
        InvalidDataException => L.T(
            "ไฟล์อัปเดตเสียหายหรือไม่ครบ กรุณาดาวน์โหลดใหม่",
            "The update package is damaged or incomplete. Download it again."),
        IOException when IsDiskFull(ex) => L.T(
            "พื้นที่จัดเก็บไม่เพียงพอ กรุณาเพิ่มพื้นที่ว่างแล้วลองใหม่",
            "There is not enough free disk space. Free some space and try again."),
        IOException => L.T(
            "ไม่สามารถอ่านหรือเขียนไฟล์อัปเดตได้ กรุณาปิดโปรแกรมที่กำลังใช้งานไฟล์แล้วลองใหม่",
            "The update files could not be read or written. Close programs using the files and try again."),
        _ => L.T("อัปเดตไม่สำเร็จ กรุณาลองใหม่หรือตรวจสอบ Update Log", "The update failed. Try again or review the Update Log.")
    };

    public static void WriteLog(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(UpdateLogPath)!);
            File.AppendAllText(UpdateLogPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch { }
    }

    private static async Task DownloadPackageAsync(UpdateManifest manifest, string destination,
        IProgress<UpdateProgress> progress, CancellationToken cancellationToken)
    {
        using var response = await Client.GetAsync(manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? manifest.Size;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
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
        if (manifest.Size > 0 && received != manifest.Size)
            throw new InvalidDataException($"Downloaded size mismatch. Expected {manifest.Size}, received {received}.");
    }

    private static void EnsureFreeSpace(string path, long packageSize)
    {
        var root = Path.GetPathRoot(Path.GetFullPath(path));
        if (string.IsNullOrWhiteSpace(root)) return;
        var required = Math.Max(packageSize * 3, 300L * 1024 * 1024);
        if (new DriveInfo(root).AvailableFreeSpace < required)
            throw new IOException("There is not enough disk space for the update package and recovery backup.", unchecked((int)0x80070070));
    }

    private static bool IsRetryable(Exception ex) => ex is HttpRequestException or TaskCanceledException ||
        ex is IOException io && !IsDiskFull(io) ||
        ex is InvalidDataException data && data.Message.Contains("size mismatch", StringComparison.OrdinalIgnoreCase);

    private static bool IsDiskFull(Exception ex) => ex.HResult == unchecked((int)0x80070070) ||
        ex.HResult == unchecked((int)0x80070027);

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
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("EdgeWorkspaceManager/" + AppInfo.Version);
        return client;
    }
}
