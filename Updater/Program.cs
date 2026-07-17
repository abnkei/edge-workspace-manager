using System.Diagnostics;

namespace EdgeWorkspaceManager.Updater;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var options = Parse(args);
        if (!Required(options, "pid", "source", "target", "launch", "version", "result")) return 2;
        var version = options["version"];
        try
        {
            if (int.TryParse(options["pid"], out var pid))
            {
                try { Process.GetProcessById(pid).WaitForExit(30000); } catch { }
            }
            var source = Path.GetFullPath(options["source"]);
            var target = Path.GetFullPath(options["target"]);
            if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
            Directory.CreateDirectory(target);
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(source, file);
                var destination = Path.Combine(target, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination, true);
            }
            WriteResult(options["result"], version, "Installed", "Update installed successfully");
            Process.Start(new ProcessStartInfo(options["launch"]) { UseShellExecute = true, WorkingDirectory = target });
            return 0;
        }
        catch (Exception ex)
        {
            WriteResult(options["result"], version, "Failed", ex.Message.Replace('|', '/'));
            try { Process.Start(new ProcessStartInfo(options["launch"]) { UseShellExecute = true }); } catch { }
            return 1;
        }
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i + 1 < args.Length; i += 2)
            if (args[i].StartsWith("--")) values[args[i][2..]] = args[i + 1];
        return values;
    }

    private static bool Required(Dictionary<string, string> values, params string[] names) => names.All(values.ContainsKey);

    private static void WriteResult(string path, string version, string status, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, $"{version}|{status}|{message}");
        }
        catch { }
    }
}
