using ServiceLib.Models.Dto.CliCommand;
using ServiceLib.Services;

namespace ServiceLib.Handler.CliCommands;

public class NodeSwitchCommand : ICliCommand
{
    public string Name => "node.switch";

    public async Task<CliResponse> ExecuteAsync(CliRequest request, ICliCommandBridge bridge)
    {
        var name = request.Args.GetValueOrDefault("name")?.ToString();
        if (string.IsNullOrWhiteSpace(name))
        {
            return CliResponse.Fail(request.Id, "Missing argument: name");
        }

        var node = await bridge.SwitchNodeAsync(name);
        if (node == null)
        {
            return CliResponse.Fail(request.Id, $"Node not found: {name}");
        }

        return CliResponse.Ok(request.Id, node);
    }
}
