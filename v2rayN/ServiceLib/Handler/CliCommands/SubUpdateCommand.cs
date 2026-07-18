using ServiceLib.Models.Dto.CliCommand;
using ServiceLib.Services;

namespace ServiceLib.Handler.CliCommands;

public class SubUpdateCommand : ICliCommand
{
    public string Name => "sub.update";

    public async Task<CliResponse> ExecuteAsync(CliRequest request, ICliCommandBridge bridge)
    {
        var subId = request.Args.GetValueOrDefault("id")?.ToString();
        var viaProxy = bool.TryParse(request.Args.GetValueOrDefault("viaProxy")?.ToString(), out var vp) && vp;

        var ok = await bridge.UpdateSubscriptionAsync(subId, viaProxy);
        return ok
            ? CliResponse.Ok(request.Id, new Dictionary<string, object?> { ["updated"] = subId ?? "all" })
            : CliResponse.Fail(request.Id, "Failed to update subscription");
    }
}
