using System.Diagnostics;

namespace EdgeWorkspaceManager;

public sealed class AboutForm : Form
{
    private const string RepositoryUrl = "https://github.com/abnkei/edge-workspace-manager";

    public AboutForm(Func<Task>? checkForUpdates = null, Action? showUpdateHistory = null)
    {
        Text = "About Edge Workspace Manager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(620, 470);
        MinimumSize = new Size(560, 450);
        Font = new Font("Segoe UI", 10F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 22, 28, 18),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        root.Controls.Add(new Label
        {
            Text = "Edge Workspace Manager",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var details = CreateDetailsTable();
        AddValueRow(details, 0, "Current version", AppInfo.Version);
        AddValueRow(details, 1, "Update channel", "Stable");
        AddValueRow(details, 2, "Build commit", AppInfo.BuildCommit);
        AddSectionRow(details, 4, "Project");
        AddLinkRow(details, 5, "Repository", "github.com/abnkei/edge-workspace-manager", RepositoryUrl);
        AddLinkRow(details, 6, "Latest release", "View releases", RepositoryUrl + "/releases");
        AddLinkRow(details, 7, "Support", "Report an issue", RepositoryUrl + "/issues/new");
        AddDocumentRow(details, 8, "Licenses", "Third-party notices", "THIRD-PARTY-NOTICES.md");
        root.Controls.Add(details, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0)
        };
        var close = new Button { Text = "Close", Width = 100, Height = 34 };
        close.Click += (_, _) => Close();
        buttons.Controls.Add(close);

        if (showUpdateHistory is not null)
        {
            var history = new Button { Text = "Update history", AutoSize = true, Height = 34 };
            history.Click += (_, _) => showUpdateHistory();
            buttons.Controls.Add(history);
        }

        if (checkForUpdates is not null)
        {
            var check = new Button { Text = "Check for updates", AutoSize = true, Height = 34 };
            check.Click += async (_, _) =>
            {
                check.Enabled = false;
                try { await checkForUpdates(); }
                finally { check.Enabled = true; }
            };
            buttons.Controls.Add(check);
        }

        root.Controls.Add(buttons, 0, 3);
        Controls.Add(root);
        AcceptButton = close;
        CancelButton = close;
        ThemeManager.Apply(this);
    }

    private static TableLayoutPanel CreateDetailsTable()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 9,
            Margin = new Padding(0)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < 9; row++)
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, row == 3 ? 22 : 34));
        return table;
    }

    private static void AddValueRow(TableLayoutPanel table, int row, string label, string value)
    {
        table.Controls.Add(CreateLabel(label, Color.DimGray), 0, row);
        table.Controls.Add(CreateLabel(value, SystemColors.ControlText), 1, row);
    }

    private static void AddSectionRow(TableLayoutPanel table, int row, string text)
    {
        var label = CreateLabel(text, SystemColors.ControlText);
        label.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
        table.Controls.Add(label, 0, row);
        table.SetColumnSpan(label, 2);
    }

    private static void AddLinkRow(TableLayoutPanel table, int row, string label, string text, string url)
    {
        table.Controls.Add(CreateLabel(label, Color.DimGray), 0, row);
        var link = new LinkLabel
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        link.LinkClicked += (_, _) => OpenUrl(url);
        table.Controls.Add(link, 1, row);
    }

    private static void AddDocumentRow(TableLayoutPanel table, int row, string label, string text, string fileName)
    {
        table.Controls.Add(CreateLabel(label, Color.DimGray), 0, row);
        var link = new LinkLabel
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        link.LinkClicked += (_, _) => OpenDocument(fileName);
        table.Controls.Add(link, 1, row);
    }

    private static Label CreateLabel(string text, Color color) => new()
    {
        Text = text,
        ForeColor = color,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to open the link.\r\n{ex.Message}", "Edge Workspace Manager",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void OpenDocument(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(path))
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open the document.\r\n{ex.Message}", "Edge Workspace Manager",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return;
        }

        OpenUrl(RepositoryUrl + "/blob/main/" + fileName);
    }
}
