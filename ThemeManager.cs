using Microsoft.Win32;

namespace EdgeWorkspaceManager;

public static class ThemeManager
{
    public static string Mode { get; private set; } = "System";
    public static bool IsDark { get; private set; }
    public static Color WindowBack => IsDark ? Color.FromArgb(31, 31, 31) : SystemColors.Window;
    public static Color ControlBack => IsDark ? Color.FromArgb(45, 45, 48) : SystemColors.Control;
    public static Color InputBack => IsDark ? Color.FromArgb(38, 38, 38) : SystemColors.Window;
    public static Color Foreground => IsDark ? Color.FromArgb(238, 238, 238) : SystemColors.ControlText;
    public static Color Muted => IsDark ? Color.FromArgb(170, 170, 170) : Color.DimGray;
    public static Color Border => IsDark ? Color.FromArgb(80, 80, 80) : SystemColors.ControlDark;

    public static bool Configure(string? mode)
    {
        Mode = mode is "Light" or "Dark" ? mode : "System";
        var next = Mode == "Dark" || (Mode == "System" && WindowsUsesDarkTheme());
        var changed = next != IsDark;
        IsDark = next;
        return changed;
    }

    public static bool RefreshSystemTheme() => Mode == "System" && Configure("System");

    public static void Apply(Control root)
    {
        ApplyControl(root);
        foreach (Control child in root.Controls) Apply(child);
    }

    private static void ApplyControl(Control control)
    {
        control.ForeColor = Foreground;
        switch (control)
        {
            case Form:
            case Panel:
                control.BackColor = ControlBack;
                break;
            case TextBoxBase text:
                text.BackColor = InputBack;
                text.ForeColor = Foreground;
                break;
            case ListBox list:
                list.BackColor = InputBack;
                list.ForeColor = Foreground;
                break;
            case ComboBox combo:
                combo.BackColor = InputBack;
                combo.ForeColor = Foreground;
                break;
            case NumericUpDown number:
                number.BackColor = InputBack;
                number.ForeColor = Foreground;
                break;
            case Button button:
                button.UseVisualStyleBackColor = !IsDark;
                button.BackColor = ControlBack;
                button.ForeColor = Foreground;
                button.FlatStyle = IsDark ? FlatStyle.Flat : FlatStyle.Standard;
                if (IsDark) button.FlatAppearance.BorderColor = Border;
                break;
            case DataGridView grid:
                grid.BackgroundColor = ControlBack;
                grid.GridColor = Border;
                grid.DefaultCellStyle.BackColor = InputBack;
                grid.DefaultCellStyle.ForeColor = Foreground;
                grid.DefaultCellStyle.SelectionBackColor = IsDark ? Color.FromArgb(0, 90, 158) : SystemColors.Highlight;
                grid.DefaultCellStyle.SelectionForeColor = Color.White;
                grid.ColumnHeadersDefaultCellStyle.BackColor = ControlBack;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Foreground;
                grid.EnableHeadersVisualStyles = !IsDark;
                break;
            case StatusStrip status:
                status.BackColor = ControlBack;
                status.ForeColor = Foreground;
                status.Renderer = new ToolStripProfessionalRenderer(new ThemeColorTable());
                break;
            case ContextMenuStrip menu:
                menu.BackColor = ControlBack;
                menu.ForeColor = Foreground;
                menu.Renderer = new ToolStripProfessionalRenderer(new ThemeColorTable());
                break;
        }
    }

    private static bool WindowsUsesDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 1)) == 0;
        }
        catch { return false; }
    }

    private sealed class ThemeColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => ControlBack;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Border;
        public override Color MenuItemSelected => IsDark ? Color.FromArgb(62, 62, 64) : SystemColors.Highlight;
        public override Color ImageMarginGradientBegin => ControlBack;
        public override Color ImageMarginGradientMiddle => ControlBack;
        public override Color ImageMarginGradientEnd => ControlBack;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => ControlBack;
    }
}
