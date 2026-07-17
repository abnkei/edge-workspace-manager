using System.Runtime.InteropServices;

namespace EdgeWorkspaceManager;

public sealed class PowerAwake : IDisposable
{
    [Flags]
    private enum ExecutionState : uint
    {
        Continuous = 0x80000000,
        SystemRequired = 0x00000001,
        DisplayRequired = 0x00000002
    }

    [DllImport("kernel32.dll")]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState executionState);

    public bool IsActive { get; private set; }
    public bool KeepDisplayOn { get; private set; }
    public DateTime? EndsAt { get; private set; }

    public void Enable(bool keepDisplayOn, TimeSpan? duration)
    {
        Disable();
        var state = ExecutionState.Continuous | ExecutionState.SystemRequired;
        if (keepDisplayOn) state |= ExecutionState.DisplayRequired;
        if (SetThreadExecutionState(state) == 0) throw new InvalidOperationException("Windows ไม่อนุญาตให้เปิด Keep Awake");
        IsActive = true;
        KeepDisplayOn = keepDisplayOn;
        EndsAt = duration.HasValue ? DateTime.Now.Add(duration.Value) : null;
    }

    public bool Disable()
    {
        var wasActive = IsActive;
        SetThreadExecutionState(ExecutionState.Continuous);
        IsActive = false;
        KeepDisplayOn = false;
        EndsAt = null;
        return wasActive;
    }

    public void Dispose() => Disable();
}

public static class L
{
    public static string Language { get; private set; } = "th";
    public static bool IsEnglish => string.Equals(Language, "en", StringComparison.OrdinalIgnoreCase);
    public static void SetLanguage(string? language) => Language = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "th";
    public static string T(string thai, string english) => IsEnglish ? english : thai;
}
