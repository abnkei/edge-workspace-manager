namespace EdgeWorkspaceManager;

public enum UpdateChoice { None, UpdateNow, RemindLater, SkipVersion }

public sealed class UpdatePromptForm : Form
{
    public UpdateChoice Choice { get; private set; }

    public UpdatePromptForm(UpdateManifest update)
    {
        Text = L.T("มีอัปเดตใหม่", "Update available");
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(650, 520);
        MinimumSize = new Size(560, 440);
        Font = new Font("Segoe UI", 10F);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 5, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.Controls.Add(new Label { Text = L.T("มีอัปเดตใหม่พร้อมใช้งาน", "A new update is ready"), Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 18F), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        root.Controls.Add(new Label { Text = $"{L.T("เวอร์ชันปัจจุบัน", "Current version")}: {AppInfo.Version}\r\n{L.T("เวอร์ชันใหม่", "New version")}: {update.Version}  •  {update.ReleaseDate:dd/MM/yyyy}  •  {FormatSize(update.Size)}", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        var notes = L.IsEnglish ? update.ReleaseNotesEn : update.ReleaseNotesTh;
        root.Controls.Add(new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, Text = string.Join(Environment.NewLine, notes.Select(x => "• " + x)), BorderStyle = BorderStyle.FixedSingle }, 0, 2);
        root.Controls.Add(new Label { Text = L.T("โปรแกรมจะบันทึก Tab และ Session ก่อนติดตั้ง", "Tabs and sessions will be saved before installation."), Dock = DockStyle.Fill }, 0, 3);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 9, 0, 0) };
        actions.Controls.Add(ButtonFor(L.T("อัปเดตตอนนี้", "Update now"), UpdateChoice.UpdateNow));
        actions.Controls.Add(ButtonFor(L.T("เตือนภายหลัง", "Remind me later"), UpdateChoice.RemindLater));
        actions.Controls.Add(ButtonFor(L.T("ข้ามเวอร์ชันนี้", "Skip this version"), UpdateChoice.SkipVersion));
        root.Controls.Add(actions, 0, 4);
        Controls.Add(root);
        ThemeManager.Apply(this);
    }

    private Button ButtonFor(string text, UpdateChoice choice)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 36, MinimumSize = new Size(130, 36) };
        button.Click += (_, _) => { Choice = choice; DialogResult = DialogResult.OK; Close(); };
        return button;
    }

    private static string FormatSize(long bytes) => bytes <= 0 ? "-" : $"{bytes / 1024d / 1024d:0.0} MB";
}

public sealed class UpdateDownloadForm : Form
{
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Top, Height = 26 };
    private readonly Label _message = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    public string? StagingPath { get; private set; }

    public UpdateDownloadForm(UpdateService service, UpdateManifest update)
    {
        Text = L.T("กำลังอัปเดต", "Installing update");
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(520, 190);
        ControlBox = false;
        Font = new Font("Segoe UI", 10F);
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 2 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(_progress, 0, 0);
        panel.Controls.Add(_message, 0, 1);
        Controls.Add(panel);
        Shown += async (_, _) =>
        {
            try
            {
                var progress = new Progress<UpdateProgress>(p => { _progress.Value = p.Percentage; _message.Text = p.Message; });
                StagingPath = await service.DownloadAndStageAsync(update, progress);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, L.T("อัปเดตไม่สำเร็จ", "Update failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Abort;
            }
            Close();
        };
        ThemeManager.Apply(this);
    }
}

public sealed class UpdateHistoryForm : Form
{
    public UpdateHistoryForm(IEnumerable<UpdateHistoryItem> history)
    {
        Text = L.T("ประวัติการอัปเดต", "Update history");
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(760, 460);
        var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        grid.Columns.Add("Version", L.T("เวอร์ชัน", "Version"));
        grid.Columns.Add("Date", L.T("วันที่", "Date"));
        grid.Columns.Add("Status", L.T("สถานะ", "Status"));
        grid.Columns.Add("Message", L.T("รายละเอียด", "Details"));
        foreach (var item in history.OrderByDescending(x => x.TimestampUtc)) grid.Rows.Add(item.Version, item.TimestampUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), item.Status, item.Message);
        Controls.Add(grid);
        ThemeManager.Apply(this);
    }
}
