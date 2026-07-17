using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace EdgeWorkspaceManager;

public sealed class MetadataBackupManifest
{
    public string Product { get; set; } = "Edge Workspace Manager";
    public int FormatVersion { get; set; } = 1;
    public string AppVersion { get; set; } = AppInfo.Version;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int InstanceCount { get; set; }
    public int TabCount { get; set; }
    public int FavoriteCount { get; set; }
    public string MetadataSha256 { get; set; } = "";
}

public sealed class MetadataBackupData
{
    public List<BrowserWorkspace> Workspaces { get; set; } = new();
    public List<FavoriteItem> Favorites { get; set; } = new();
}

public sealed class ImportedMetadataBackup
{
    public required MetadataBackupManifest Manifest { get; init; }
    public required List<BrowserWorkspace> Workspaces { get; init; }
    public required List<FavoriteItem> Favorites { get; init; }
}

public static class MetadataBackupService
{
    private const int CurrentFormatVersion = 1;
    private const long MaxBackupBytes = 50 * 1024 * 1024;
    private const long MaxMetadataBytes = 25 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static void Export(string path, IEnumerable<BrowserWorkspace> workspaces,
        IEnumerable<FavoriteItem> favorites)
    {
        var data = new MetadataBackupData
        {
            Workspaces = workspaces.Select(CloneWorkspace).ToList(),
            Favorites = favorites.Select(CloneFavorite).ToList()
        };
        if (data.Workspaces.Count == 0)
            throw new InvalidOperationException(L.T("ไม่มี Instance สำหรับ Export", "There are no instances to export."));

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions);
        var manifest = new MetadataBackupManifest
        {
            FormatVersion = CurrentFormatVersion,
            InstanceCount = data.Workspaces.Count,
            TabCount = data.Workspaces.Sum(workspace => workspace.Tabs.Count),
            FavoriteCount = data.Favorites.Count,
            MetadataSha256 = Convert.ToHexString(SHA256.HashData(metadataBytes))
        };

        var tempPath = path + ".tmp";
        try
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            using (var archive = ZipFile.Open(tempPath, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "manifest.json", JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions));
                WriteEntry(archive, "metadata.json", metadataBytes);
            }
            File.Move(tempPath, path, true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public static ImportedMetadataBackup Import(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists) throw new FileNotFoundException(L.T("ไม่พบไฟล์ Backup", "Backup file was not found."), path);
        if (file.Length <= 0 || file.Length > MaxBackupBytes)
            throw new InvalidDataException(L.T("ไฟล์ Backup มีขนาดไม่ถูกต้อง", "The backup file size is invalid."));

        using var archive = ZipFile.OpenRead(path);
        var manifestBytes = ReadRequiredEntry(archive, "manifest.json", 1024 * 1024);
        var metadataBytes = ReadRequiredEntry(archive, "metadata.json", MaxMetadataBytes);
        var manifest = JsonSerializer.Deserialize<MetadataBackupManifest>(manifestBytes, JsonOptions)
            ?? throw new InvalidDataException(L.T("อ่าน Manifest ไม่สำเร็จ", "The backup manifest is invalid."));

        if (!string.Equals(manifest.Product, "Edge Workspace Manager", StringComparison.Ordinal) ||
            manifest.FormatVersion != CurrentFormatVersion)
            throw new InvalidDataException(L.T("โปรแกรมไม่รองรับรูปแบบ Backup นี้", "This backup format is not supported."));
        var actualHash = Convert.ToHexString(SHA256.HashData(metadataBytes));
        if (!string.Equals(actualHash, manifest.MetadataSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(L.T("ข้อมูล Backup ไม่ผ่านการตรวจสอบ SHA-256", "Backup SHA-256 verification failed."));

        var data = JsonSerializer.Deserialize<MetadataBackupData>(metadataBytes, JsonOptions)
            ?? throw new InvalidDataException(L.T("อ่านข้อมูล Backup ไม่สำเร็จ", "The backup metadata is invalid."));
        if (data.Workspaces.Count == 0 || data.Workspaces.Count != manifest.InstanceCount ||
            data.Workspaces.Sum(workspace => workspace.Tabs.Count) != manifest.TabCount ||
            data.Favorites.Count != manifest.FavoriteCount)
            throw new InvalidDataException(L.T("จำนวนข้อมูลใน Backup ไม่ตรงกับ Manifest", "Backup item counts do not match the manifest."));

        var idMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var imported = data.Workspaces.Select(workspace =>
        {
            var clone = CloneWorkspace(workspace);
            var oldId = clone.Id;
            clone.Id = Guid.NewGuid().ToString("N");
            idMap[oldId] = clone.Id;
            foreach (var tab in clone.Tabs) tab.Id = Guid.NewGuid().ToString("N");
            return clone;
        }).ToList();
        var favorites = data.Favorites.Select(favorite =>
        {
            var clone = CloneFavorite(favorite);
            clone.Id = Guid.NewGuid().ToString("N");
            clone.WorkspaceId = idMap.TryGetValue(favorite.WorkspaceId, out var newId) ? newId : "";
            return clone;
        }).ToList();

        return new ImportedMetadataBackup { Manifest = manifest, Workspaces = imported, Favorites = favorites };
    }

    private static void WriteEntry(ZipArchive archive, string name, byte[] bytes)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(bytes);
    }

    private static byte[] ReadRequiredEntry(ZipArchive archive, string name, long maxBytes)
    {
        var entries = archive.Entries.Where(entry => string.Equals(entry.FullName, name, StringComparison.Ordinal)).ToList();
        if (entries.Count != 1 || entries[0].Length < 1 || entries[0].Length > maxBytes)
            throw new InvalidDataException(L.T($"ไฟล์ {name} ใน Backup ไม่ถูกต้อง", $"Backup entry {name} is invalid."));
        using var stream = entries[0].Open();
        using var memory = new MemoryStream((int)entries[0].Length);
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static BrowserWorkspace CloneWorkspace(BrowserWorkspace workspace) => new()
    {
        Id = workspace.Id,
        Name = workspace.Name,
        ProfileFolder = workspace.ProfileFolder,
        ColorHex = workspace.ColorHex,
        Tabs = workspace.Tabs.Select(tab => new BrowserTab
        {
            Id = tab.Id,
            Name = tab.Name,
            Url = tab.Url,
            CurrentUrl = tab.CurrentUrl,
            IsOpen = tab.IsOpen,
            IsTemporary = tab.IsTemporary,
            IsPinned = tab.IsPinned
        }).ToList()
    };

    private static FavoriteItem CloneFavorite(FavoriteItem favorite) => new()
    {
        Id = favorite.Id,
        WorkspaceId = favorite.WorkspaceId,
        Title = favorite.Title,
        Url = favorite.Url,
        AddedAt = favorite.AddedAt
    };
}
