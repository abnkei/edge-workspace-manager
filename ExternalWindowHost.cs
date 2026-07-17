using System.Runtime.InteropServices;
using System.Text;

namespace EdgeWorkspaceManager;

public sealed class ExternalWindowHost : Panel
{
    private IntPtr _windowHandle;
    private IntPtr _originalParent;
    private nint _originalStyle;

    public IntPtr WindowHandle => _windowHandle;

    public ExternalWindowHost()
    {
        Dock = DockStyle.Fill;
        BackColor = ThemeManager.ControlBack;
        Resize += (_, _) => ResizeEmbeddedWindow();
    }

    public bool Attach(IntPtr handle)
    {
        if (handle == IntPtr.Zero || !NativeMethods.IsWindow(handle)) return false;

        _windowHandle = handle;
        _originalParent = NativeMethods.GetParent(handle);
        _originalStyle = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_STYLE);

        var style = _originalStyle.ToInt64();
        style &= ~NativeMethods.WS_POPUP;
        style &= ~NativeMethods.WS_CAPTION;
        style &= ~NativeMethods.WS_THICKFRAME;
        style |= NativeMethods.WS_CHILD;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWL_STYLE, new IntPtr(style));
        NativeMethods.SetParent(handle, Handle);
        NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOW);
        ResizeEmbeddedWindow();
        return true;
    }

    public void Detach()
    {
        if (_windowHandle == IntPtr.Zero || !NativeMethods.IsWindow(_windowHandle)) return;
        NativeMethods.SetParent(_windowHandle, _originalParent);
        NativeMethods.SetWindowLongPtr(_windowHandle, NativeMethods.GWL_STYLE, _originalStyle);
        NativeMethods.ShowWindow(_windowHandle, NativeMethods.SW_RESTORE);
        _windowHandle = IntPtr.Zero;
    }

    public void CloseExternalWindow()
    {
        if (_windowHandle == IntPtr.Zero || !NativeMethods.IsWindow(_windowHandle)) return;
        var handle = _windowHandle;
        Detach();
        NativeMethods.PostMessage(handle, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Detach();
        base.Dispose(disposing);
    }

    private void ResizeEmbeddedWindow()
    {
        if (_windowHandle != IntPtr.Zero && NativeMethods.IsWindow(_windowHandle))
            NativeMethods.MoveWindow(_windowHandle, 0, 0, ClientSize.Width, ClientSize.Height, true);
    }

    public static string GetWindowTitle(IntPtr handle)
    {
        var length = NativeMethods.GetWindowTextLength(handle);
        var sb = new StringBuilder(Math.Max(length + 1, 256));
        NativeMethods.GetWindowText(handle, sb, sb.Capacity);
        return string.IsNullOrWhiteSpace(sb.ToString()) ? "External Program" : sb.ToString();
    }
}

internal static class NativeMethods
{
    public const int GWL_STYLE = -16;
    public const long WS_CHILD = 0x40000000L;
    public const long WS_POPUP = 0x80000000L;
    public const long WS_CAPTION = 0x00C00000L;
    public const long WS_THICKFRAME = 0x00040000L;
    public const int SW_SHOW = 5;
    public const int SW_RESTORE = 9;
    public const uint GA_ROOT = 2;
    public const uint WM_CLOSE = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll")]
    public static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

    [DllImport("user32.dll")]
    public static extern IntPtr GetParent(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    public static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    public static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll")]
    public static extern bool MoveWindow(IntPtr hwnd, int x, int y, int width, int height, bool repaint);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hwnd, int command);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
}
