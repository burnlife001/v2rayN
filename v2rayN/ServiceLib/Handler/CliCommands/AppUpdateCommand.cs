using ServiceLib.Models.Dto.CliCommand;
using ServiceLib.Services;

namespace ServiceLib.Handler.CliCommands;

public class AppUpdateCommand : ICliCommand
{
    public string Name => "app.update";

    public async Task<CliResponse> ExecuteAsync(CliRequest request, ICliCommandBridge bridge)
    {
        var preRelease = bool.TryParse(request.Args.GetValueOrDefault("preRelease")?.ToString(), out var pr) && pr;
        var ok = await bridge.UpdateAppAsync(preRelease);
        return ok
            ? CliResponse.Ok(request.Id, new Dictionary<string, object?> { ["checked"] = true })
            : CliResponse.Fail(request.Id, "Failed to check for update");
    }
}
