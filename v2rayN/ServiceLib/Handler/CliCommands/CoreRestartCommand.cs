using ServiceLib.Models.Dto.CliCommand;
using ServiceLib.Services;

namespace ServiceLib.Handler.CliCommands;

public class CoreRestartCommand : ICliCommand
{
    public string Name => "core.restart";

    public async Task<CliResponse> ExecuteAsync(CliRequest request, ICliCommandBridge bridge)
    {
        var ok = await bridge.RestartCoreAsync();
        return ok
            ? CliResponse.Ok(request.Id, new Dictionary<string, object?> { ["restarted"] = true })
            : CliResponse.Fail(request.Id, "Failed to restart core");
    }
}
