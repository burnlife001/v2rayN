using ServiceLib.Events;
using ServiceLib.Handler;
using ServiceLib.Manager;
using ServiceLib.Models.Dto.CliCommand;
using ServiceLib.ViewModels;

namespace ServiceLib.Services;

public class CliCommandBridge : ICliCommandBridge
{
    private static CliCommandBridge? _instance;
    public static CliCommandBridge Instance => _instance ??= new CliCommandBridge();

    public MainWindowViewModel? MainWindowViewModel { get; set; }
    public ProfilesViewModel? ProfilesViewModel { get; set; }

    public async Task<List<NodeListItem>> GetNodesAsync(string? subid = null)
    {
        var items = await AppManager.Instance.ProfileItems(subid ?? string.Empty) ?? [];
        var subs = await AppManager.Instance.SubItems() ?? [];
        var subMap = subs.ToDictionary(s => s.Id, s => s.Remarks);

        return items
            .Where(p => p.ConfigType != Enums.EConfigType.Custom)
            .Select(p => new NodeListItem(
                p.IndexId,
                p.Remarks,
                p.Address,
                p.Port,
                p.ConfigType.ToString(),
                string.Empty,
                p.Subid,
                subMap.TryGetValue(p.Subid, out var remark) ? remark : string.Empty))
            .ToList();
    }

    public async Task<NodeListItem?> SwitchNodeAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Node name cannot be empty.", nameof(name));
        }

        var items = await AppManager.Instance.ProfileItems(string.Empty) ?? [];
        var match = items.FirstOrDefault(p =>
            p.Remarks.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            throw new InvalidOperationException($"Node not found: {name}");
        }

        if (ProfilesViewModel == null)
        {
            throw new InvalidOperationException("ProfilesViewModel is not available.");
        }

        await ProfilesViewModel.SetDefaultServerAsync(match.IndexId);

        return new NodeListItem(
            match.IndexId,
            match.Remarks,
            match.Address,
            match.Port,
            match.ConfigType.ToString(),
            string.Empty,
            match.Subid);
    }

    public async Task<List<SubListItem>> GetSubscriptionsAsync()
    {
        var subs = await AppManager.Instance.SubItems() ?? [];
        return subs.Select(s => new SubListItem(
            s.Id,
            s.Remarks,
            s.Url,
            s.Enabled,
            s.UpdateTime)).ToList();
    }

    public async Task<bool> UpdateSubscriptionAsync(string? subId, bool viaProxy)
    {
        if (MainWindowViewModel == null)
        {
            return false;
        }

        await MainWindowViewModel.UpdateSubscriptionProcess(subId ?? string.Empty, viaProxy);
        return true;
    }

    public async Task<bool> RestartCoreAsync()
    {
        if (MainWindowViewModel == null)
        {
            return false;
        }

        await MainWindowViewModel.Reload();
        return true;
    }

    public async Task<bool> UpdateAppAsync(bool preRelease)
    {
        var config = AppManager.Instance.Config;
        // CLI has no UI message channel: use a no-op updateFunc. Progress is logged
        // by UpdateService; results surface via the returned UpdateResult.
        var service = new UpdateService(config, (_, _) => Task.CompletedTask);
        try
        {
            await service.CheckUpdateGuiN(preRelease);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
