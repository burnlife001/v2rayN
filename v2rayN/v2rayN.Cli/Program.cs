using ServiceLib.Services;
using v2rayN.Cli.Commands;

namespace v2rayN.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintHelp();
            return ExitCodes.Success;
        }

        try
        {
            return await RunAsync(args);
        }
        catch (FileNotFoundException ex)
        {
            OutputFormatter.PrintError(ex.Message);
            return ExitCodes.ConnectionError;
        }
        catch (Exception ex)
        {
            OutputFormatter.PrintError(ex.Message);
            return ExitCodes.BusinessError;
        }
    }

    private static async Task<int> RunAsync(string[] args)
    {
        var command = args[0].ToLowerInvariant();
        var subArgs = args.Skip(1).ToArray();
        var json = subArgs.Contains("--json");
        subArgs = subArgs.Where(a => a != "--json").ToArray();

        var exePath = ProcessLauncher.GetV2RayNExePath();
        var pipeName = CliPipeService.GetPipeName(exePath);

        if (!await CliClient.IsPipeAvailableAsync(pipeName, 500))
        {
            if (command == "launch")
            {
                if (ProcessLauncher.IsV2RayNRunning())
                {
                    Console.WriteLine("v2rayN is already running.");
                    return ExitCodes.Success;
                }

                var launched = await ProcessLauncher.LaunchAsync(exePath);
                return launched ? ExitCodes.Success : ExitCodes.ConnectionError;
            }

            Console.WriteLine("v2rayN is not running. Starting...");
            var started = await ProcessLauncher.LaunchAsync(exePath);
            if (!started)
            {
                OutputFormatter.PrintError("Failed to start v2rayN or connect to pipe.");
                return ExitCodes.ConnectionError;
            }
        }

        using var client = new CliClient(pipeName);

        return command switch
        {
            "node" => await HandleNodeAsync(client, subArgs, json),
            "sub" => await HandleSubAsync(client, subArgs, json),
            "core" => await HandleCoreAsync(client, subArgs, json),
            "app" => await HandleAppAsync(client, subArgs, json),
            "launch" => await HandleLaunchAsync(),
            _ => UnknownCommand(command),
        };
    }

    private static async Task<int> HandleLaunchAsync()
    {
        if (ProcessLauncher.IsV2RayNRunning())
        {
            Console.WriteLine("v2rayN is running.");
            return ExitCodes.Success;
        }
        var exePath = ProcessLauncher.GetV2RayNExePath();
        var launched = await ProcessLauncher.LaunchAsync(exePath);
        return launched ? ExitCodes.Success : ExitCodes.ConnectionError;
    }

    private static int UnknownCommand(string command)
    {
        OutputFormatter.PrintError($"Unknown command: {command}");
        return ExitCodes.UsageError;
    }

    private static async Task<int> HandleNodeAsync(CliClient client, string[] args, bool json)
    {
        if (args.Length == 0 || args[0] == "list")
        {
            var subid = GetNamedArg(args, "--sub");
            return await NodeCliCommands.ListAsync(client, subid, json);
        }

        if (args[0] == "switch" && args.Length > 1)
        {
            return await NodeCliCommands.SwitchAsync(client, args[1], json);
        }

        OutputFormatter.PrintError("Usage: v2rayn node list [--sub <id>] | switch <name>");
        return ExitCodes.UsageError;
    }

    private static async Task<int> HandleSubAsync(CliClient client, string[] args, bool json)
    {
        if (args.Length == 0 || args[0] == "list")
        {
            return await SubCliCommands.ListAsync(client, json);
        }

        if (args[0] == "update")
        {
            var id = GetNamedArg(args, "--id");
            var viaProxy = args.Contains("--via-proxy");
            return await SubCliCommands.UpdateAsync(client, id, viaProxy, json);
        }

        OutputFormatter.PrintError("Usage: v2rayn sub list | update [--id <id>] [--via-proxy]");
        return ExitCodes.UsageError;
    }

    private static async Task<int> HandleCoreAsync(CliClient client, string[] args, bool json)
    {
        if (args.Length > 0 && args[0] == "restart")
        {
            return await CoreCliCommands.RestartAsync(client, json);
        }

        OutputFormatter.PrintError("Usage: v2rayn core restart");
        return ExitCodes.UsageError;
    }

    private static async Task<int> HandleAppAsync(CliClient client, string[] args, bool json)
    {
        if (args.Length > 0 && args[0] == "update")
        {
            var preRelease = args.Contains("--pre-release");
            return await AppCliCommands.UpdateAsync(client, preRelease, json);
        }

        OutputFormatter.PrintError("Usage: v2rayn app update [--pre-release]");
        return ExitCodes.UsageError;
    }

    private static string? GetNamedArg(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        if (idx >= 0 && idx < args.Length - 1)
        {
            return args[idx + 1];
        }
        return null;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"v2rayn CLI

Usage:
  v2rayn node list [--sub <id>] [--json]
  v2rayn node switch <name> [--json]
  v2rayn sub list [--json]
  v2rayn sub update [--id <id>] [--via-proxy] [--json]
  v2rayn core restart [--json]
  v2rayn app update [--pre-release] [--json]
  v2rayn launch
");
    }
}
