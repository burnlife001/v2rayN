using ServiceLib.Models.Dto.CliCommand;

namespace v2rayN.Cli.Commands;

public static class CoreCliCommands
{
    public static async Task<int> RestartAsync(CliClient client, bool json)
    {
        var request = new CliRequest { Cmd = "core.restart" };
        var response = await client.SendAsync(request);
        return NodeCliCommands.HandleResponse(response, json, "Core restart requested.");
    }
}
