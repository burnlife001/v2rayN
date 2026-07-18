using ServiceLib.Models.Dto.CliCommand;
using ServiceLib.Services;

namespace ServiceLib.Handler.CliCommands;

public class SubListCommand : ICliCommand
{
    public string Name => "sub.list";

    public async Task<CliResponse> ExecuteAsync(CliRequest request, ICliCommandBridge bridge)
    {
        var subs = await bridge.GetSubscriptionsAsync();
        return CliResponse.Ok(request.Id, subs);
    }
}
