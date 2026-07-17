namespace EdgeWorkspaceManager;

public sealed class AboutForm : Form
{
    public AboutForm(Func<Task>? checkForUpdates = null, Action? showUpdateHistory = null)
    {
        Text = "ข้อมูลเวอร์ชันและการอัปเดต";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(680, 560);
        MinimumSize = new Size(560, 440);
        Font = new Font("Segoe UI", 10F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            RowCount = 4,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        root.Controls.Add(new Label
        {
            Text = "Edge Workspace Manager",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        root.Controls.Add(new Label
        {
            Text = $"เวอร์ชันปัจจุบัน  {AppInfo.Version}",
            Dock = DockStyle.Fill,
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        var notes = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = SystemColors.Window,
            DetectUrls = false,
            Text = BuildReleaseNotes()
        };
        root.Controls.Add(notes, 0, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 10, 0, 0) };
        var close = new Button { Text = "ปิด", Width = 100, Height = 34 };
        close.Click += (_, _) => Close();
        buttons.Controls.Add(close);
        if (showUpdateHistory is not null)
        {
            var history = new Button { Text = L.T("ประวัติอัปเดต", "Update history"), AutoSize = true, Height = 34 };
            history.Click += (_, _) => showUpdateHistory();
            buttons.Controls.Add(history);
        }
        if (checkForUpdates is not null)
        {
            var check = new Button { Text = L.T("ตรวจอัปเดต", "Check for updates"), AutoSize = true, Height = 34 };
            check.Click += async (_, _) => { check.Enabled = false; await checkForUpdates(); check.Enabled = true; };
            buttons.Controls.Add(check);
        }
        root.Controls.Add(buttons, 0, 3);
        Controls.Add(root);
        ThemeManager.Apply(this);
    }

    private static string BuildReleaseNotes()
    {
        var lines = new List<string>();
        foreach (var release in AppInfo.ReleaseNotes)
        {
            lines.Add($"เวอร์ชัน {release.Version}  •  {release.Date}");
            lines.Add(new string('─', 48));
            lines.AddRange(release.Changes.Select(change => "• " + change));
            lines.Add("");
        }
        return string.Join(Environment.NewLine, lines);
    }
}
