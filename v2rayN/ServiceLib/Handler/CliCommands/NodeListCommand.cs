using ServiceLib.Models.Dto.CliCommand;
using ServiceLib.Services;

namespace ServiceLib.Handler.CliCommands;

public class NodeListCommand : ICliCommand
{
    public string Name => "node.list";

    public async Task<CliResponse> ExecuteAsync(CliRequest request, ICliCommandBridge bridge)
    {
        var subid = request.Args.TryGetValue("subid", out var v) ? v?.ToString() : null;
        var nodes = await bridge.GetNodesAsync(subid);
        return CliResponse.Ok(request.Id, nodes);
    }
}
