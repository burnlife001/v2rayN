using ServiceLib.Models.Dto.CliCommand;

namespace ServiceLib.Services;

public interface ICliCommandBridge
{
    Task<List<NodeListItem>> GetNodesAsync(string? subid = null);

    Task<NodeListItem?> SwitchNodeAsync(string name);

    Task<List<SubListItem>> GetSubscriptionsAsync();

    Task<bool> UpdateSubscriptionAsync(string? subId, bool viaProxy);

    Task<bool> RestartCoreAsync();

    Task<bool> UpdateAppAsync(bool preRelease);
}
