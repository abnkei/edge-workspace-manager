namespace EdgeWorkspaceManager;

public sealed class WorkspaceEditorForm : Form
{
    private readonly BrowserWorkspace _workspace;
    private readonly TextBox _nameBox = new();
    private readonly TextBox _profileBox = new();
    private readonly DataGridView _tabsGrid = new();
    private readonly Button _colorButton = new() { Text = "เลือกสี...", Width = 120 };
    private string _colorHex = "";
    private readonly IReadOnlyList<string> _externalTabNames;
    private readonly IReadOnlyList<BrowserTab> _activeBrowserTabs;

    public bool DeleteRequested { get; private set; }

    public WorkspaceEditorForm(
        BrowserWorkspace workspace,
        bool isNew,
        IReadOnlyList<string>? externalTabNames = null,
        IReadOnlyList<BrowserTab>? activeBrowserTabs = null)
    {
        _workspace = workspace;
        _externalTabNames = externalTabNames ?? Array.Empty<string>();
        _activeBrowserTabs = activeBrowserTabs ?? workspace.Tabs.Where(tab => tab.IsOpen).ToList();
        Text = isNew ? "เพิ่ม Edge Instance" : "แก้ไข Edge Instance";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(760, 540);
        MinimumSize = new Size(680, 460);
        Font = new Font("Segoe UI", 10F);

        BuildLayout(isNew);
        LoadValues();
        ThemeManager.Apply(this);
    }

    private void BuildLayout(bool isNew)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 6
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));

        root.Controls.Add(new Label { Text = "ชื่อ Instance", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _nameBox.Dock = DockStyle.Fill;
        root.Controls.Add(_nameBox, 1, 0);

        root.Controls.Add(new Label { Text = "Profile Folder", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _profileBox.Dock = DockStyle.Fill;
        root.Controls.Add(_profileBox, 1, 1);

        root.Controls.Add(new Label { Text = L.T("สี Instance", "Instance Color"), AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        _colorButton.Click += (_, _) => ChooseColor();
        var colorActions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var clearColor = new Button { Text = L.T("ล้างสี", "Reset color"), Width = 100 };
        clearColor.Click += (_, _) => { _colorHex = ""; UpdateColorButton(); };
        colorActions.Controls.Add(_colorButton);
        colorActions.Controls.Add(clearColor);
        root.Controls.Add(colorActions, 1, 2);

        var tabHeader = new Label
        {
            Text = "Tab ที่เปิดอยู่ (URL ปัจจุบันจากหน้าเว็บ)",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            Anchor = AnchorStyles.Left
        };
        root.Controls.Add(tabHeader, 0, 3);
        root.SetColumnSpan(tabHeader, 2);

        _tabsGrid.Dock = DockStyle.Fill;
        _tabsGrid.AllowUserToAddRows = true;
        _tabsGrid.AllowUserToDeleteRows = true;
        _tabsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _tabsGrid.RowHeadersVisible = false;
        _tabsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TabId", Visible = false });
        _tabsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ชื่อ Tab", FillWeight = 35 });
        _tabsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "URL", FillWeight = 65 });
        root.Controls.Add(_tabsGrid, 0, 4);
        root.SetColumnSpan(_tabsGrid, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };

        var save = new Button { Text = "บันทึก", Width = 100, Height = 36 };
        save.Click += (_, _) => SaveAndClose();
        var cancel = new Button { Text = "ยกเลิก", Width = 100, Height = 36 };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);

        if (!isNew)
        {
            var delete = new Button { Text = "ลบ Instance", Width = 110, Height = 36, ForeColor = Color.DarkRed };
            delete.Click += (_, _) =>
            {
                if (MessageBox.Show("ยืนยันการลบ Instance นี้?", "ยืนยัน", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    DeleteRequested = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };
            buttons.Controls.Add(delete);
        }

        root.Controls.Add(buttons, 0, 5);
        root.SetColumnSpan(buttons, 2);
        Controls.Add(root);
    }

    private void LoadValues()
    {
        _nameBox.Text = _workspace.Name;
        _profileBox.Text = _workspace.ProfileFolder;
        _colorHex = _workspace.ColorHex;
        UpdateColorButton();
        foreach (var tab in _activeBrowserTabs)
            _tabsGrid.Rows.Add(tab.Id, tab.Name, tab.CurrentUrl ?? tab.Url);
        foreach (var name in _externalTabNames)
        {
            var index = _tabsGrid.Rows.Add("external:" + Guid.NewGuid().ToString("N"), name, "[หน้าต่างโปรแกรมภายนอก]");
            _tabsGrid.Rows[index].ReadOnly = true;
            _tabsGrid.Rows[index].DefaultCellStyle.BackColor = Color.Gainsboro;
            _tabsGrid.Rows[index].DefaultCellStyle.ForeColor = Color.DimGray;
        }
    }

    private void SaveAndClose()
    {
        var name = _nameBox.Text.Trim();
        var profile = _profileBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(profile))
        {
            MessageBox.Show("กรุณากรอกชื่อ Instance และ Profile Folder", "ข้อมูลไม่ครบ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var tabs = new List<BrowserTab>();
        var existingTabs = _workspace.Tabs.ToDictionary(tab => tab.Id);
        foreach (DataGridViewRow row in _tabsGrid.Rows)
        {
            if (row.IsNewRow) continue;
            var tabId = Convert.ToString(row.Cells[0].Value)?.Trim() ?? "";
            var tabName = Convert.ToString(row.Cells[1].Value)?.Trim() ?? "";
            var url = Convert.ToString(row.Cells[2].Value)?.Trim() ?? "";
            if (tabId.StartsWith("external:", StringComparison.Ordinal)) continue;
            if (string.IsNullOrWhiteSpace(url)) continue;
            var tab = existingTabs.TryGetValue(tabId, out var existing) ? existing : new BrowserTab();
            tab.Name = string.IsNullOrWhiteSpace(tabName) ? $"Tab {tabs.Count + 1}" : tabName;
            tab.Url = url;
            tab.CurrentUrl = url;
            tab.IsOpen = true;
            tabs.Add(tab);
        }

        if (tabs.Count == 0)
        {
            MessageBox.Show("กรุณาเพิ่ม URL อย่างน้อย 1 รายการ", "ยังไม่มี Tab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _workspace.Name = name;
        _workspace.ProfileFolder = profile;
        _workspace.ColorHex = _colorHex;
        _workspace.Tabs = tabs;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ChooseColor()
    {
        using var dialog = new ColorDialog { FullOpen = true, Color = ParseColor(_colorHex, SystemColors.Control) };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _colorHex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        UpdateColorButton();
    }

    private void UpdateColorButton()
    {
        var color = ParseColor(_colorHex, SystemColors.Control);
        _colorButton.BackColor = color;
        _colorButton.ForeColor = color.GetBrightness() < 0.5f ? Color.White : Color.Black;
        _colorButton.Text = string.IsNullOrWhiteSpace(_colorHex) ? L.T("เลือกสี...", "Choose...") : _colorHex;
    }

    private static Color ParseColor(string value, Color fallback)
    {
        try { return string.IsNullOrWhiteSpace(value) ? fallback : ColorTranslator.FromHtml(value); }
        catch { return fallback; }
    }
}
