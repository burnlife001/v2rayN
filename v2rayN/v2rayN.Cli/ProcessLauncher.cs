using System.Diagnostics;
using ServiceLib.Common;

namespace v2rayN.Cli;

public static class ProcessLauncher
{
    public static string GetV2RayNExePath()
    {
        var currentDir = AppContext.BaseDirectory;
        var exeName = Utils.IsWindows() ? "v2rayN.exe" : "v2rayN";
        var path = Path.Combine(currentDir, exeName);

        if (File.Exists(path))
        {
            return path;
        }

        // CLI may be one folder deep (e.g. publish layout)
        path = Path.Combine(currentDir, "..", exeName);
        if (File.Exists(path))
        {
            return Path.GetFullPath(path);
        }

        // macOS .app bundle
        if (Utils.IsMacOS())
        {
            var appBundle = Path.Combine(currentDir, "..", "v2rayN.app", "Contents", "MacOS", "v2rayN");
            if (File.Exists(appBundle))
            {
                return Path.GetFullPath(appBundle);
            }
        }

        throw new FileNotFoundException($"Could not find v2rayN executable in or above {currentDir}");
    }

    public static bool IsV2RayNRunning()
    {
        var running = Process.GetProcessesByName("v2rayN");
        try
        {
            return running.Length > 0;
        }
        finally
        {
            foreach (var p in running)
            {
                p.Dispose();
            }
        }
    }

    public static async Task<bool> LaunchAsync(string exePath, int readyTimeoutMs = 30000, CancellationToken ct = default)
    {
        var startInfo = new ProcessStartInfo(exePath)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exePath),
        };

        Process.Start(startInfo);

        var pipeName = ServiceLib.Services.CliPipeService.GetPipeName(exePath);
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < readyTimeoutMs)
        {
            if (await CliClient.IsPipeAvailableAsync(pipeName, 500, ct))
            {
                return true;
            }
            await Task.Delay(200, ct);
        }

        return false;
    }
}
