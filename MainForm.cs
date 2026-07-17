using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.ComponentModel;
using System.Text.Json;

namespace EdgeWorkspaceManager;

public sealed class MainForm : Form
{
    private const int CloseButtonWidth = 22;
    private WorkspaceConfig _config = ConfigStore.Load();
    private readonly TabControl _workspaceTabs = new() { Dock = DockStyle.Fill };
    private readonly TextBox _address = new() { Dock = DockStyle.Fill };
    private readonly ListBox _addressSuggestions = new()
    {
        Visible = false,
        IntegralHeight = false,
        Height = 190,
        Font = new Font("Segoe UI", 10F),
        BorderStyle = BorderStyle.FixedSingle
    };
    private readonly System.Windows.Forms.Timer _suggestionTimer = new() { Interval = 160 };
    private readonly ToolStripStatusLabel _status = new("พร้อมใช้งาน");
    private readonly Dictionary<string, CoreWebView2Environment> _environments = new();
    private readonly BindingList<DownloadRecord> _downloads = new();
    private readonly Dictionary<TabControl, int> _tabDragStart = new();
    private readonly PowerAwake _powerAwake = new();
    private readonly UpdateService _updateService = new();
    private readonly System.Windows.Forms.Timer _awakeTimer = new() { Interval = 1000 };
    private int _workspaceDragStart = -1;
    private bool _syncingAddress;
    private bool _addressUserEditing;
    private Button? _windowPickerButton;
    private Button? _pinTabButton;
    private readonly ToolTip _pinTabToolTip = new();
    private bool _isReloading;

    public MainForm()
    {
        L.SetLanguage(_config.Settings.Language);
        ThemeManager.Configure(_config.Settings.Theme);
        Text = L.T($"Edge Workspace Manager v{AppInfo.Version} - พื้นที่ทำงานแบบ Tab", $"Edge Workspace Manager v{AppInfo.Version} - Tabbed Workspace");
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1000, 650);
        Font = new Font("Segoe UI", 10F);
        KeyPreview = true;
        BuildLayout();
        ThemeManager.Apply(this);
        Shown += async (_, _) =>
        {
            ImportUpdateResult();
            await ReloadWorkspaceAsync();
            CompleteUpdateHealthCheck();
            if (!string.IsNullOrWhiteSpace(AppInfo.UpdateHealthPath))
                await ImportUpdateResultWhenAvailableAsync();
            await CheckForUpdatesAsync(false);
        };
        _awakeTimer.Tick += (_, _) => { CheckAwakeTimeout(); CheckSystemTheme(); };
        _awakeTimer.Start();
        _suggestionTimer.Tick += (_, _) => { _suggestionTimer.Stop(); UpdateAddressSuggestions(); };
        FormClosing += (_, _) => { _powerAwake.Dispose(); SaveSession(); };
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        var bar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 14, Padding = new Padding(6) };
        for (var i = 0; i < 11; i++) bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));

        bar.Controls.Add(MakeButton("←", (_, _) => GoBack(), L.T("ย้อนกลับ (Alt+Left)", "Back (Alt+Left)")), 0, 0);
        bar.Controls.Add(MakeButton("→", (_, _) => GoForward(), L.T("ถัดไป (Alt+Right)", "Forward (Alt+Right)")), 1, 0);
        bar.Controls.Add(MakeButton("⟳", (_, _) => CurrentWebView()?.Reload(), L.T("รีเฟรช (Ctrl+R)", "Reload (Ctrl+R)")), 2, 0);
        bar.Controls.Add(MakeButton("⌂", (_, _) => NavigateHome(), L.T("หน้าแรกของ Tab", "Tab home page")), 3, 0);
        bar.Controls.Add(MakeButton("＋", async (_, _) => await AddNewTabAsync(), L.T("เปิด Tab ใหม่ (Ctrl+T)", "New tab (Ctrl+T)")), 4, 0);
        bar.Controls.Add(MakeButton("↶", async (_, _) => await RestoreClosedTabAsync(), L.T("คืน Tab ที่ปิด (Ctrl+Shift+T)", "Reopen closed tab (Ctrl+Shift+T)")), 5, 0);
        bar.Controls.Add(MakeButton("▣", (_, _) => AddWorkspace(), L.T("เพิ่ม Workspace", "Add workspace")), 6, 0);
        _windowPickerButton = MakeButton("⊕", (_, _) => { }, "กดค้างแล้วลากไปปล่อยบนหน้าต่างโปรแกรม");
        _windowPickerButton.MouseDown += WindowPickerMouseDown;
        _windowPickerButton.MouseUp += WindowPickerMouseUp;
        bar.Controls.Add(_windowPickerButton, 7, 0);
        bar.Controls.Add(MakeButton("☆", (_, _) => AddCurrentFavorite(), L.T("เพิ่มหน้าเว็บปัจจุบันใน Favorites", "Add current page to Favorites")), 8, 0);
        bar.Controls.Add(MakeButton("⋯", (_, _) => ShowBrowserMenu(), L.T("เครื่องมือ Browser", "Browser tools")), 9, 0);
        _pinTabButton = MakeButton("📌", (_, _) => ToggleCurrentPinnedTab(), L.T("ปักหมุด/เลิกปักหมุด Tab ปัจจุบัน (Ctrl+Shift+P)", "Pin/Unpin current tab (Ctrl+Shift+P)"));
        _pinTabButton.AccessibleName = L.T("ปักหมุด Tab ปัจจุบัน", "Pin current tab");
        bar.Controls.Add(_pinTabButton, 10, 0);
        _address.KeyDown += AddressKeyDown;
        _address.TextChanged += (_, _) =>
        {
            if (!_syncingAddress && _address.Focused) _addressUserEditing = true;
            ScheduleAddressSuggestions();
        };
        _address.Leave += async (_, _) =>
        {
            await Task.Delay(150);
            if (!_addressSuggestions.ContainsFocus)
            {
                _addressUserEditing = false;
                HideAddressSuggestions();
                SyncAddress();
            }
        };
        bar.Controls.Add(_address, 11, 0);
        bar.Controls.Add(MakeButton(L.T("ไป", "Go"), (_, _) => NavigateAddress()), 12, 0);
        bar.Controls.Add(MakeButton(L.T("จัดการ Tab", "Manage tabs"), async (_, _) => await EditCurrentWorkspaceAsync(), width: 135), 13, 0);

        _workspaceTabs.SelectedIndexChanged += (_, _) =>
        {
            _addressUserEditing = false;
            SyncAddress();
        };
        _workspaceTabs.AllowDrop = true;
        _workspaceTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _workspaceTabs.DrawItem += DrawWorkspaceTab;
        _workspaceTabs.MouseDown += WorkspaceTabsMouseDown;
        _workspaceTabs.MouseMove += WorkspaceTabsMouseMove;
        _workspaceTabs.DragOver += WorkspaceTabsDragOver;
        _workspaceTabs.DragDrop += WorkspaceTabsDragDrop;
        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_status);
        statusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });
        var versionLink = new ToolStripStatusLabel($"เวอร์ชัน {AppInfo.Version}  •  ข้อมูลอัปเดต")
        {
            IsLink = true,
            ToolTipText = "ดูรายการเปลี่ยนแปลงของแต่ละเวอร์ชัน"
        };
        versionLink.Click += (_, _) =>
        {
            using var about = new AboutForm(() => CheckForUpdatesAsync(true), ShowUpdateHistory);
            about.ShowDialog(this);
        };
        statusStrip.Items.Add(versionLink);
        root.Controls.Add(bar, 0, 0);
        root.Controls.Add(_workspaceTabs, 0, 1);
        root.Controls.Add(statusStrip, 0, 2);
        Controls.Add(root);
        _addressSuggestions.MouseClick += (_, _) => OpenSelectedAddressSuggestion();
        Controls.Add(_addressSuggestions);
        _addressSuggestions.BringToFront();
    }

    private static Button MakeButton(string text, EventHandler click, string? tip = null, int width = 36)
    {
        var button = new Button { Text = text, Dock = DockStyle.Fill, Margin = new Padding(2), Width = width };
        button.Click += click;
        if (!string.IsNullOrWhiteSpace(tip)) new ToolTip().SetToolTip(button, tip);
        return button;
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (!manual)
        {
            if (!_config.Settings.CheckForUpdates) return;
            if (_config.Settings.LastUpdateCheckUtc is { } last && DateTime.UtcNow - last < TimeSpan.FromHours(6)) return;
            if (_config.Settings.RemindUpdateAfterUtc is { } remind && remind > DateTime.UtcNow) return;
        }
        try
        {
            _status.Text = L.T("กำลังตรวจอัปเดต...", "Checking for updates...");
            var update = await _updateService.CheckAsync();
            _config.Settings.LastUpdateCheckUtc = DateTime.UtcNow;
            ConfigStore.Save(_config);
            if (update is null)
            {
                _status.Text = L.T("โปรแกรมเป็นเวอร์ชันล่าสุด", "The app is up to date");
                if (manual) MessageBox.Show(this, _status.Text, L.T("ตรวจอัปเดต", "Check for updates"));
                return;
            }
            if (!manual && string.Equals(update.Version, _config.Settings.SkippedUpdateVersion, StringComparison.OrdinalIgnoreCase)) return;
            using var prompt = new UpdatePromptForm(update);
            if (prompt.ShowDialog(this) != DialogResult.OK) return;
            if (prompt.Choice == UpdateChoice.RemindLater)
            {
                _config.Settings.RemindUpdateAfterUtc = DateTime.UtcNow.AddHours(24);
                AddUpdateHistory(update.Version, "Deferred", L.T("เตือนอีกครั้งใน 24 ชั่วโมง", "Reminder postponed for 24 hours"));
                return;
            }
            if (prompt.Choice == UpdateChoice.SkipVersion)
            {
                _config.Settings.SkippedUpdateVersion = update.Version;
                AddUpdateHistory(update.Version, "Skipped", L.T("ผู้ใช้ข้ามเวอร์ชันนี้", "Version skipped by user"));
                return;
            }
            if (prompt.Choice == UpdateChoice.UpdateNow)
            {
                using var download = new UpdateDownloadForm(_updateService, update);
                if (download.ShowDialog(this) == DialogResult.OK && download.StagingPath is not null)
                {
                    SaveSession();
                    AddUpdateHistory(update.Version, "Installing", L.T("ดาวน์โหลดและตรวจสอบไฟล์แล้ว", "Package downloaded and verified"));
                    UpdateService.LaunchUpdater(download.StagingPath, update.Version);
                    Close();
                }
                else AddUpdateHistory(update.Version, "Failed", download.FailureMessage ??
                    L.T("ดาวน์โหลดหรือตรวจสอบไฟล์ไม่สำเร็จ", "Download or verification failed"));
            }
        }
        catch (Exception ex)
        {
            UpdateService.WriteLog($"Update check failed: {ex}");
            _status.Text = L.T("ไม่สามารถตรวจอัปเดตได้", "Unable to check for updates");
            if (manual) MessageBox.Show(this, UpdateService.FriendlyError(ex), _status.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AddUpdateHistory(string version, string status, string message)
    {
        _config.UpdateHistory.Add(new UpdateHistoryItem { Version = version, Status = status, Message = message });
        if (_config.UpdateHistory.Count > 100) _config.UpdateHistory.RemoveRange(0, _config.UpdateHistory.Count - 100);
        ConfigStore.Save(_config);
    }

    private void ShowUpdateHistory()
    {
        using var history = new UpdateHistoryForm(_config.UpdateHistory);
        history.ShowDialog(this);
    }

    private void ImportUpdateResult()
    {
        var path = Path.Combine(ConfigStore.AppFolder, "Updates", "update-result.txt");
        if (!File.Exists(path)) return;
        try
        {
            var parts = File.ReadAllText(path).Split('|', 3);
            if (parts.Length >= 2) AddUpdateHistory(parts[0], parts[1], parts.Length > 2 ? parts[2] : "");
            File.Delete(path);
        }
        catch { }
    }

    private void CompleteUpdateHealthCheck()
    {
        if (string.IsNullOrWhiteSpace(AppInfo.UpdateHealthPath)) return;
        try
        {
            var path = Path.GetFullPath(AppInfo.UpdateHealthPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, $"{AppInfo.Version}|Healthy|{DateTimeOffset.UtcNow:O}");
            _status.Text = L.T("อัปเดตและกู้คืน Session สำเร็จ", "Update and session recovery completed");
        }
        catch (Exception ex)
        {
            UpdateService.WriteLog($"Could not write update health check: {ex}");
        }
    }

    private async Task ImportUpdateResultWhenAvailableAsync()
    {
        var path = Path.Combine(ConfigStore.AppFolder, "Updates", "update-result.txt");
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (File.Exists(path))
            {
                ImportUpdateResult();
                return;
            }
            await Task.Delay(250);
        }
        UpdateService.WriteLog("The application became healthy, but the updater result was not available within 5 seconds.");
    }

    private async Task ReloadWorkspaceAsync()
    {
        if (_isReloading) return;
        _isReloading = true;
        var openTabIds = _config.Workspaces.ToDictionary(
            workspace => workspace.Id,
            workspace => (_config.OpenTabIdsByWorkspace.TryGetValue(workspace.Id, out var savedIds)
                    ? savedIds
                    : workspace.Tabs.Where(tab => tab.IsOpen).Select(tab => tab.Id))
                .ToHashSet(StringComparer.Ordinal));
        var loadCompleted = false;
        try
        {
            _workspaceTabs.TabPages.Clear();
            _status.Text = "กำลังเปิด Workspace...";
            foreach (var workspace in _config.Workspaces)
            {
                var outer = new TabPage(workspace.Name) { Tag = workspace };
                var inner = CreateBrowserTabControl();
                outer.Controls.Add(inner);
                _workspaceTabs.TabPages.Add(outer);
                var tabs = workspace.Tabs.Where(tab =>
                    !_config.RestoreLastSession ||
                    (openTabIds.TryGetValue(workspace.Id, out var ids) && ids.Contains(tab.Id)))
                    .OrderByDescending(tab => tab.IsPinned).ToList();
                if (!_config.RestoreLastSession) tabs = workspace.Tabs.OrderByDescending(tab => tab.IsPinned).ToList();
                foreach (var tab in tabs)
                    await CreateBrowserTabPageAsync(inner, workspace, tab, false);
            }
            _status.Text = "พร้อมใช้งาน";
            SyncAddress();
            loadCompleted = true;
        }
        finally
        {
            _isReloading = false;
            if (loadCompleted) SaveSession();
        }
    }

    private TabControl CreateBrowserTabControl()
    {
        var inner = new TabControl
        {
            Dock = DockStyle.Fill,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            Padding = new Point(CloseButtonWidth, 4),
            ItemSize = new Size(190, 30),
            SizeMode = TabSizeMode.Normal,
            ShowToolTips = true
        };
        inner.AllowDrop = true;
        inner.DrawItem += DrawBrowserTab;
        inner.MouseDown += BrowserTabsMouseDown;
        inner.MouseMove += BrowserTabsMouseMove;
        inner.MouseUp += BrowserTabsMouseUp;
        inner.DragOver += BrowserTabsDragOver;
        inner.DragDrop += BrowserTabsDragDrop;
        inner.SelectedIndexChanged += (_, _) =>
        {
            _addressUserEditing = false;
            SyncAddress();
        };
        return inner;
    }

    private async Task<TabPage> CreateBrowserTabPageAsync(TabControl inner, BrowserWorkspace workspace, BrowserTab tab, bool select = true)
    {
        tab.IsOpen = true;
        var page = new TabPage { Tag = tab };
        UpdateBrowserTabCaption(page, tab);
        page.ContextMenuStrip = BuildBrowserTabMenu(inner, page);
        inner.TabPages.Add(page);
        if (select) inner.SelectedTab = page;
        await AddBrowserAsync(page, workspace, tab);
        return page;
    }

    private async Task AddBrowserAsync(TabPage page, BrowserWorkspace workspace, BrowserTab tab)
    {
        try
        {
            var browser = new WebView2 { Dock = DockStyle.Fill, Tag = tab };
            page.Controls.Add(browser);
            page.Controls.Add(BuildFindBar(browser));
            if (!_environments.TryGetValue(workspace.ProfileFolder, out var environment))
            {
                var profilePath = Path.Combine(ConfigStore.ProfilesFolder, Sanitize(workspace.ProfileFolder));
                Directory.CreateDirectory(profilePath);
                var options = new CoreWebView2EnvironmentOptions();
                if (_config.Settings.ForceDarkWebPages)
                    options.AdditionalBrowserArguments = "--enable-features=WebContentsForceDark";
                environment = await CoreWebView2Environment.CreateAsync(null, profilePath, options);
                _environments[workspace.ProfileFolder] = environment;
            }
            await browser.EnsureCoreWebView2Async(environment);
            browser.DefaultBackgroundColor = ThemeManager.WindowBack;
            browser.CoreWebView2.Profile.PreferredColorScheme = ThemeManager.IsDark
                ? CoreWebView2PreferredColorScheme.Dark
                : CoreWebView2PreferredColorScheme.Light;
            browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            browser.CoreWebView2.Settings.AreDevToolsEnabled = _config.Settings.DevToolsEnabled;
            browser.CoreWebView2.DocumentTitleChanged += (_, _) => BeginInvoke(() =>
            {
                var title = browser.CoreWebView2.DocumentTitle;
                if (!string.IsNullOrWhiteSpace(title)) UpdateBrowserTabCaption(page, tab, title);
            });
            browser.CoreWebView2.SourceChanged += (_, _) => BeginInvoke(() => WebViewSourceChanged(browser, tab));
            browser.CoreWebView2.NavigationStarting += (_, _) => _status.Text = $"กำลังเปิด {tab.Name}...";
            browser.CoreWebView2.NavigationCompleted += (_, e) =>
            {
                tab.CurrentUrl = browser.Source?.ToString();
                _status.Text = e.IsSuccess ? "พร้อมใช้งาน" : $"เปิดหน้าไม่สำเร็จ: {e.WebErrorStatus}";
                if (e.IsSuccess) AddHistory(workspace, browser);
                SyncAddressFrom(browser);
                SaveSession();
            };
            browser.CoreWebView2.DownloadStarting += (_, e) => TrackDownload(e);
            browser.CoreWebView2.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                BeginInvoke(async () => await AddNewTabAsync(e.Uri));
            };
            var startUrl = _config.RestoreLastSession ? tab.CurrentUrl ?? tab.Url : tab.Url;
            browser.Source = new Uri(ToNavigationUrl(startUrl));
        }
        catch (Exception ex)
        {
            page.Controls.Add(new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Text = "ไม่สามารถเปิด WebView2 ได้\n\n" + ex.Message });
        }
    }

    private async Task AddNewTabAsync(string? value = null)
    {
        var workspace = CurrentWorkspace();
        var inner = CurrentInnerTabs();
        if (workspace is null || inner is null)
        {
            MessageBox.Show("กรุณาสร้างหรือเลือก Workspace ก่อน", "แจ้งเตือน");
            return;
        }
        var url = string.IsNullOrWhiteSpace(value) ? "https://www.google.com" : ToNavigationUrl(value);
        var tab = new BrowserTab { Name = "New Tab", Url = url, CurrentUrl = url, IsTemporary = true, IsOpen = true };
        workspace.Tabs.Add(tab);
        await CreateBrowserTabPageAsync(inner, workspace, tab);
        ConfigStore.Save(_config);
        _address.Focus();
        _address.SelectAll();
    }

    private void CloseTab(TabControl inner, TabPage page, bool remember = true)
    {
        if (page.Controls.OfType<ExternalWindowHost>().FirstOrDefault() is { } externalHost)
        {
            externalHost.Detach();
            inner.TabPages.Remove(page);
            page.Dispose();
            _status.Text = "นำหน้าต่างโปรแกรมกลับออกมาแล้ว";
            SyncAddress();
            return;
        }
        if (page.Tag is not BrowserTab tab) return;
        var workspace = WorkspaceFor(inner);
        if (workspace is null) return;
        if (tab.IsPinned && MessageBox.Show(
                L.T("แท็บนี้ถูกปักหมุดไว้ ต้องการปิดหรือไม่?", "This tab is pinned. Close it?"),
                L.T("ยืนยันปิด Tab", "Confirm close tab"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        tab.CurrentUrl = page.Controls.OfType<WebView2>().FirstOrDefault()?.Source?.ToString() ?? tab.CurrentUrl;
        tab.IsOpen = false;
        if (remember)
        {
            _config.RecentlyClosedTabs.Insert(0, new ClosedBrowserTab { WorkspaceId = workspace.Id, Tab = CloneTab(tab) });
            if (_config.RecentlyClosedTabs.Count > 50)
                _config.RecentlyClosedTabs.RemoveRange(50, _config.RecentlyClosedTabs.Count - 50);
        }
        inner.TabPages.Remove(page);
        page.Dispose();
        ConfigStore.Save(_config);
        SyncAddress();
    }

    private async Task RestoreClosedTabAsync()
    {
        if (_config.RecentlyClosedTabs.Count == 0)
        {
            _status.Text = "ไม่มี Tab ที่ปิดล่าสุด";
            return;
        }
        var closed = _config.RecentlyClosedTabs[0];
        _config.RecentlyClosedTabs.RemoveAt(0);
        var workspace = _config.Workspaces.FirstOrDefault(w => w.Id == closed.WorkspaceId) ?? CurrentWorkspace();
        if (workspace is null) return;
        SelectWorkspace(workspace);
        var inner = CurrentInnerTabs();
        if (inner is null) return;
        var existing = workspace.Tabs.FirstOrDefault(t => t.Id == closed.Tab.Id);
        var tab = existing ?? CloneTab(closed.Tab);
        if (existing is null) workspace.Tabs.Add(tab);
        tab.IsOpen = true;
        tab.CurrentUrl = closed.Tab.CurrentUrl;
        await CreateBrowserTabPageAsync(inner, workspace, tab);
        ConfigStore.Save(_config);
    }

    private ContextMenuStrip BuildBrowserTabMenu(TabControl inner, TabPage page)
    {
        var menu = new ContextMenuStrip();
        menu.Opening += (_, _) => menu.Items[0].Text = page.Tag is BrowserTab { IsPinned: true }
            ? L.T("เลิกปักหมุด Tab", "Unpin tab")
            : L.T("ปักหมุด Tab", "Pin tab");
        menu.Items.Add(L.T("ปักหมุด Tab", "Pin tab"), null, (_, _) => TogglePinnedTab(inner, page));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L.T("ปิด Tab", "Close tab"), null, (_, _) => CloseTab(inner, page));
        menu.Items.Add(L.T("ปิด Tab อื่น", "Close other tabs"), null, (_, _) =>
        {
            foreach (var other in inner.TabPages.Cast<TabPage>().Where(p => p != page).ToList()) CloseTab(inner, other);
        });
        menu.Items.Add(L.T("ปิด Tab ด้านขวา", "Close tabs to the right"), null, (_, _) =>
        {
            var index = inner.TabPages.IndexOf(page);
            foreach (var other in inner.TabPages.Cast<TabPage>().Skip(index + 1).ToList()) CloseTab(inner, other);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L.T("ทำสำเนา Tab", "Duplicate tab"), null, async (_, _) => await DuplicateTabAsync(inner, page));
        ThemeManager.Apply(menu);
        return menu;
    }

    private void TogglePinnedTab(TabControl inner, TabPage page)
    {
        if (page.Tag is not BrowserTab tab) return;
        tab.IsPinned = !tab.IsPinned;
        UpdateBrowserTabCaption(page, tab);
        inner.TabPages.Remove(page);
        var firstNormal = inner.TabPages.Cast<TabPage>().Count(p => p.Tag is BrowserTab { IsPinned: true });
        inner.TabPages.Insert(firstNormal, page);
        inner.SelectedTab = page;
        PersistBrowserTabOrder(inner);
        inner.Invalidate();
        _status.Text = tab.IsPinned ? L.T("ปักหมุด Tab แล้ว", "Tab pinned") : L.T("เลิกปักหมุด Tab แล้ว", "Tab unpinned");
        UpdatePinButtonState();
    }

    private void ToggleCurrentPinnedTab()
    {
        var inner = CurrentInnerTabs();
        if (inner?.SelectedTab is { Tag: BrowserTab } page) TogglePinnedTab(inner, page);
    }

    private async Task DuplicateCurrentTabAsync()
    {
        var inner = CurrentInnerTabs();
        if (inner?.SelectedTab is { } page) await DuplicateTabAsync(inner, page);
    }

    private async Task DuplicateTabAsync(TabControl inner, TabPage sourcePage)
    {
        if (sourcePage.Tag is not BrowserTab source || WorkspaceFor(inner) is not { } workspace) return;
        var browser = sourcePage.Controls.OfType<WebView2>().FirstOrDefault();
        var currentUrl = browser?.Source?.ToString() ?? source.CurrentUrl ?? source.Url;
        var currentTitle = browser?.CoreWebView2?.DocumentTitle;
        var duplicate = new BrowserTab
        {
            Name = string.IsNullOrWhiteSpace(currentTitle) ? source.Name : currentTitle,
            Url = currentUrl,
            CurrentUrl = currentUrl,
            IsOpen = true,
            IsTemporary = true,
            IsPinned = false
        };
        var sourceModelIndex = workspace.Tabs.IndexOf(source);
        workspace.Tabs.Insert(sourceModelIndex < 0 ? workspace.Tabs.Count : sourceModelIndex + 1, duplicate);
        var duplicatePage = await CreateBrowserTabPageAsync(inner, workspace, duplicate, false);
        inner.TabPages.Remove(duplicatePage);
        var pinnedCount = inner.TabPages.Cast<TabPage>().Count(p => p.Tag is BrowserTab { IsPinned: true });
        var sourceUiIndex = inner.TabPages.IndexOf(sourcePage);
        var target = source.IsPinned ? pinnedCount : Math.Max(pinnedCount, sourceUiIndex + 1);
        inner.TabPages.Insert(Math.Min(target, inner.TabCount), duplicatePage);
        inner.SelectedTab = duplicatePage;
        PersistBrowserTabOrder(inner);
        _status.Text = L.T("ทำสำเนา Tab แล้ว", "Tab duplicated");
    }

    private void DrawBrowserTab(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tabs || e.Index < 0) return;
        var bounds = e.Bounds;
        var selected = e.Index == tabs.SelectedIndex;
        var backgroundColor = selected ? ParseColor(_config.Settings.FocusedTabColorHex, Color.FromArgb(0, 120, 212)) : ThemeManager.ControlBack;
        var foregroundColor = selected && backgroundColor.GetBrightness() < 0.55f ? Color.White : ThemeManager.Foreground;
        using var background = new SolidBrush(backgroundColor);
        e.Graphics.FillRectangle(background, bounds);
        var pinned = tabs.TabPages[e.Index].Tag is BrowserTab { IsPinned: true };
        var textBounds = new Rectangle(bounds.X + 8, bounds.Y + 4, bounds.Width - (pinned ? 8 : CloseButtonWidth + 8), bounds.Height - 6);
        TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, Font, textBounds, foregroundColor,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        if (!pinned)
        {
            var closeBounds = CloseButtonBounds(bounds);
            TextRenderer.DrawText(e.Graphics, "×", new Font(Font.FontFamily, 11F), closeBounds, selected ? foregroundColor : Color.DimGray,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        if (selected)
        {
            using var accent = new Pen(foregroundColor, 3);
            e.Graphics.DrawLine(accent, bounds.Left + 3, bounds.Bottom - 2, bounds.Right - 3, bounds.Bottom - 2);
        }
    }

    private void BrowserTabsMouseDown(object? sender, MouseEventArgs e)
    {
        if (sender is not TabControl tabs) return;
        _tabDragStart[tabs] = -1;
        for (var i = 0; i < tabs.TabCount; i++)
        {
            var bounds = tabs.GetTabRect(i);
            var pinned = tabs.TabPages[i].Tag is BrowserTab { IsPinned: true };
            if ((e.Button == MouseButtons.Left && !pinned && CloseButtonBounds(bounds).Contains(e.Location)) ||
                (e.Button == MouseButtons.Middle && bounds.Contains(e.Location)))
            {
                CloseTab(tabs, tabs.TabPages[i]);
                return;
            }
            if (e.Button == MouseButtons.Left && bounds.Contains(e.Location))
                _tabDragStart[tabs] = i;
        }
    }

    private void BrowserTabsMouseMove(object? sender, MouseEventArgs e)
    {
        if (sender is not TabControl tabs || e.Button != MouseButtons.Left ||
            !_tabDragStart.TryGetValue(tabs, out var index) || index < 0 || index >= tabs.TabCount) return;
        _tabDragStart[tabs] = -1;
        tabs.DoDragDrop(tabs.TabPages[index], DragDropEffects.Move);
    }

    private static void BrowserTabsDragOver(object? sender, DragEventArgs e)
    {
        e.Effect = sender is TabControl && e.Data?.GetDataPresent(typeof(TabPage)) == true
            ? DragDropEffects.Move : DragDropEffects.None;
    }

    private void BrowserTabsDragDrop(object? sender, DragEventArgs e)
    {
        if (sender is not TabControl tabs || e.Data?.GetData(typeof(TabPage)) is not TabPage page || !tabs.TabPages.Contains(page)) return;
        var point = tabs.PointToClient(new Point(e.X, e.Y));
        var target = tabs.TabCount - 1;
        for (var i = 0; i < tabs.TabCount; i++)
            if (tabs.GetTabRect(i).Contains(point)) { target = i; break; }
        var source = tabs.TabPages.IndexOf(page);
        var pinnedCount = tabs.TabPages.Cast<TabPage>().Count(p => p.Tag is BrowserTab { IsPinned: true });
        var isPinned = page.Tag is BrowserTab { IsPinned: true };
        target = isPinned ? Math.Clamp(target, 0, Math.Max(0, pinnedCount - 1)) : Math.Clamp(target, pinnedCount, tabs.TabCount - 1);
        if (source == target) return;
        tabs.TabPages.Remove(page);
        tabs.TabPages.Insert(target, page);
        tabs.SelectedTab = page;
        PersistBrowserTabOrder(tabs);
    }

    private void PersistBrowserTabOrder(TabControl tabs)
    {
        var workspace = WorkspaceFor(tabs);
        if (workspace is null) return;
        var visible = tabs.TabPages.Cast<TabPage>().Select(page => page.Tag as BrowserTab).Where(tab => tab is not null).Select(tab => tab!).ToList();
        var hidden = workspace.Tabs.Where(tab => !visible.Any(item => item.Id == tab.Id)).ToList();
        workspace.Tabs = visible.Concat(hidden).ToList();
        ConfigStore.Save(_config);
        _status.Text = L.T("บันทึกลำดับ Tab แล้ว", "Tab order saved");
    }

    private void DrawWorkspaceTab(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _workspaceTabs.TabCount) return;
        var page = _workspaceTabs.TabPages[e.Index];
        var color = page.Tag is BrowserWorkspace workspace ? ParseColor(workspace.ColorHex, ThemeManager.ControlBack) : ThemeManager.ControlBack;
        if (e.Index == _workspaceTabs.SelectedIndex) color = ControlPaint.Light(color, 0.18f);
        using var brush = new SolidBrush(color);
        e.Graphics.FillRectangle(brush, e.Bounds);
        var foreground = color.GetBrightness() < 0.5f ? Color.White : Color.Black;
        TextRenderer.DrawText(e.Graphics, page.Text, Font, e.Bounds, foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void WorkspaceTabsMouseDown(object? sender, MouseEventArgs e)
    {
        _workspaceDragStart = -1;
        if (e.Button != MouseButtons.Left) return;
        for (var i = 0; i < _workspaceTabs.TabCount; i++)
            if (_workspaceTabs.GetTabRect(i).Contains(e.Location)) { _workspaceDragStart = i; break; }
    }

    private void WorkspaceTabsMouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _workspaceDragStart < 0 || _workspaceDragStart >= _workspaceTabs.TabCount) return;
        var page = _workspaceTabs.TabPages[_workspaceDragStart];
        _workspaceDragStart = -1;
        _workspaceTabs.DoDragDrop(page, DragDropEffects.Move);
    }

    private static void WorkspaceTabsDragOver(object? sender, DragEventArgs e) =>
        e.Effect = e.Data?.GetDataPresent(typeof(TabPage)) == true ? DragDropEffects.Move : DragDropEffects.None;

    private void WorkspaceTabsDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(typeof(TabPage)) is not TabPage page || !_workspaceTabs.TabPages.Contains(page)) return;
        var point = _workspaceTabs.PointToClient(new Point(e.X, e.Y));
        var target = _workspaceTabs.TabCount - 1;
        for (var i = 0; i < _workspaceTabs.TabCount; i++)
            if (_workspaceTabs.GetTabRect(i).Contains(point)) { target = i; break; }
        var source = _workspaceTabs.TabPages.IndexOf(page);
        if (source == target) return;
        _workspaceTabs.TabPages.Remove(page);
        _workspaceTabs.TabPages.Insert(target, page);
        _workspaceTabs.SelectedTab = page;
        _config.Workspaces = _workspaceTabs.TabPages.Cast<TabPage>().Select(item => (BrowserWorkspace)item.Tag!).ToList();
        ConfigStore.Save(_config);
        _status.Text = L.T("บันทึกลำดับ Instance แล้ว", "Instance order saved");
    }

    private void BrowserTabsMouseUp(object? sender, MouseEventArgs e)
    {
        if (sender is not TabControl tabs || e.Button != MouseButtons.Right) return;
        for (var i = 0; i < tabs.TabCount; i++)
            if (tabs.GetTabRect(i).Contains(e.Location))
            {
                tabs.SelectedIndex = i;
                tabs.TabPages[i].ContextMenuStrip?.Show(tabs, e.Location);
                return;
            }
    }

    private static Rectangle CloseButtonBounds(Rectangle tabBounds) =>
        new(tabBounds.Right - CloseButtonWidth, tabBounds.Top + 3, CloseButtonWidth - 3, tabBounds.Height - 5);

    private Control BuildFindBar(WebView2 browser)
    {
        var panel = new FlowLayoutPanel
        {
            Name = "FindBar",
            Dock = DockStyle.Top,
            Height = 38,
            Visible = false,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(6, 4, 6, 2),
            BackColor = ThemeManager.ControlBack
        };
        var search = new TextBox { Width = 300 };
        var result = new Label { Text = "", AutoSize = false, Width = 90, Height = 28, TextAlign = ContentAlignment.MiddleLeft };
        var previous = new Button { Text = "↑", Width = 36, Height = 28 };
        var next = new Button { Text = "↓", Width = 36, Height = 28 };
        var close = new Button { Text = "×", Width = 36, Height = 28 };
        async Task FindAsync(bool backwards)
        {
            if (string.IsNullOrEmpty(search.Text) || browser.CoreWebView2 is null) return;
            var query = JsonSerializer.Serialize(search.Text);
            var found = await browser.CoreWebView2.ExecuteScriptAsync($"window.find({query}, false, {backwards.ToString().ToLowerInvariant()}, true, false, true, false)");
            result.Text = string.Equals(found, "true", StringComparison.OrdinalIgnoreCase) ? "พบข้อความ" : "ไม่พบ";
        }
        search.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { await FindAsync(e.Shift); e.SuppressKeyPress = true; }
            if (e.KeyCode == Keys.Escape) { panel.Visible = false; browser.Focus(); e.SuppressKeyPress = true; }
        };
        previous.Click += async (_, _) => await FindAsync(true);
        next.Click += async (_, _) => await FindAsync(false);
        close.Click += (_, _) => { panel.Visible = false; browser.Focus(); };
        panel.Controls.AddRange([search, previous, next, result, close]);
        panel.Tag = search;
        return panel;
    }

    private void ShowFindBar()
    {
        var page = CurrentInnerTabs()?.SelectedTab;
        var panel = page?.Controls.Find("FindBar", false).FirstOrDefault();
        if (panel is null) return;
        panel.Visible = true;
        panel.BringToFront();
        if (panel.Tag is TextBox search) { search.Focus(); search.SelectAll(); }
    }

    private void AddCurrentFavorite()
    {
        var browser = CurrentWebView();
        var workspace = CurrentWorkspace();
        var url = browser?.Source?.ToString();
        if (browser?.CoreWebView2 is null || workspace is null || string.IsNullOrWhiteSpace(url)) return;
        if (_config.Favorites.Any(item => item.WorkspaceId == workspace.Id && string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase)))
        {
            _status.Text = "หน้านี้อยู่ใน Favorites แล้ว";
            return;
        }
        _config.Favorites.Add(new FavoriteItem
        {
            WorkspaceId = workspace.Id,
            Title = string.IsNullOrWhiteSpace(browser.CoreWebView2.DocumentTitle) ? url : browser.CoreWebView2.DocumentTitle,
            Url = url
        });
        ConfigStore.Save(_config);
        _status.Text = "เพิ่มใน Favorites แล้ว";
    }

    private void AddHistory(BrowserWorkspace workspace, WebView2 browser)
    {
        var url = browser.Source?.ToString();
        if (string.IsNullOrWhiteSpace(url) || url.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) return;
        var last = _config.History.LastOrDefault();
        if (last is not null && last.WorkspaceId == workspace.Id &&
            string.Equals(last.Url, url, StringComparison.OrdinalIgnoreCase) &&
            DateTime.Now - last.VisitedAt < TimeSpan.FromSeconds(10)) return;
        _config.History.Add(new HistoryItem
        {
            WorkspaceId = workspace.Id,
            Title = string.IsNullOrWhiteSpace(browser.CoreWebView2?.DocumentTitle) ? url : browser.CoreWebView2.DocumentTitle,
            Url = url,
            VisitedAt = DateTime.Now
        });
        var maximum = Math.Max(100, _config.Settings.MaxHistoryItems);
        if (_config.History.Count > maximum)
            _config.History.RemoveRange(0, _config.History.Count - maximum);
    }

    private void TrackDownload(CoreWebView2DownloadStartingEventArgs e)
    {
        var operation = e.DownloadOperation;
        if (!_config.Settings.AskWhereToSaveDownloads && !string.IsNullOrWhiteSpace(_config.Settings.DownloadFolder))
        {
            Directory.CreateDirectory(_config.Settings.DownloadFolder);
            e.ResultFilePath = Path.Combine(_config.Settings.DownloadFolder, Path.GetFileName(e.ResultFilePath));
        }
        var record = new DownloadRecord
        {
            FileName = Path.GetFileName(e.ResultFilePath),
            Path = e.ResultFilePath,
            SourceUrl = operation.Uri,
            Status = "กำลังดาวน์โหลด",
            Pause = operation.Pause,
            Resume = operation.Resume,
            Cancel = operation.Cancel
        };
        _downloads.Insert(0, record);
        void Update()
        {
            if (IsDisposed) return;
            BeginInvoke(() =>
            {
                record.Path = e.ResultFilePath;
                record.FileName = Path.GetFileName(record.Path);
                var totalBytes = operation.TotalBytesToReceive;
                record.Progress = totalBytes is > 0
                    ? operation.BytesReceived * 100d / totalBytes.Value : 0;
                record.Status = operation.State switch
                {
                    CoreWebView2DownloadState.Completed => "เสร็จแล้ว",
                    CoreWebView2DownloadState.Interrupted => "ขัดข้อง",
                    _ => "กำลังดาวน์โหลด"
                };
                _status.Text = record.Status == "เสร็จแล้ว" ? $"ดาวน์โหลด {record.FileName} เสร็จแล้ว" : $"ดาวน์โหลด {record.FileName} {record.Progress:0}%";
            });
        }
        operation.BytesReceivedChanged += (_, _) => Update();
        operation.StateChanged += (_, _) => Update();
        _status.Text = $"เริ่มดาวน์โหลด {record.FileName}";
    }

    private void ShowBrowserMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Favorites", null, (_, _) => ShowBrowserTools(0));
        menu.Items.Add("History", null, (_, _) => ShowBrowserTools(1));
        menu.Items.Add("Downloads", null, (_, _) => ShowBrowserTools(2));
        menu.Items.Add("Settings", null, (_, _) => ShowBrowserTools(3));
        var backup = new ToolStripMenuItem(L.T("สำรองและย้ายข้อมูล", "Backup and transfer"));
        backup.DropDownItems.Add(L.T("Export ทุก Instance", "Export all instances"), null,
            (_, _) => ExportMetadataBackup(BackupExportScope.All));
        backup.DropDownItems.Add(L.T("Export Instance ปัจจุบัน", "Export current instance"), null,
            (_, _) => ExportMetadataBackup(BackupExportScope.CurrentWorkspace));
        backup.DropDownItems.Add(L.T("Export Tab ปัจจุบัน", "Export current tab"), null,
            (_, _) => ExportMetadataBackup(BackupExportScope.CurrentTab));
        backup.DropDownItems.Add(new ToolStripSeparator());
        backup.DropDownItems.Add(L.T("Import จากไฟล์ Backup", "Import from backup"), null,
            (_, _) => ImportMetadataBackup());
        menu.Items.Add(backup);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L.T("ค้นหาในหน้า   Ctrl+F", "Find in page   Ctrl+F"), null, (_, _) => ShowFindBar());
        menu.Items.Add("Zoom In   Ctrl++", null, (_, _) => ChangeZoom(0.1));
        menu.Items.Add("Zoom Out   Ctrl+-", null, (_, _) => ChangeZoom(-0.1));
        menu.Items.Add("Reset Zoom   Ctrl+0", null, (_, _) => ResetZoom());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L.T("พิมพ์ / บันทึก PDF   Ctrl+P", "Print / Save PDF   Ctrl+P"), null, (_, _) => PrintCurrentPage());
        menu.Items.Add(L.T("เต็มหน้าจอ   F11", "Full Screen   F11"), null, (_, _) => ToggleFullScreen());
        menu.Items.Add(new ToolStripSeparator());
        var awake = new ToolStripMenuItem(L.T("Keep Awake / ป้องกัน Sleep", "Keep Awake"));
        awake.DropDownItems.Add(BuildAwakeMenu(L.T("ป้องกัน Sleep", "Prevent sleep"), false));
        awake.DropDownItems.Add(BuildAwakeMenu(L.T("ป้องกัน Sleep และเปิดจอไว้", "Prevent sleep and keep display on"), true));
        awake.DropDownItems.Add(new ToolStripSeparator());
        awake.DropDownItems.Add(L.T("ปิด Keep Awake", "Turn off Keep Awake"), null, (_, _) => DisableKeepAwake());
        menu.Items.Add(awake);
        ThemeManager.Apply(menu);
        menu.Show(Cursor.Position);
    }

    private void ExportMetadataBackup(BackupExportScope scope)
    {
        SaveSession();
        var currentWorkspace = CurrentWorkspace();
        var currentTab = CurrentTab();
        List<BrowserWorkspace> workspaces;
        List<FavoriteItem> favorites;
        switch (scope)
        {
            case BackupExportScope.CurrentWorkspace when currentWorkspace is not null:
                workspaces = [currentWorkspace];
                favorites = _config.Favorites.Where(item => item.WorkspaceId == currentWorkspace.Id).ToList();
                break;
            case BackupExportScope.CurrentTab when currentWorkspace is not null && currentTab is not null:
                workspaces =
                [
                    new BrowserWorkspace
                    {
                        Id = currentWorkspace.Id,
                        Name = currentWorkspace.Name,
                        ProfileFolder = currentWorkspace.ProfileFolder,
                        ColorHex = currentWorkspace.ColorHex,
                        Tabs = [CloneTab(currentTab)]
                    }
                ];
                favorites = _config.Favorites.Where(item =>
                    item.WorkspaceId == currentWorkspace.Id &&
                    string.Equals(item.Url.TrimEnd('/'), (currentTab.CurrentUrl ?? currentTab.Url).TrimEnd('/'),
                        StringComparison.OrdinalIgnoreCase)).ToList();
                break;
            case BackupExportScope.All:
                workspaces = _config.Workspaces.ToList();
                favorites = _config.Favorites.ToList();
                break;
            default:
                MessageBox.Show(L.T("ไม่มี Instance หรือ Tab ที่เลือก", "No instance or tab is selected."),
                    L.T("Export Backup", "Export backup"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = L.T("บันทึกไฟล์ Backup", "Save backup file"),
            Filter = "Edge Workspace Manager Backup (*.ewmbackup)|*.ewmbackup",
            DefaultExt = "ewmbackup",
            AddExtension = true,
            FileName = $"EdgeWorkspaceManager-{DateTime.Now:yyyyMMdd-HHmm}.ewmbackup"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            MetadataBackupService.Export(dialog.FileName, workspaces, favorites);
            _status.Text = L.T("Export Backup สำเร็จ", "Backup exported successfully");
            MessageBox.Show(L.T(
                    $"Export สำเร็จ\r\n\r\nInstance: {workspaces.Count}\r\nTab: {workspaces.Sum(item => item.Tabs.Count)}\r\nFavorites: {favorites.Count}\r\n\r\nไฟล์นี้ไม่รวม Cookie, Login, Password หรือ WebView2 Profile",
                    $"Export completed.\r\n\r\nInstances: {workspaces.Count}\r\nTabs: {workspaces.Sum(item => item.Tabs.Count)}\r\nFavorites: {favorites.Count}\r\n\r\nThis file does not include cookies, logins, passwords, or WebView2 profiles."),
                L.T("Export Backup", "Export backup"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(L.T($"Export ไม่สำเร็จ\r\n\r\n{ex.Message}", $"Export failed.\r\n\r\n{ex.Message}"),
                L.T("Export Backup", "Export backup"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportMetadataBackup()
    {
        using var dialog = new OpenFileDialog
        {
            Title = L.T("เลือกไฟล์ Backup", "Select a backup file"),
            Filter = "Edge Workspace Manager Backup (*.ewmbackup)|*.ewmbackup",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var imported = MetadataBackupService.Import(dialog.FileName);
            var answer = MessageBox.Show(L.T(
                    $"นำเข้าข้อมูลจาก Edge Workspace Manager {imported.Manifest.AppVersion}\r\n\r\nInstance: {imported.Manifest.InstanceCount}\r\nTab: {imported.Manifest.TabCount}\r\nFavorites: {imported.Manifest.FavoriteCount}\r\n\r\nข้อมูลจะถูกสร้างเป็น Instance ใหม่ และไม่เขียนทับข้อมูลเดิม ต้องการดำเนินการต่อหรือไม่?",
                    $"Import data from Edge Workspace Manager {imported.Manifest.AppVersion}\r\n\r\nInstances: {imported.Manifest.InstanceCount}\r\nTabs: {imported.Manifest.TabCount}\r\nFavorites: {imported.Manifest.FavoriteCount}\r\n\r\nThe data will be created as new instances and will not replace existing data. Continue?"),
                L.T("Import Backup", "Import backup"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;

            SaveSession();
            foreach (var workspace in imported.Workspaces)
            {
                workspace.Name = UniqueImportedWorkspaceName(workspace.Name);
                workspace.ProfileFolder = UniqueImportedProfileFolder(workspace.ProfileFolder);
                _config.Workspaces.Add(workspace);
                _config.OpenTabIdsByWorkspace[workspace.Id] = workspace.Tabs
                    .Where(tab => tab.IsOpen).Select(tab => tab.Id).ToList();
            }
            _config.Favorites.AddRange(imported.Favorites);
            SaveAndReload();
            _status.Text = L.T("Import Backup สำเร็จ", "Backup imported successfully");
            MessageBox.Show(L.T(
                    "Import สำเร็จ ข้อมูลถูกเพิ่มเป็น Instance ใหม่\r\n\r\nกรุณา Login เว็บไซต์อีกครั้ง เนื่องจาก Metadata Backup ไม่รวม Cookie หรือ Login",
                    "Import completed. The data was added as new instances.\r\n\r\nPlease sign in to websites again because metadata backups do not contain cookies or login data."),
                L.T("Import Backup", "Import backup"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(L.T($"Import ไม่สำเร็จ\r\n\r\n{ex.Message}", $"Import failed.\r\n\r\n{ex.Message}"),
                L.T("Import Backup", "Import backup"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string UniqueImportedWorkspaceName(string source)
    {
        var baseName = string.IsNullOrWhiteSpace(source) ? L.T("Instance ที่นำเข้า", "Imported instance") : source.Trim();
        var candidate = baseName;
        for (var suffix = 2; _config.Workspaces.Any(item => string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)); suffix++)
            candidate = $"{baseName} ({suffix})";
        return candidate;
    }

    private string UniqueImportedProfileFolder(string source)
    {
        var baseName = Sanitize(string.IsNullOrWhiteSpace(source) ? "Imported" : source) + "-Imported";
        var candidate = baseName;
        for (var suffix = 2; _config.Workspaces.Any(item => string.Equals(item.ProfileFolder, candidate, StringComparison.OrdinalIgnoreCase)); suffix++)
            candidate = $"{baseName}-{suffix}";
        return candidate;
    }

    private enum BackupExportScope { All, CurrentWorkspace, CurrentTab }

    private ToolStripMenuItem BuildAwakeMenu(string title, bool keepDisplayOn)
    {
        var menu = new ToolStripMenuItem(title);
        menu.DropDownItems.Add(L.T("30 นาที", "30 minutes"), null, (_, _) => EnableKeepAwake(keepDisplayOn, TimeSpan.FromMinutes(30)));
        menu.DropDownItems.Add(L.T("1 ชั่วโมง", "1 hour"), null, (_, _) => EnableKeepAwake(keepDisplayOn, TimeSpan.FromHours(1)));
        menu.DropDownItems.Add(L.T("2 ชั่วโมง", "2 hours"), null, (_, _) => EnableKeepAwake(keepDisplayOn, TimeSpan.FromHours(2)));
        menu.DropDownItems.Add(L.T("จนกว่าจะปิดเอง", "Until turned off"), null, (_, _) => EnableKeepAwake(keepDisplayOn, null));
        return menu;
    }

    private void EnableKeepAwake(bool keepDisplayOn, TimeSpan? duration)
    {
        try
        {
            _powerAwake.Enable(keepDisplayOn, duration);
            UpdateAwakeStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Keep Awake", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void DisableKeepAwake()
    {
        if (_powerAwake.Disable()) _status.Text = L.T("ปิด Keep Awake แล้ว", "Keep Awake turned off");
    }

    private void CheckAwakeTimeout()
    {
        if (!_powerAwake.IsActive) return;
        if (_powerAwake.EndsAt.HasValue && DateTime.Now >= _powerAwake.EndsAt.Value) { DisableKeepAwake(); return; }
        UpdateAwakeStatus();
    }

    private void UpdateAwakeStatus()
    {
        if (!_powerAwake.IsActive) return;
        var mode = _powerAwake.KeepDisplayOn ? L.T("เปิดจอไว้", "display on") : L.T("ป้องกัน Sleep", "prevent sleep");
        var remaining = _powerAwake.EndsAt.HasValue
            ? L.T($" เหลือ {Math.Max(0, (_powerAwake.EndsAt.Value - DateTime.Now).TotalMinutes):0} นาที", $" {_powerAwake.EndsAt.Value - DateTime.Now:h\\:mm} remaining")
            : L.T(" จนกว่าจะปิดเอง", " until turned off");
        _status.Text = $"Keep Awake: {mode}{remaining}";
    }

    private void ShowBrowserTools(int selectedTab)
    {
        using var tools = new BrowserToolsForm(_config, _downloads,
            url => { _ = AddNewTabAsync(url); },
            () => ConfigStore.Save(_config), selectedTab);
        tools.ShowDialog(this);
        ThemeManager.Configure(_config.Settings.Theme);
        ApplyTheme();
        foreach (var inner in _workspaceTabs.TabPages.Cast<TabPage>().SelectMany(page => page.Controls.OfType<TabControl>()))
            inner.Invalidate();
    }

    private void ChangeZoom(double delta)
    {
        var browser = CurrentWebView();
        if (browser is null) return;
        browser.ZoomFactor = Math.Clamp(browser.ZoomFactor + delta, 0.25, 5.0);
        _status.Text = $"Zoom {browser.ZoomFactor * 100:0}%";
    }

    private void ResetZoom()
    {
        if (CurrentWebView() is not { } browser) return;
        browser.ZoomFactor = 1.0;
        _status.Text = "Zoom 100%";
    }

    private void PrintCurrentPage() => CurrentWebView()?.CoreWebView2?.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);

    private void ToggleFullScreen()
    {
        if (FormBorderStyle == FormBorderStyle.None)
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Maximized;
        }
        else
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
        }
    }

    private void CheckSystemTheme()
    {
        if (ThemeManager.RefreshSystemTheme()) ApplyTheme();
    }

    private void ApplyTheme()
    {
        ThemeManager.Apply(this);
        _addressSuggestions.BackColor = ThemeManager.InputBack;
        _addressSuggestions.ForeColor = ThemeManager.Foreground;
        foreach (var outer in _workspaceTabs.TabPages.Cast<TabPage>())
            if (outer.Controls.OfType<TabControl>().FirstOrDefault() is { } inner)
            {
                inner.Invalidate();
                foreach (var browser in inner.TabPages.Cast<TabPage>().SelectMany(page => page.Controls.OfType<WebView2>()))
                {
                    browser.DefaultBackgroundColor = ThemeManager.WindowBack;
                    if (browser.CoreWebView2 is not null)
                        browser.CoreWebView2.Profile.PreferredColorScheme = ThemeManager.IsDark
                            ? CoreWebView2PreferredColorScheme.Dark
                            : CoreWebView2PreferredColorScheme.Light;
                }
            }
        _workspaceTabs.Invalidate();
        Invalidate(true);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.L)) { _address.Focus(); _address.SelectAll(); return true; }
        if (keyData == (Keys.Control | Keys.T)) { _ = AddNewTabAsync(); return true; }
        if (keyData == (Keys.Control | Keys.W)) { CloseCurrentTab(); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.T)) { _ = RestoreClosedTabAsync(); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.D)) { _ = DuplicateCurrentTabAsync(); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.P)) { ToggleCurrentPinnedTab(); return true; }
        if (keyData == (Keys.Control | Keys.R) || keyData == Keys.F5) { CurrentWebView()?.Reload(); return true; }
        if (keyData == (Keys.Alt | Keys.Left)) { GoBack(); return true; }
        if (keyData == (Keys.Alt | Keys.Right)) { GoForward(); return true; }
        if (keyData == (Keys.Control | Keys.Tab)) { SelectRelativeTab(1); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.Tab)) { SelectRelativeTab(-1); return true; }
        if (keyData == (Keys.Control | Keys.F)) { ShowFindBar(); return true; }
        if (keyData == (Keys.Control | Keys.Oemplus) || keyData == (Keys.Control | Keys.Add)) { ChangeZoom(0.1); return true; }
        if (keyData == (Keys.Control | Keys.OemMinus) || keyData == (Keys.Control | Keys.Subtract)) { ChangeZoom(-0.1); return true; }
        if (keyData == (Keys.Control | Keys.D0) || keyData == (Keys.Control | Keys.NumPad0)) { ResetZoom(); return true; }
        if (keyData == (Keys.Control | Keys.P)) { PrintCurrentPage(); return true; }
        if (keyData == Keys.F11) { ToggleFullScreen(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void CloseCurrentTab()
    {
        var inner = CurrentInnerTabs();
        if (inner?.SelectedTab is { } page) CloseTab(inner, page);
    }

    private void SelectRelativeTab(int offset)
    {
        var inner = CurrentInnerTabs();
        if (inner is null || inner.TabCount == 0) return;
        inner.SelectedIndex = (inner.SelectedIndex + offset + inner.TabCount) % inner.TabCount;
    }

    private void GoBack() { var browser = CurrentWebView(); if (browser?.CanGoBack == true) browser.GoBack(); }
    private void GoForward() { var browser = CurrentWebView(); if (browser?.CanGoForward == true) browser.GoForward(); }
    private TabControl? CurrentInnerTabs() => _workspaceTabs.SelectedTab?.Controls.OfType<TabControl>().FirstOrDefault();
    private WebView2? CurrentWebView() => CurrentInnerTabs()?.SelectedTab?.Controls.OfType<WebView2>().FirstOrDefault();
    private BrowserWorkspace? CurrentWorkspace() => _workspaceTabs.SelectedTab?.Tag as BrowserWorkspace;
    private BrowserTab? CurrentTab() => CurrentInnerTabs()?.SelectedTab?.Tag as BrowserTab;

    private BrowserWorkspace? WorkspaceFor(TabControl inner) => _workspaceTabs.TabPages.Cast<TabPage>()
        .FirstOrDefault(p => p.Controls.Contains(inner))?.Tag as BrowserWorkspace;

    private void SelectWorkspace(BrowserWorkspace workspace)
    {
        var page = _workspaceTabs.TabPages.Cast<TabPage>().FirstOrDefault(p => ReferenceEquals(p.Tag, workspace));
        if (page is not null) _workspaceTabs.SelectedTab = page;
    }

    private void SyncAddress()
    {
        var browser = CurrentWebView();
        if (_addressUserEditing)
        {
            UpdatePinButtonState();
            return;
        }
        _syncingAddress = true;
        _address.Text = browser?.Source?.ToString() ?? CurrentTab()?.CurrentUrl ?? CurrentTab()?.Url ?? "";
        _syncingAddress = false;
        HideAddressSuggestions();
        UpdatePinButtonState();
    }

    private void WebViewSourceChanged(WebView2 browser, BrowserTab tab)
    {
        var url = browser.Source?.ToString();
        if (string.IsNullOrWhiteSpace(url)) return;

        tab.CurrentUrl = url;
        SyncAddressFrom(browser);
        SaveSession();
    }

    private void SyncAddressFrom(WebView2 browser)
    {
        if (!ReferenceEquals(browser, CurrentWebView()) || _addressUserEditing) return;
        SyncAddress();
    }

    private void UpdatePinButtonState()
    {
        if (_pinTabButton is null) return;
        var tab = CurrentTab();
        _pinTabButton.Enabled = tab is not null;
        var pinned = tab?.IsPinned == true;
        _pinTabButton.Text = pinned ? "📍" : "📌";
        _pinTabButton.AccessibleName = pinned
            ? L.T("เลิกปักหมุด Tab ปัจจุบัน", "Unpin current tab")
            : L.T("ปักหมุด Tab ปัจจุบัน", "Pin current tab");
        _pinTabToolTip.SetToolTip(_pinTabButton, pinned
            ? L.T("เลิกปักหมุด Tab ปัจจุบัน (Ctrl+Shift+P)", "Unpin current tab (Ctrl+Shift+P)")
            : L.T("ปักหมุด Tab ปัจจุบัน (Ctrl+Shift+P)", "Pin current tab (Ctrl+Shift+P)"));
    }

    private void ScheduleAddressSuggestions()
    {
        if (_syncingAddress || !_address.Focused || !_config.Settings.AddressSuggestionsEnabled) return;
        _suggestionTimer.Stop();
        _suggestionTimer.Start();
    }

    private void UpdateAddressSuggestions()
    {
        var query = _address.Text.Trim();
        var workspace = CurrentWorkspace();
        if (query.Length == 0 || workspace is null || !_address.Focused) { HideAddressSuggestions(); return; }
        var allInstances = _config.Settings.SearchSuggestionsAcrossInstances;
        var candidates = new Dictionary<string, AddressSuggestion>(StringComparer.OrdinalIgnoreCase);

        void Add(string title, string url, int baseScore, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            var titleMatch = title.Contains(query, StringComparison.OrdinalIgnoreCase);
            var urlMatch = url.Contains(query, StringComparison.OrdinalIgnoreCase);
            if (!titleMatch && !urlMatch) return;
            var score = baseScore + count * 3;
            if (url.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 120;
            if (title.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 100;
            if (candidates.TryGetValue(url, out var existing))
            {
                existing.Score += score;
                if (existing.Title == existing.Url && !string.IsNullOrWhiteSpace(title)) existing.Title = title;
            }
            else candidates[url] = new AddressSuggestion(title, url, score);
        }

        foreach (var favorite in _config.Favorites.Where(item => allInstances || item.WorkspaceId == workspace.Id))
            Add(favorite.Title, favorite.Url, 90);

        foreach (var group in _config.History.Where(item => allInstances || item.WorkspaceId == workspace.Id)
                     .GroupBy(item => item.Url, StringComparer.OrdinalIgnoreCase))
        {
            var latest = group.OrderByDescending(item => item.VisitedAt).First();
            var recency = latest.VisitedAt > DateTime.Now.AddDays(-1) ? 35 : latest.VisitedAt > DateTime.Now.AddDays(-7) ? 20 : 0;
            Add(latest.Title, latest.Url, 40 + recency, group.Count());
        }

        var tabPages = allInstances
            ? _workspaceTabs.TabPages.Cast<TabPage>().SelectMany(page => page.Controls.OfType<TabControl>().SelectMany(tabs => tabs.TabPages.Cast<TabPage>()))
            : CurrentInnerTabs()?.TabPages.Cast<TabPage>() ?? Enumerable.Empty<TabPage>();
        foreach (var page in tabPages)
            if (page.Controls.OfType<WebView2>().FirstOrDefault() is { } browser && browser.Source is not null)
                Add(page.Text, browser.Source.ToString(), 140);

        var results = candidates.Values.OrderByDescending(item => item.Score).ThenBy(item => item.Title).Take(8).ToList();
        if (results.Count == 0) { HideAddressSuggestions(); return; }
        _addressSuggestions.BeginUpdate();
        _addressSuggestions.Items.Clear();
        foreach (var result in results) _addressSuggestions.Items.Add(result);
        _addressSuggestions.SelectedIndex = -1;
        _addressSuggestions.EndUpdate();
        var location = PointToClient(_address.PointToScreen(new Point(0, _address.Height)));
        _addressSuggestions.Location = location;
        _addressSuggestions.Width = Math.Max(_address.Width, 520);
        _addressSuggestions.Height = Math.Min(190, results.Count * _addressSuggestions.ItemHeight + 4);
        _addressSuggestions.Visible = true;
        _addressSuggestions.BringToFront();
    }

    private void AddressKeyDown(object? sender, KeyEventArgs e)
    {
        if (_addressSuggestions.Visible && e.KeyCode is Keys.Down or Keys.Up)
        {
            var delta = e.KeyCode == Keys.Down ? 1 : -1;
            var next = _addressSuggestions.SelectedIndex + delta;
            if (next < 0) next = _addressSuggestions.Items.Count - 1;
            if (next >= _addressSuggestions.Items.Count) next = 0;
            _addressSuggestions.SelectedIndex = next;
            e.SuppressKeyPress = true;
            return;
        }
        if (e.KeyCode == Keys.Escape && _addressSuggestions.Visible)
        {
            HideAddressSuggestions();
            e.SuppressKeyPress = true;
            return;
        }
        if (e.KeyCode != Keys.Enter) return;
        if (_addressSuggestions.Visible && _addressSuggestions.SelectedItem is AddressSuggestion) OpenSelectedAddressSuggestion();
        else NavigateAddress();
        e.SuppressKeyPress = true;
    }

    private void OpenSelectedAddressSuggestion()
    {
        if (_addressSuggestions.SelectedItem is not AddressSuggestion suggestion) return;
        _syncingAddress = true;
        _address.Text = suggestion.Url;
        _syncingAddress = false;
        HideAddressSuggestions();
        NavigateAddress();
    }

    private void HideAddressSuggestions()
    {
        _suggestionTimer.Stop();
        _addressSuggestions.Visible = false;
    }

    private sealed class AddressSuggestion(string title, string url, int score)
    {
        public string Title { get; set; } = string.IsNullOrWhiteSpace(title) ? url : title;
        public string Url { get; } = url;
        public int Score { get; set; } = score;
        public override string ToString() => $"{Title}    —    {Url}";
    }

    private void NavigateAddress()
    {
        HideAddressSuggestions();
        var browser = CurrentWebView();
        if (browser is null || string.IsNullOrWhiteSpace(_address.Text)) return;
        _addressUserEditing = false;
        try { browser.Source = new Uri(ToNavigationUrl(_address.Text)); }
        catch { MessageBox.Show("URL หรือคำค้นหาไม่ถูกต้อง", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void NavigateHome()
    {
        var browser = CurrentWebView();
        var tab = CurrentTab();
        if (browser is not null && tab is not null) browser.Source = new Uri(ToNavigationUrl(tab.Url));
    }

    private void AddWorkspace()
    {
        var workspace = new BrowserWorkspace { Name = $"Workspace {_config.Workspaces.Count + 1}", ProfileFolder = $"Profile{_config.Workspaces.Count + 1:00}", Tabs = [new BrowserTab()] };
        using var editor = new WorkspaceEditorForm(workspace, true);
        if (editor.ShowDialog(this) != DialogResult.OK) return;
        _config.Workspaces.Add(workspace);
        SaveAndReload();
    }

    private async Task EditCurrentWorkspaceAsync()
    {
        var workspace = CurrentWorkspace();
        var inner = CurrentInnerTabs();
        if (workspace is null || inner is null) return;
        SaveSession();
        var oldProfileFolder = workspace.ProfileFolder;
        var activeBrowserTabs = inner.TabPages.Cast<TabPage>()
            .Where(page => page.Tag is BrowserTab)
            .Select(page =>
            {
                var sourceTab = (BrowserTab)page.Tag!;
                var currentTab = CloneTab(sourceTab);
                var browser = page.Controls.OfType<WebView2>().FirstOrDefault();
                var documentTitle = browser?.CoreWebView2?.DocumentTitle;
                currentTab.Name = !string.IsNullOrWhiteSpace(documentTitle) ? documentTitle : page.Text;
                currentTab.CurrentUrl = browser?.Source?.ToString() ?? sourceTab.CurrentUrl ?? sourceTab.Url;
                return currentTab;
            })
            .ToList();
        var externalTabNames = inner.TabPages.Cast<TabPage>()
            .Where(page => page.Controls.OfType<ExternalWindowHost>().Any())
            .Select(page => page.Text)
            .ToList();
        using var editor = new WorkspaceEditorForm(workspace, false, externalTabNames, activeBrowserTabs);
        if (editor.ShowDialog(this) != DialogResult.OK) return;
        if (editor.DeleteRequested)
        {
            _config.Workspaces.Remove(workspace);
            SaveAndReload();
            return;
        }
        if (!string.Equals(oldProfileFolder, workspace.ProfileFolder, StringComparison.OrdinalIgnoreCase))
        {
            SaveAndReload();
            return;
        }
        await ApplyWorkspaceEditsAsync(workspace, inner);
    }

    private async Task ApplyWorkspaceEditsAsync(BrowserWorkspace workspace, TabControl inner)
    {
        var configuredIds = workspace.Tabs.Select(tab => tab.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var page in inner.TabPages.Cast<TabPage>()
                     .Where(page => page.Tag is BrowserTab tab && !configuredIds.Contains(tab.Id)).ToList())
        {
            inner.TabPages.Remove(page);
            page.Dispose();
        }

        var openIds = inner.TabPages.Cast<TabPage>()
            .Select(page => page.Tag as BrowserTab)
            .Where(tab => tab is not null)
            .Select(tab => tab!.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var page in inner.TabPages.Cast<TabPage>())
            if (page.Tag is BrowserTab tab)
            {
                page.Text = tab.Name;
                var browser = page.Controls.OfType<WebView2>().FirstOrDefault();
                if (browser is not null && !string.IsNullOrWhiteSpace(tab.CurrentUrl) &&
                    !string.Equals(browser.Source?.ToString().TrimEnd('/'), tab.CurrentUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                    browser.Source = new Uri(ToNavigationUrl(tab.CurrentUrl));
            }

        foreach (var tab in workspace.Tabs.Where(tab => tab.IsOpen && !openIds.Contains(tab.Id)))
            await CreateBrowserTabPageAsync(inner, workspace, tab, false);

        if (_workspaceTabs.SelectedTab is { } outer) outer.Text = workspace.Name;
        _workspaceTabs.Invalidate();
        ConfigStore.Save(_config);
        SyncAddress();
        _status.Text = "บันทึก Tab ที่เปิดอยู่แล้ว";
    }

    private void SaveAndReload()
    {
        ConfigStore.Save(_config);
        _environments.Clear();
        _ = ReloadWorkspaceAsync();
    }

    private void SaveSession()
    {
        if (_isReloading) return;
        var openTabIds = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var workspace in _config.Workspaces)
        {
            foreach (var tab in workspace.Tabs)
                tab.IsOpen = false;
            openTabIds[workspace.Id] = new List<string>();
        }
        foreach (var outer in _workspaceTabs.TabPages.Cast<TabPage>())
            if (outer.Controls.OfType<TabControl>().FirstOrDefault() is { } inner)
                foreach (var page in inner.TabPages.Cast<TabPage>())
                    if (page.Tag is BrowserTab tab)
                    {
                        tab.IsOpen = true;
                        tab.CurrentUrl = page.Controls.OfType<WebView2>().FirstOrDefault()?.Source?.ToString() ?? tab.CurrentUrl;
                        if (outer.Tag is BrowserWorkspace workspace)
                            openTabIds[workspace.Id].Add(tab.Id);
                    }
        _config.OpenTabIdsByWorkspace = openTabIds;
        try { ConfigStore.Save(_config); } catch { }
    }

    private void WindowPickerMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _windowPickerButton is null) return;
        _windowPickerButton.Capture = true;
        Cursor = Cursors.Cross;
        _status.Text = "ลากเป้าไปปล่อยบนหน้าต่างโปรแกรมที่ต้องการ...";
    }

    private void WindowPickerMouseUp(object? sender, MouseEventArgs e)
    {
        if (_windowPickerButton is null) return;
        _windowPickerButton.Capture = false;
        Cursor = Cursors.Default;
        var screen = Cursor.Position;
        var handle = NativeMethods.GetAncestor(NativeMethods.WindowFromPoint(new NativeMethods.POINT { X = screen.X, Y = screen.Y }), NativeMethods.GA_ROOT);
        if (handle == IntPtr.Zero || handle == Handle || IsChildWindowOfThisApp(handle)) { _status.Text = "ไม่ได้เลือกหน้าต่างภายนอก"; return; }
        AddExternalWindowTab(handle);
    }

    private bool IsChildWindowOfThisApp(IntPtr handle)
    {
        for (var current = handle; current != IntPtr.Zero; current = NativeMethods.GetParent(current)) if (current == Handle) return true;
        return false;
    }

    private void AddExternalWindowTab(IntPtr handle)
    {
        if (CurrentInnerTabs() is not { } inner) { MessageBox.Show("กรุณาสร้างหรือเลือก Workspace ก่อน", "แจ้งเตือน"); return; }
        var title = ExternalWindowHost.GetWindowTitle(handle);
        var page = new TabPage(ShortTitle(title));
        var host = new ExternalWindowHost();
        page.Controls.Add(host);
        inner.TabPages.Add(page);
        inner.SelectedTab = page;
        if (!host.Attach(handle))
        {
            inner.TabPages.Remove(page);
            page.Dispose();
            MessageBox.Show("ไม่สามารถฝังหน้าต่างนี้ได้", "ไม่รองรับ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var menu = new ContextMenuStrip();
        menu.Items.Add(L.T("นำหน้าต่างออกจาก Tab", "Detach window from tab"), null, (_, _) =>
        {
            host.Detach();
            inner.TabPages.Remove(page);
            page.Dispose();
            _status.Text = "นำหน้าต่างโปรแกรมกลับออกมาแล้ว";
        });
        menu.Items.Add(L.T("ปิดหน้าต่างโปรแกรม...", "Close external program..."), null, (_, _) =>
        {
            var answer = MessageBox.Show(
                $"ต้องการปิดโปรแกรม '{title}' หรือไม่?\n\nหากมีงานที่ยังไม่บันทึก โปรแกรมนั้นอาจแสดงหน้าต่างถามให้บันทึก",
                "ยืนยันการปิดโปรแกรม",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
            host.CloseExternalWindow();
            inner.TabPages.Remove(page);
            page.Dispose();
            _status.Text = $"ส่งคำสั่งปิด {title} แล้ว";
        });
        page.ContextMenuStrip = menu;
        ThemeManager.Apply(menu);
        _status.Text = $"เพิ่ม {title} เป็น Tab แล้ว";
    }

    private string ToNavigationUrl(string value)
    {
        value = value.Trim();
        var shortcuts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["g"] = "https://www.google.com/search?q=",
            ["b"] = "https://www.bing.com/search?q=",
            ["yt"] = "https://www.youtube.com/results?search_query=",
            ["maps"] = "https://www.google.com/maps/search/"
        };
        var firstSpace = value.IndexOf(' ');
        if (firstSpace > 0 && shortcuts.TryGetValue(value[..firstSpace], out var searchUrl))
            return searchUrl + Uri.EscapeDataString(value[(firstSpace + 1)..].Trim());
        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute) && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps)) return absolute.ToString();
        if (!value.Contains(' ') && (value.Equals("localhost", StringComparison.OrdinalIgnoreCase) || value.Contains('.') || value.Contains(':')))
            return "https://" + value;
        var engine = string.Equals(_config.Settings.SearchEngine, "Bing", StringComparison.OrdinalIgnoreCase)
            ? "https://www.bing.com/search?q="
            : "https://www.google.com/search?q=";
        return engine + Uri.EscapeDataString(value);
    }

    private static BrowserTab CloneTab(BrowserTab tab) => new()
    {
        Id = tab.Id, Name = tab.Name, Url = tab.Url, CurrentUrl = tab.CurrentUrl,
        IsOpen = tab.IsOpen, IsTemporary = tab.IsTemporary, IsPinned = tab.IsPinned
    };

    private static void UpdateBrowserTabCaption(TabPage page, BrowserTab tab, string? title = null)
    {
        if (!string.IsNullOrWhiteSpace(title)) tab.Name = title;
        page.ToolTipText = tab.Name;
        page.Text = tab.IsPinned ? "📌" : ShortTitle(tab.Name);
    }

    private static string ShortTitle(string title) => title.Length > 28 ? title[..28] + "…" : title;
    private static string Sanitize(string name) => string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
    private static Color ParseColor(string value, Color fallback)
    {
        try { return string.IsNullOrWhiteSpace(value) ? fallback : ColorTranslator.FromHtml(value); }
        catch { return fallback; }
    }
}
