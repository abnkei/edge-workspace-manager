using System.Diagnostics;
using System.Text.Json;

namespace EdgeWorkspaceManager.Updater;

internal static class Program
{
    private sealed class RecoveryJournal
    {
        public string FromVersion { get; set; } = "";
        public string ToVersion { get; set; } = "";
        public string Target { get; set; } = "";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Preparing";
        public List<string> BackedUpFiles { get; set; } = new();
        public List<string> NewFiles { get; set; } = new();
    }

    [STAThread]
    private static int Main(string[] args)
    {
        var options = Parse(args);
        if (!Required(options, "pid", "source", "target", "launch", "version", "current-version",
                "backup", "health", "result", "log")) return 2;

        var version = options["version"];
        var currentVersion = options["current-version"];
        var resultPath = Path.GetFullPath(options["result"]);
        var logPath = Path.GetFullPath(options["log"]);
        RecoveryJournal? journal = null;
        Process? updatedProcess = null;
        try
        {
            Log(logPath, $"Updater started. {currentVersion} -> {version}.");
            WaitForApplicationExit(options["pid"]);

            var source = Path.GetFullPath(options["source"]);
            var target = Path.GetFullPath(options["target"]);
            var launch = Path.GetFullPath(options["launch"]);
            var backup = Path.GetFullPath(options["backup"]);
            var health = Path.GetFullPath(options["health"]);
            ValidatePaths(source, target, backup, launch);

            if (File.Exists(health)) File.Delete(health);
            journal = CreateBackup(source, target, backup, currentVersion, version, logPath);
            journal.Status = "Installing";
            SaveJournal(backup, journal);
            InstallFiles(source, target, logPath);

            journal.Status = "AwaitingHealthCheck";
            SaveJournal(backup, journal);
            updatedProcess = LaunchApplication(launch, target, health);
            WaitForHealthCheck(updatedProcess, health, TimeSpan.FromSeconds(45));

            journal.Status = "Installed";
            SaveJournal(backup, journal);
            WriteResult(resultPath, version, "Installed",
                $"Update installed successfully. Recovery backup: {backup}");
            try { File.Delete(health); } catch { }
            Log(logPath, $"Update {version} installed and passed the startup health check.");
            updatedProcess.Dispose();
            return 0;
        }
        catch (Exception ex)
        {
            Log(logPath, $"Update {version} failed: {ex}");
            StopProcess(updatedProcess, logPath);

            if (journal is not null)
            {
                try
                {
                    var backup = Path.GetFullPath(options["backup"]);
                    journal.Status = "RollingBack";
                    SaveJournal(backup, journal);
                    Rollback(backup, journal, logPath);
                    journal.Status = "RolledBack";
                    SaveJournal(backup, journal);
                    WriteResult(resultPath, version, "RolledBack",
                        $"Update failed and version {currentVersion} was restored: {CleanMessage(ex.Message)}");
                    Log(logPath, $"Rollback to {currentVersion} completed successfully.");
                    TryLaunch(options["launch"], options["target"], logPath);
                    return 1;
                }
                catch (Exception rollbackError)
                {
                    Log(logPath, $"Rollback failed: {rollbackError}");
                    WriteResult(resultPath, version, "RollbackFailed",
                        $"Update and rollback failed. Backup: {options["backup"]}. Error: {CleanMessage(rollbackError.Message)}");
                    TryLaunch(options["launch"], options["target"], logPath);
                    return 3;
                }
            }

            WriteResult(resultPath, version, "Failed", CleanMessage(ex.Message));
            TryLaunch(options["launch"], options["target"], logPath);
            return 1;
        }
    }

    private static RecoveryJournal CreateBackup(string source, string target, string backup,
        string currentVersion, string version, string logPath)
    {
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
        Directory.CreateDirectory(target);
        if (Directory.Exists(backup)) Directory.Delete(backup, true);
        Directory.CreateDirectory(backup);

        var journal = new RecoveryJournal
        {
            FromVersion = currentVersion,
            ToVersion = version,
            Target = target
        };

        foreach (var sourceFile in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, sourceFile);
            var targetFile = SafeCombine(target, relative);
            if (File.Exists(targetFile))
            {
                var backupFile = SafeCombine(backup, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(backupFile)!);
                CopyWithRetry(targetFile, backupFile, true);
                journal.BackedUpFiles.Add(relative);
            }
            else
            {
                journal.NewFiles.Add(relative);
            }
        }

        journal.Status = "BackupComplete";
        SaveJournal(backup, journal);
        Log(logPath, $"Recovery backup created at {backup}. Files: {journal.BackedUpFiles.Count}; new files: {journal.NewFiles.Count}.");
        return journal;
    }

    private static void InstallFiles(string source, string target, string logPath)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destination = SafeCombine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            CopyWithRetry(file, destination, true);
        }
        Log(logPath, "All staged update files were copied to the install directory.");
    }

    private static void Rollback(string backup, RecoveryJournal journal, string logPath)
    {
        foreach (var relative in journal.NewFiles)
        {
            var path = SafeCombine(journal.Target, relative);
            if (File.Exists(path)) DeleteWithRetry(path);
        }

        foreach (var relative in journal.BackedUpFiles)
        {
            var source = SafeCombine(backup, relative);
            var destination = SafeCombine(journal.Target, relative);
            if (!File.Exists(source)) throw new FileNotFoundException("Recovery file is missing.", source);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            CopyWithRetry(source, destination, true);
        }
        Log(logPath, $"Restored {journal.BackedUpFiles.Count} files and removed {journal.NewFiles.Count} new files.");
    }

    private static Process LaunchApplication(string executable, string workingDirectory, string healthPath)
    {
        if (!File.Exists(executable)) throw new FileNotFoundException("The updated application executable is missing.", executable);
        return Process.Start(new ProcessStartInfo(executable)
        {
            UseShellExecute = true,
            WorkingDirectory = workingDirectory,
            Arguments = $"--update-health \"{healthPath}\""
        }) ?? throw new InvalidOperationException("The updated application could not be started.");
    }

    private static void WaitForHealthCheck(Process process, string healthPath, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(healthPath)) return;
            if (process.HasExited)
                throw new InvalidOperationException($"The updated application exited before startup completed (exit code {process.ExitCode}).");
            Thread.Sleep(250);
        }
        throw new TimeoutException("The updated application did not complete startup within 45 seconds.");
    }

    private static void WaitForApplicationExit(string pidText)
    {
        if (!int.TryParse(pidText, out var pid)) return;
        try
        {
            using var process = Process.GetProcessById(pid);
            if (!process.WaitForExit(30000))
                throw new TimeoutException("The running application did not close within 30 seconds.");
        }
        catch (ArgumentException) { }
    }

    private static void ValidatePaths(string source, string target, string backup, string launch)
    {
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
        if (string.Equals(source.TrimEnd('\\'), target.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Source and target directories must be different.");
        if (string.IsNullOrWhiteSpace(Path.GetFileName(target.TrimEnd(Path.DirectorySeparatorChar))))
            throw new InvalidOperationException("The install target is not safe.");
        if (string.IsNullOrWhiteSpace(Path.GetFileName(backup.TrimEnd(Path.DirectorySeparatorChar))))
            throw new InvalidOperationException("The recovery backup target is not safe.");
        if (backup.StartsWith(target.TrimEnd('\\') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The recovery backup must be outside the install directory.");
        if (!launch.StartsWith(target.TrimEnd('\\') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The launch executable must be inside the install directory.");
    }

    private static string SafeCombine(string root, string relative)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var combined = Path.GetFullPath(Path.Combine(fullRoot, relative));
        if (!combined.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Unsafe package path: {relative}");
        return combined;
    }

    private static void CopyWithRetry(string source, string destination, bool overwrite) => RetryFileOperation(() =>
        File.Copy(source, destination, overwrite));

    private static void DeleteWithRetry(string path) => RetryFileOperation(() => File.Delete(path));

    private static void RetryFileOperation(Action operation)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            try { operation(); return; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                last = ex;
                if (attempt < 4) Thread.Sleep(attempt * 500);
            }
        }
        throw last ?? new IOException("File operation failed.");
    }

    private static void StopProcess(Process? process, string logPath)
    {
        if (process is null) return;
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
                process.WaitForExit(10000);
                Log(logPath, "Stopped the failed updated application before rollback.");
            }
        }
        catch (Exception ex) { Log(logPath, $"Could not stop updated application: {ex.Message}"); }
        finally { process.Dispose(); }
    }

    private static void TryLaunch(string executable, string workingDirectory, string logPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Path.GetFullPath(executable))
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetFullPath(workingDirectory)
            });
        }
        catch (Exception ex) { Log(logPath, $"Could not restart application: {ex}"); }
    }

    private static void SaveJournal(string backup, RecoveryJournal journal)
    {
        Directory.CreateDirectory(backup);
        File.WriteAllText(Path.Combine(backup, "recovery.json"),
            JsonSerializer.Serialize(journal, new JsonSerializerOptions { WriteIndented = true }));
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
            File.WriteAllText(path, $"{version}|{status}|{CleanMessage(message)}");
        }
        catch { }
    }

    private static string CleanMessage(string message) => message
        .Replace('|', '/')
        .Replace('\r', ' ')
        .Replace('\n', ' ');

    private static void Log(string path, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch { }
    }
}
