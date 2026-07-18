using System.Text.Json;
using ServiceLib.Models.Dto.CliCommand;

namespace v2rayN.Cli.Commands;

public static class NodeCliCommands
{
    public static async Task<int> ListAsync(CliClient client, string? subid, bool json)
    {
        var request = new CliRequest { Cmd = "node.list" };
        if (subid != null)
        {
            request.Args["subid"] = subid;
        }
        var response = await client.SendAsync(request);

        if (response == null)
        {
            OutputFormatter.PrintError("No response from v2rayN.");
            return ExitCodes.ConnectionError;
        }

        if (!response.Success)
        {
            OutputFormatter.PrintError(response.Error ?? "Unknown error");
            return ExitCodes.BusinessError;
        }

        // JSON or filtered by sub: use standard output
        if (json || subid != null)
        {
            OutputFormatter.Print(response.Data, json);
            return ExitCodes.Success;
        }

        // Plain text, all nodes: group by subscription
        var nodes = MaterializeNodes(response.Data);
        if (nodes != null)
        {
            PrintNodesGrouped(nodes);
        }
        else
        {
            OutputFormatter.Print(response.Data, false);
        }

        return ExitCodes.Success;
    }

    public static async Task<int> SwitchAsync(CliClient client, string name, bool json)
    {
        var request = new CliRequest
        {
            Cmd = "node.switch",
            Args = new Dictionary<string, object?> { ["name"] = name },
        };
        var response = await client.SendAsync(request);
        return HandleResponse(response, json, $"Switched to {name}");
    }

    internal static int HandleResponse(CliResponse? response, bool json, string? successMessage = null)
    {
        if (response == null)
        {
            OutputFormatter.PrintError("No response from v2rayN.");
            return ExitCodes.ConnectionError;
        }

        if (!response.Success)
        {
            OutputFormatter.PrintError(response.Error ?? "Unknown error");
            return ExitCodes.BusinessError;
        }

        if (!json && successMessage != null)
        {
            Console.WriteLine(successMessage);
        }

        OutputFormatter.Print(response.Data, json);
        return ExitCodes.Success;
    }

    private static void PrintNodesGrouped(List<NodeListItem> nodes)
    {
        var groups = nodes.GroupBy(n => string.IsNullOrEmpty(n.GroupName) ? "(Uncategorized)" : n.GroupName);

        foreach (var group in groups)
        {
            Console.WriteLine();
            Console.WriteLine($"[{group.Key}]");
            foreach (var node in group)
            {
                var delay = string.IsNullOrEmpty(node.Delay) ? "" : $"  {node.Delay}";
                Console.WriteLine($"  {node.Name,-24} {node.Address}:{node.Port,-6} {node.Type}{delay}");
            }
        }

        if (nodes.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Total: {nodes.Count} node(s)");
        }
    }

    private static List<NodeListItem>? MaterializeNodes(object? data)
    {
        if (data is List<NodeListItem> list)
        {
            return list;
        }

        if (data is JsonElement { ValueKind: JsonValueKind.Array } element)
        {
            return element.Deserialize<List<NodeListItem>>();
        }

        return null;
    }
}
