using ServiceLib.Models.Dto.CliCommand;

namespace v2rayN.Cli.Commands;

public static class SubCliCommands
{
    public static async Task<int> ListAsync(CliClient client, bool json)
    {
        var request = new CliRequest { Cmd = "sub.list" };
        var response = await client.SendAsync(request);
        return NodeCliCommands.HandleResponse(response, json);
    }

    public static async Task<int> UpdateAsync(CliClient client, string? id, bool viaProxy, bool json)
    {
        var request = new CliRequest
        {
            Cmd = "sub.update",
            Args = new Dictionary<string, object?>
            {
                ["id"] = id,
                ["viaProxy"] = viaProxy,
            },
        };
        var response = await client.SendAsync(request);
        return NodeCliCommands.HandleResponse(response, json, "Subscription update requested.");
    }
}
