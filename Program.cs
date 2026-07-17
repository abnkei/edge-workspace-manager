using System.Text.Json;
using System.Reflection;

namespace EdgeWorkspaceManager;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public static class AppInfo
{
    public static string Version => Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "1.7.1";

    public static IReadOnlyList<(string Version, string Date, string[] Changes)> ReleaseNotes { get; } =
    [
        ("1.7.1", "17 กรกฎาคม 2026",
        [
            "Test Release สำหรับตรวจสอบ Official Public Update จาก v1.7.0",
            "ทดสอบการแจ้งเตือน ดาวน์โหลด ตรวจ SHA-256 และเรียก Updater",
            "ทดสอบการบันทึก Update History และเปิดโปรแกรมกลับหลังติดตั้ง"
        ]),
        ("1.7.0", "17 กรกฎาคม 2026",
        [
            "เพิ่ม Official Public Update ผ่าน GitHub Releases แบบไม่ต้อง Login",
            "เพิ่มหน้าต่าง Update now, Remind me later และ Skip this version",
            "เพิ่มการดาวน์โหลดพร้อมแสดงความคืบหน้าและตรวจสอบ SHA-256",
            "เพิ่ม Updater แยกสำหรับติดตั้งและเปิดโปรแกรมกลับอัตโนมัติ",
            "เพิ่ม Check for updates, Update History และตัวเลือกปิดการตรวจอัปเดตอัตโนมัติ"
        ]),
        ("1.6.1", "17 กรกฎาคม 2026",
        [
            "แก้ปัญหาคลิกขวาที่หัว Tab แล้วเมนู Pin Tab ไม่แสดง",
            "เพิ่มปุ่ม Pin/Unpin Tab บน Toolbar พร้อม Tooltip",
            "ปุ่ม Pin เปลี่ยนสถานะตาม Tab ปัจจุบันและปิดใช้งานสำหรับ External Program Tab",
            "เพิ่มคีย์ลัด Ctrl+Shift+P สำหรับ Pin หรือ Unpin Tab ปัจจุบัน"
        ]),
        ("1.6.0", "17 กรกฎาคม 2026",
        [
            "แก้หน้า Settings ให้ตรึงปุ่ม Save และเลื่อนรายการได้เมื่อพื้นที่ไม่พอ",
            "เพิ่ม Duplicate Tab ให้สร้างถัดจาก Tab ต้นฉบับด้วย Ctrl+Shift+D",
            "เพิ่ม Pin/Unpin Tab พร้อมจำสถานะและจัด Pinned Tab ไว้ด้านซ้าย",
            "ซ่อนปุ่มปิดของ Pinned Tab และถามยืนยันก่อนปิด",
            "จำกัดการลากเรียงไม่ให้ Tab ปกติแทรกในกลุ่ม Pinned"
        ]),
        ("1.5.1", "17 กรกฎาคม 2026",
        [
            "แก้ Tab จาก v1.4.0 ไม่แสดงหลังเปิดด้วย v1.5.0 แม้ข้อมูล URL ยังอยู่",
            "แยก snapshot รายการ Tab ที่เปิดต่อ Workspace ออกจาก IsOpen flag",
            "เพิ่ม migration กู้คืน Workspace ที่ Tab ถูกบันทึกเป็นปิดทั้งหมดโดยอัตโนมัติ",
            "ป้องกัน Session ของ Workspace หนึ่งเขียนทับสถานะ Tab ของ Workspace อื่น"
        ]),
        ("1.5.0", "17 กรกฎาคม 2026",
        [
            "เพิ่ม Theme แบบ Light, Dark และ Use Windows setting",
            "ใช้ Dark Mode กับ Toolbar, Address Bar, Tabs, Menus, Status, Dialog และตารางข้อมูล",
            "ติดตามการเปลี่ยน Theme ของ Windows และปรับ UI อัตโนมัติ",
            "เปลี่ยน Theme โดยไม่ Reload WebView และไม่กระทบ Login หรือ Session",
            "เพิ่ม Force dark web pages แบบแยกต่างหากและมีผลเมื่อเปิดโปรแกรมใหม่",
            "เชื่อม WebView2 Preferred Color Scheme สำหรับเว็บไซต์ที่รองรับ Dark Mode"
        ]),
        ("1.4.0", "17 กรกฎาคม 2026",
        [
            "เพิ่ม Address Bar Suggestions จาก History, Favorites และ Tab ที่เปิดอยู่",
            "จำกัดคำแนะนำใน Instance ปัจจุบันและเลือกค้นหาทุก Instance จาก Settings ได้",
            "รองรับ Up/Down, Enter, Esc และการคลิกเพื่อเลือกคำแนะนำ",
            "เพิ่มสีเน้นและเส้นใต้สำหรับ Tab ที่กำลัง Focus พร้อม Contrast อัตโนมัติ",
            "กำหนดหรือคืนค่า Focused Tab Color ได้จาก Settings"
        ]),
        ("1.3.1", "17 กรกฎาคม 2026",
        [
            "แก้ Race Condition ที่ทำให้ Tab ของ Instance ลำดับหลังหายระหว่างเปิดโปรแกรม",
            "เก็บ snapshot รายการ Tab ที่เปิดก่อนเริ่มสร้าง WebView ทุก Instance",
            "ป้องกันการบันทึก Session ระหว่าง Reload และบันทึกครั้งเดียวหลังโหลดครบ"
        ]),
        ("1.3.0", "17 กรกฎาคม 2026",
        [
            "ลากสลับลำดับ Tab และ Instance พร้อมบันทึกลำดับถาวรโดยไม่ Reload WebView",
            "กำหนดสีประจำ Instance พร้อมเลือกสีข้อความตาม Contrast อัตโนมัติ",
            "เพิ่มภาษาไทยและ English ใน Settings และหน้าจอหลักของโปรแกรม",
            "เพิ่ม Keep Awake ป้องกัน Sleep เลือกเปิดจอค้างและตั้งเวลาปิดอัตโนมัติ",
            "Keep Awake ใช้ Windows Power API โดยไม่จำลอง Mouse หรือ Keyboard"
        ]),
        ("1.2.1", "17 กรกฎาคม 2026",
        [
            "ปุ่มปิด Tab และ Ctrl+W สามารถนำหน้าต่างโปรแกรมภายนอกกลับสู่ Desktop ได้",
            "เพิ่มคำสั่งปิดหน้าต่างโปรแกรมภายนอก พร้อมข้อความยืนยันก่อนปิด",
            "คืนหน้าต่างสู่ Desktop ก่อนส่งคำสั่งปิด เพื่อรองรับหน้าต่างถามบันทึกงาน"
        ]),
        ("1.2.0", "17 กรกฎาคม 2026",
        [
            "เพิ่ม Favorites และ History พร้อมแยกข้อมูลตาม Workspace",
            "เพิ่ม Download Manager พร้อมความคืบหน้า พัก ทำต่อ ยกเลิก เปิดไฟล์และโฟลเดอร์",
            "เพิ่มแถบ Find in Page พร้อมค้นหาก่อนหน้าและถัดไป",
            "เพิ่ม Zoom 25–500% และคีย์ลัด Ctrl++, Ctrl+- และ Ctrl+0",
            "เพิ่ม Print Preview และ Save PDF ผ่าน Ctrl+P",
            "เพิ่ม Settings สำหรับ Search Engine, Download Folder, DevTools และจำนวน History",
            "เพิ่มเมนูเครื่องมือ Browser และโหมด Full Screen"
        ]),
        ("1.1.1", "17 กรกฎาคม 2026",
        [
            "หน้าจัดการ Tab แสดงชื่อ Document Title และ URL จากหน้าเว็บที่เปิดจริง",
            "สร้างข้อมูลชั่วคราวสำหรับหน้าจัดการ เพื่อไม่เปลี่ยนชื่อ Tab เมื่อกดยกเลิก"
        ]),
        ("1.1.0", "17 กรกฎาคม 2026",
        [
            "Smart Search และคำค้นหาลัด Google, Bing, YouTube และ Maps",
            "เปิด ปิด ทำสำเนา และคืน Tab ที่ปิดล่าสุด",
            "บันทึก Session และเปิดต่อจาก URL ล่าสุด",
            "รองรับ Popup และลิงก์ที่เปิดเป็น Tab ใหม่",
            "หน้าจัดการ Tab อ่านรายการและ URL จาก Tab ที่เปิดอยู่จริง",
            "รักษา WebView และหน้าต่างโปรแกรมภายนอกขณะบันทึกการจัดการ Tab",
            "เพิ่มคีย์ลัดแบบ Browser"
        ]),
        ("1.0.0", "17 กรกฎาคม 2026",
        [
            "รุ่นเริ่มต้นของ Edge Workspace Manager",
            "Workspace แยก Profile, Cookie, Login และ Session",
            "รองรับ WebView2 และการฝังหน้าต่างโปรแกรมภายนอก"
        ])
    ];
}

public sealed class WorkspaceConfig
{
    public int SessionRecoveryVersion { get; set; }
    public Dictionary<string, List<string>> OpenTabIdsByWorkspace { get; set; } = new();
    public List<BrowserWorkspace> Workspaces { get; set; } = new();
    public List<ClosedBrowserTab> RecentlyClosedTabs { get; set; } = new();
    public bool RestoreLastSession { get; set; } = true;
    public List<FavoriteItem> Favorites { get; set; } = new();
    public List<HistoryItem> History { get; set; } = new();
    public BrowserSettings Settings { get; set; } = new();
    public List<UpdateHistoryItem> UpdateHistory { get; set; } = new();
}

public sealed class BrowserWorkspace
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Edge Instance";
    public string ProfileFolder { get; set; } = "Instance01";
    public List<BrowserTab> Tabs { get; set; } = new();
    public string ColorHex { get; set; } = "";
}

public sealed class BrowserTab
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Tab";
    public string Url { get; set; } = "https://www.google.com";
    public string? CurrentUrl { get; set; }
    public bool IsOpen { get; set; } = true;
    public bool IsTemporary { get; set; }
    public bool IsPinned { get; set; }
}

public sealed class ClosedBrowserTab
{
    public string WorkspaceId { get; set; } = "";
    public BrowserTab Tab { get; set; } = new();
    public DateTime ClosedAt { get; set; } = DateTime.Now;
}

public sealed class FavoriteItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string WorkspaceId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime AddedAt { get; set; } = DateTime.Now;
}

public sealed class HistoryItem
{
    public string WorkspaceId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime VisitedAt { get; set; } = DateTime.Now;
}

public sealed class BrowserSettings
{
    public string SearchEngine { get; set; } = "Google";
    public string DownloadFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public bool AskWhereToSaveDownloads { get; set; }
    public bool DevToolsEnabled { get; set; } = true;
    public int MaxHistoryItems { get; set; } = 2000;
    public string Language { get; set; } = "th";
    public bool AddressSuggestionsEnabled { get; set; } = true;
    public bool SearchSuggestionsAcrossInstances { get; set; }
    public string FocusedTabColorHex { get; set; } = "#0078D4";
    public string Theme { get; set; } = "System";
    public bool ForceDarkWebPages { get; set; }
    public bool CheckForUpdates { get; set; } = true;
    public DateTime? LastUpdateCheckUtc { get; set; }
    public DateTime? RemindUpdateAfterUtc { get; set; }
    public string SkippedUpdateVersion { get; set; } = "";
}

public sealed class UpdateHistoryItem
{
    public string Version { get; set; } = "";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
}

public sealed class DownloadRecord
{
    public string FileName { get; set; } = "";
    public string Path { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string Status { get; set; } = "กำลังดาวน์โหลด";
    public double Progress { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public Action? Pause { get; set; }
    public Action? Resume { get; set; }
    public Action? Cancel { get; set; }
}

public static class ConfigStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string AppFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EdgeWorkspaceManager");

    public static string ConfigPath => Path.Combine(AppFolder, "workspaces.json");
    public static string ProfilesFolder => Path.Combine(AppFolder, "Profiles");

    public static WorkspaceConfig Load()
    {
        try
        {
            Directory.CreateDirectory(AppFolder);
            Directory.CreateDirectory(ProfilesFolder);

            if (!File.Exists(ConfigPath))
            {
                var sample = CreateSample();
                Save(sample);
                return sample;
            }

            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<WorkspaceConfig>(json, Options) ?? CreateSample();
            if (config.SessionRecoveryVersion < 2)
            {
                foreach (var workspace in config.Workspaces.Where(workspace =>
                             workspace.Tabs.Count > 0 && workspace.Tabs.All(tab => !tab.IsOpen)))
                    foreach (var tab in workspace.Tabs)
                        tab.IsOpen = true;
                config.OpenTabIdsByWorkspace = config.Workspaces.ToDictionary(
                    workspace => workspace.Id,
                    workspace => workspace.Tabs.Where(tab => tab.IsOpen).Select(tab => tab.Id).ToList());
                config.SessionRecoveryVersion = 2;
                Save(config);
            }
            return config;
        }
        catch
        {
            return CreateSample();
        }
    }

    public static void Save(WorkspaceConfig config)
    {
        Directory.CreateDirectory(AppFolder);
        Directory.CreateDirectory(ProfilesFolder);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, Options));
    }

    private static WorkspaceConfig CreateSample() => new()
    {
        SessionRecoveryVersion = 2,
        Workspaces = new List<BrowserWorkspace>
        {
            new()
            {
                Name = "Edge Instance 1",
                ProfileFolder = "Instance01",
                Tabs = new List<BrowserTab>
                {
                    new() { Name = "Tab 1", Url = "https://www.google.com" },
                    new() { Name = "Tab 2", Url = "https://www.microsoft.com" }
                }
            },
            new()
            {
                Name = "Edge Instance 2",
                ProfileFolder = "Instance02",
                Tabs = new List<BrowserTab>
                {
                    new() { Name = "Tab 1", Url = "https://www.bing.com" }
                }
            }
        }
    };
}
