using ServiceLib.Models.Dto.CliCommand;

namespace v2rayN.Cli.Commands;

public static class AppCliCommands
{
    public static async Task<int> UpdateAsync(CliClient client, bool preRelease, bool json)
    {
        var request = new CliRequest
        {
            Cmd = "app.update",
            Args = new Dictionary<string, object?> { ["preRelease"] = preRelease },
        };
        var response = await client.SendAsync(request);
        return NodeCliCommands.HandleResponse(response, json, "App update check requested.");
    }
}
