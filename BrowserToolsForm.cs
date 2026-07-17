using System.ComponentModel;

namespace EdgeWorkspaceManager;

public sealed class BrowserToolsForm : Form
{
    private readonly WorkspaceConfig _config;
    private readonly BindingList<DownloadRecord> _downloads;
    private readonly Action<string> _openUrl;
    private readonly Action _save;
    private readonly DataGridView _favorites = CreateGrid();
    private readonly DataGridView _history = CreateGrid();
    private readonly DataGridView _downloadGrid = CreateGrid();
    private readonly ComboBox _searchEngine = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly TextBox _downloadFolder = new() { Width = 420 };
    private readonly CheckBox _askDownload = new() { Text = "ถามตำแหน่งบันทึกก่อนดาวน์โหลด", AutoSize = true };
    private readonly CheckBox _devTools = new() { Text = "เปิดใช้งาน Developer Tools", AutoSize = true };
    private readonly NumericUpDown _maxHistory = new() { Minimum = 100, Maximum = 10000, Increment = 100, Width = 120 };
    private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly CheckBox _addressSuggestionsEnabled = new() { Text = "แสดงคำแนะนำขณะพิมพ์ URL", AutoSize = true };
    private readonly CheckBox _allInstanceSuggestions = new() { Text = "ค้นหาคำแนะนำจากทุก Instance", AutoSize = true };
    private readonly TextBox _focusedTabColor = new() { Width = 140, ReadOnly = true };
    private readonly ComboBox _theme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly CheckBox _forceDarkWebPages = new() { Text = "Force dark web pages (มีผลเมื่อเปิดโปรแกรมใหม่)", AutoSize = true };
    private readonly CheckBox _checkForUpdates = new() { Text = "ตรวจสอบอัปเดตอัตโนมัติ", AutoSize = true };
    private readonly System.Windows.Forms.Timer _downloadRefreshTimer = new() { Interval = 500 };

    public BrowserToolsForm(WorkspaceConfig config, BindingList<DownloadRecord> downloads, Action<string> openUrl, Action save, int selectedTab = 0)
    {
        _config = config;
        _downloads = downloads;
        _openUrl = openUrl;
        _save = save;
        Text = L.T("เครื่องมือ Browser", "Browser Tools");
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(900, 620);
        MinimumSize = new Size(720, 480);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildFavoritesPage());
        tabs.TabPages.Add(BuildHistoryPage());
        tabs.TabPages.Add(BuildDownloadsPage());
        tabs.TabPages.Add(BuildSettingsPage());
        tabs.SelectedIndex = Math.Clamp(selectedTab, 0, tabs.TabCount - 1);
        Controls.Add(tabs);
        LoadData();
        ThemeManager.Apply(this);
        _downloadRefreshTimer.Tick += (_, _) => RefreshDownloadRows();
        _downloadRefreshTimer.Start();
        FormClosed += (_, _) => _downloadRefreshTimer.Stop();
    }

    private TabPage BuildFavoritesPage()
    {
        var page = new TabPage("Favorites");
        _favorites.Columns.Add("Title", "ชื่อ");
        _favorites.Columns.Add("Url", "URL");
        _favorites.Columns.Add("Workspace", "Workspace");
        _favorites.Columns.Add("Added", "เพิ่มเมื่อ");
        _favorites.Columns[0].FillWeight = 30;
        _favorites.Columns[1].FillWeight = 45;
        _favorites.Columns[2].FillWeight = 15;
        _favorites.Columns[3].FillWeight = 15;
        _favorites.CellDoubleClick += (_, e) => OpenSelected(_favorites, e.RowIndex, 1);
        page.Controls.Add(BuildGridLayout(_favorites,
            (L.T("เปิด", "Open"), () => OpenCurrent(_favorites, 1)),
            (L.T("ลบ", "Delete"), DeleteFavorite)));
        return page;
    }

    private TabPage BuildHistoryPage()
    {
        var page = new TabPage("History");
        _history.Columns.Add("Title", "ชื่อหน้า");
        _history.Columns.Add("Url", "URL");
        _history.Columns.Add("Workspace", "Workspace");
        _history.Columns.Add("Visited", "เวลาเข้าใช้งาน");
        _history.Columns[0].FillWeight = 30;
        _history.Columns[1].FillWeight = 45;
        _history.Columns[2].FillWeight = 15;
        _history.Columns[3].FillWeight = 18;
        _history.CellDoubleClick += (_, e) => OpenSelected(_history, e.RowIndex, 1);
        page.Controls.Add(BuildGridLayout(_history,
            (L.T("เปิด", "Open"), () => OpenCurrent(_history, 1)),
            (L.T("ลบรายการ", "Delete item"), DeleteHistory),
            (L.T("ล้างทั้งหมด", "Clear all"), ClearHistory)));
        return page;
    }

    private TabPage BuildDownloadsPage()
    {
        var page = new TabPage("Downloads");
        _downloadGrid.Columns.Add("File", "ไฟล์");
        _downloadGrid.Columns.Add("Progress", "ความคืบหน้า");
        _downloadGrid.Columns.Add("Status", "สถานะ");
        _downloadGrid.Columns.Add("Path", "ตำแหน่ง");
        _downloadGrid.Columns[0].FillWeight = 28;
        _downloadGrid.Columns[1].FillWeight = 12;
        _downloadGrid.Columns[2].FillWeight = 15;
        _downloadGrid.Columns[3].FillWeight = 45;
        page.Controls.Add(BuildGridLayout(_downloadGrid,
            (L.T("เปิดไฟล์", "Open file"), OpenDownloadedFile),
            (L.T("เปิดโฟลเดอร์", "Open folder"), OpenDownloadFolder),
            (L.T("พัก", "Pause"), () => CurrentDownload()?.Pause?.Invoke()),
            (L.T("ทำต่อ", "Resume"), () => CurrentDownload()?.Resume?.Invoke()),
            (L.T("ยกเลิก", "Cancel"), () => CurrentDownload()?.Cancel?.Invoke()),
            (L.T("ล้างรายการ", "Clear list"), () => { _downloads.Clear(); LoadDownloads(); })));
        return page;
    }

    private TabPage BuildSettingsPage()
    {
        var page = new TabPage("Settings");
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(24), ColumnCount = 3, RowCount = 12 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        _searchEngine.Items.AddRange(["Google", "Bing"]);
        layout.Controls.Add(LabelFor("Search Engine"), 0, 0);
        layout.Controls.Add(_searchEngine, 1, 0);
        layout.Controls.Add(LabelFor("Download Folder"), 0, 1);
        layout.Controls.Add(_downloadFolder, 1, 1);
        var browse = new Button { Text = "เลือก...", Width = 90 };
        browse.Click += (_, _) => ChooseDownloadFolder();
        layout.Controls.Add(browse, 2, 1);
        layout.Controls.Add(_askDownload, 1, 2);
        layout.Controls.Add(_devTools, 1, 3);
        layout.Controls.Add(LabelFor("จำนวน History สูงสุด"), 0, 4);
        layout.Controls.Add(_maxHistory, 1, 4);
        _language.Items.AddRange(["ไทย", "English"]);
        layout.Controls.Add(LabelFor(L.T("ภาษา", "Language")), 0, 5);
        layout.Controls.Add(_language, 1, 5);
        layout.Controls.Add(_addressSuggestionsEnabled, 1, 6);
        layout.Controls.Add(_allInstanceSuggestions, 1, 7);
        layout.Controls.Add(LabelFor(L.T("สี Tab ที่ Focus", "Focused tab color")), 0, 8);
        var colorPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var chooseColor = new Button { Text = L.T("เลือกสี...", "Choose..."), Width = 100 };
        chooseColor.Click += (_, _) => ChooseFocusedTabColor();
        var resetColor = new Button { Text = L.T("ค่าเริ่มต้น", "Default"), Width = 100 };
        resetColor.Click += (_, _) => { _focusedTabColor.Text = "#0078D4"; UpdateFocusedColorPreview(); };
        colorPanel.Controls.Add(_focusedTabColor);
        colorPanel.Controls.Add(chooseColor);
        colorPanel.Controls.Add(resetColor);
        layout.Controls.Add(colorPanel, 1, 8);
        _theme.Items.AddRange(["Use Windows setting", "Light", "Dark"]);
        layout.Controls.Add(LabelFor(L.T("Theme", "Theme")), 0, 9);
        layout.Controls.Add(_theme, 1, 9);
        layout.Controls.Add(_forceDarkWebPages, 1, 10);
        layout.Controls.Add(_checkForUpdates, 1, 11);
        var save = new Button { Text = L.T("บันทึกการตั้งค่า", "Save settings"), Width = 150, Height = 38 };
        save.Click += (_, _) => SaveSettings();
        AcceptButton = save;
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        scroll.Controls.Add(layout);
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12, 8, 24, 8),
            WrapContents = false
        };
        footer.Controls.Add(save);
        var outer = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        outer.Controls.Add(scroll, 0, 0);
        outer.Controls.Add(footer, 0, 1);
        page.Controls.Add(outer);
        return page;
    }

    private static Control BuildGridLayout(DataGridView grid, params (string Text, Action Click)[] buttons)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        panel.Controls.Add(grid, 0, 0);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
        foreach (var item in buttons)
        {
            var button = new Button { Text = item.Text, AutoSize = true, Height = 34 };
            button.Click += (_, _) => item.Click();
            actions.Controls.Add(button);
        }
        panel.Controls.Add(actions, 0, 1);
        return panel;
    }

    private void LoadData()
    {
        _favorites.Rows.Clear();
        foreach (var item in _config.Favorites.OrderByDescending(x => x.AddedAt))
            _favorites.Rows.Add(item.Title, item.Url, WorkspaceName(item.WorkspaceId), item.AddedAt.ToString("dd/MM/yyyy HH:mm"));
        _history.Rows.Clear();
        foreach (var item in _config.History.OrderByDescending(x => x.VisitedAt))
            _history.Rows.Add(item.Title, item.Url, WorkspaceName(item.WorkspaceId), item.VisitedAt.ToString("dd/MM/yyyy HH:mm:ss"));
        LoadDownloads();
        _searchEngine.SelectedItem = _config.Settings.SearchEngine;
        if (_searchEngine.SelectedIndex < 0) _searchEngine.SelectedIndex = 0;
        _downloadFolder.Text = _config.Settings.DownloadFolder;
        _askDownload.Checked = _config.Settings.AskWhereToSaveDownloads;
        _devTools.Checked = _config.Settings.DevToolsEnabled;
        _maxHistory.Value = Math.Clamp(_config.Settings.MaxHistoryItems, 100, 10000);
        _language.SelectedIndex = string.Equals(_config.Settings.Language, "en", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _addressSuggestionsEnabled.Checked = _config.Settings.AddressSuggestionsEnabled;
        _allInstanceSuggestions.Checked = _config.Settings.SearchSuggestionsAcrossInstances;
        _focusedTabColor.Text = _config.Settings.FocusedTabColorHex;
        UpdateFocusedColorPreview();
        _theme.SelectedIndex = _config.Settings.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        _forceDarkWebPages.Checked = _config.Settings.ForceDarkWebPages;
        _checkForUpdates.Checked = _config.Settings.CheckForUpdates;
    }

    public void LoadDownloads()
    {
        _downloadGrid.Rows.Clear();
        foreach (var item in _downloads.OrderByDescending(x => x.StartedAt))
        {
            var index = _downloadGrid.Rows.Add(item.FileName, $"{item.Progress:0}%", item.Status, item.Path);
            _downloadGrid.Rows[index].Tag = item;
        }
    }

    private void RefreshDownloadRows()
    {
        foreach (DataGridViewRow row in _downloadGrid.Rows)
            if (row.Tag is DownloadRecord item)
            {
                row.Cells[0].Value = item.FileName;
                row.Cells[1].Value = $"{item.Progress:0}%";
                row.Cells[2].Value = item.Status;
                row.Cells[3].Value = item.Path;
            }
    }

    private DownloadRecord? CurrentDownload() => _downloadGrid.CurrentRow?.Tag as DownloadRecord;

    private void DeleteFavorite()
    {
        if (_favorites.CurrentRow is null) return;
        var url = Convert.ToString(_favorites.CurrentRow.Cells[1].Value);
        var item = _config.Favorites.FirstOrDefault(x => x.Url == url);
        if (item is not null) _config.Favorites.Remove(item);
        _save();
        LoadData();
    }

    private void DeleteHistory()
    {
        if (_history.CurrentRow is null) return;
        var url = Convert.ToString(_history.CurrentRow.Cells[1].Value);
        var timeText = Convert.ToString(_history.CurrentRow.Cells[3].Value);
        var item = _config.History.FirstOrDefault(x => x.Url == url && x.VisitedAt.ToString("dd/MM/yyyy HH:mm:ss") == timeText);
        if (item is not null) _config.History.Remove(item);
        _save();
        LoadData();
    }

    private void ClearHistory()
    {
        if (MessageBox.Show("ล้าง History ทั้งหมด?", "ยืนยัน", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _config.History.Clear();
        _save();
        LoadData();
    }

    private void OpenCurrent(DataGridView grid, int urlColumn)
    {
        if (grid.CurrentRow is null) return;
        var url = Convert.ToString(grid.CurrentRow.Cells[urlColumn].Value);
        if (!string.IsNullOrWhiteSpace(url)) _openUrl(url);
    }

    private void OpenSelected(DataGridView grid, int row, int urlColumn)
    {
        if (row < 0) return;
        var url = Convert.ToString(grid.Rows[row].Cells[urlColumn].Value);
        if (!string.IsNullOrWhiteSpace(url)) _openUrl(url);
    }

    private void OpenDownloadedFile()
    {
        var path = CurrentDownload()?.Path;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void OpenDownloadFolder()
    {
        var path = CurrentDownload()?.Path;
        var folder = string.IsNullOrWhiteSpace(path) ? null : Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder)) System.Diagnostics.Process.Start("explorer.exe", folder);
    }

    private void ChooseDownloadFolder()
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = _downloadFolder.Text, Description = "เลือกโฟลเดอร์ Download" };
        if (dialog.ShowDialog(this) == DialogResult.OK) _downloadFolder.Text = dialog.SelectedPath;
    }

    private void SaveSettings()
    {
        _config.Settings.SearchEngine = Convert.ToString(_searchEngine.SelectedItem) ?? "Google";
        _config.Settings.DownloadFolder = _downloadFolder.Text.Trim();
        _config.Settings.AskWhereToSaveDownloads = _askDownload.Checked;
        _config.Settings.DevToolsEnabled = _devTools.Checked;
        _config.Settings.MaxHistoryItems = (int)_maxHistory.Value;
        _config.Settings.Language = _language.SelectedIndex == 1 ? "en" : "th";
        _config.Settings.AddressSuggestionsEnabled = _addressSuggestionsEnabled.Checked;
        _config.Settings.SearchSuggestionsAcrossInstances = _allInstanceSuggestions.Checked;
        _config.Settings.FocusedTabColorHex = string.IsNullOrWhiteSpace(_focusedTabColor.Text) ? "#0078D4" : _focusedTabColor.Text;
        _config.Settings.Theme = _theme.SelectedIndex switch { 1 => "Light", 2 => "Dark", _ => "System" };
        _config.Settings.ForceDarkWebPages = _forceDarkWebPages.Checked;
        _config.Settings.CheckForUpdates = _checkForUpdates.Checked;
        L.SetLanguage(_config.Settings.Language);
        ThemeManager.Configure(_config.Settings.Theme);
        ThemeManager.Apply(this);
        _save();
        MessageBox.Show(L.T(
            "บันทึกการตั้งค่าแล้ว การตั้งค่าภาษาและ DevTools จะมีผลครบถ้วนเมื่อเปิดโปรแกรมครั้งถัดไป",
            "Settings saved. Language and DevTools changes will fully apply the next time the app starts."), "Settings");
    }

    private void ChooseFocusedTabColor()
    {
        using var dialog = new ColorDialog { FullOpen = true, Color = ParseColor(_focusedTabColor.Text, Color.FromArgb(0, 120, 212)) };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _focusedTabColor.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        UpdateFocusedColorPreview();
    }

    private void UpdateFocusedColorPreview()
    {
        var color = ParseColor(_focusedTabColor.Text, Color.FromArgb(0, 120, 212));
        _focusedTabColor.BackColor = color;
        _focusedTabColor.ForeColor = color.GetBrightness() < 0.55f ? Color.White : Color.Black;
    }

    private static Color ParseColor(string value, Color fallback)
    {
        try { return string.IsNullOrWhiteSpace(value) ? fallback : ColorTranslator.FromHtml(value); }
        catch { return fallback; }
    }

    private string WorkspaceName(string id) => _config.Workspaces.FirstOrDefault(x => x.Id == id)?.Name ?? "-";
    private static Label LabelFor(string text) => new() { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };
}
