using AwesomeAssertions;
using ServiceLib.Handler.CliCommands;
using ServiceLib.Models.Dto.CliCommand;
using ServiceLib.Services;
using Xunit;

namespace ServiceLib.Tests.CliCommand;

public class CliCommandHandlerTests
{
    private class FakeBridge : ICliCommandBridge
    {
        public Task<List<NodeListItem>> GetNodesAsync(string? subid = null) =>
            Task.FromResult(new List<NodeListItem> { new("id1", "HK-01", "1.1.1.1", 443, "VMess", "120ms") });

        public Task<NodeListItem?> SwitchNodeAsync(string name) =>
            Task.FromResult<NodeListItem?>(new("id1", name, "1.1.1.1", 443, "VMess", "120ms"));

        public Task<List<SubListItem>> GetSubscriptionsAsync() => Task.FromResult(new List<SubListItem>());
        public Task<bool> UpdateSubscriptionAsync(string? subId, bool viaProxy) => Task.FromResult(true);
        public Task<bool> RestartCoreAsync() => Task.FromResult(true);
        public Task<bool> UpdateAppAsync(bool preRelease) => Task.FromResult(true);
    }

    private class FailingBridge : ICliCommandBridge
    {
        public Task<List<NodeListItem>> GetNodesAsync(string? subid = null) => Task.FromResult(new List<NodeListItem>());
        public Task<NodeListItem?> SwitchNodeAsync(string name) => Task.FromResult<NodeListItem?>(null);
        public Task<List<SubListItem>> GetSubscriptionsAsync() => Task.FromResult(new List<SubListItem>());
        public Task<bool> UpdateSubscriptionAsync(string? subId, bool viaProxy) => Task.FromResult(false);
        public Task<bool> RestartCoreAsync() => Task.FromResult(false);
        public Task<bool> UpdateAppAsync(bool preRelease) => Task.FromResult(false);
    }

    [Fact]
    public async Task NodeListCommand_ReturnsNodes()
    {
        var handler = new NodeListCommand();
        var request = new CliRequest { Cmd = "node.list", Id = "1" };

        var response = await handler.ExecuteAsync(request, new FakeBridge());

        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task NodeSwitchCommand_MissingName_ReturnsFail()
    {
        var handler = new NodeSwitchCommand();
        var request = new CliRequest { Cmd = "node.switch", Id = "2" };

        var response = await handler.ExecuteAsync(request, new FakeBridge());

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("name");
    }

    [Fact]
    public async Task NodeSwitchCommand_NoMatch_ReturnsFail()
    {
        var handler = new NodeSwitchCommand();
        var request = new CliRequest
        {
            Cmd = "node.switch",
            Args = new() { ["name"] = "Nope" },
            Id = "3",
        };

        var response = await handler.ExecuteAsync(request, new FailingBridge());

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("Nope");
    }

    [Fact]
    public async Task NodeSwitchCommand_Match_ReturnsOk()
    {
        var handler = new NodeSwitchCommand();
        var request = new CliRequest
        {
            Cmd = "node.switch",
            Args = new() { ["name"] = "HK" },
            Id = "4",
        };

        var response = await handler.ExecuteAsync(request, new FakeBridge());

        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SubListCommand_ReturnsSubs()
    {
        var handler = new SubListCommand();
        var request = new CliRequest { Cmd = "sub.list", Id = "5" };

        var response = await handler.ExecuteAsync(request, new FakeBridge());

        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SubUpdateCommand_NoId_UpdatesAll()
    {
        var handler = new SubUpdateCommand();
        var request = new CliRequest { Cmd = "sub.update", Id = "6" };

        var response = await handler.ExecuteAsync(request, new FakeBridge());

        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SubUpdateCommand_BridgeFails_ReturnsFail()
    {
        var handler = new SubUpdateCommand();
        var request = new CliRequest { Cmd = "sub.update", Id = "7" };

        var response = await handler.ExecuteAsync(request, new FailingBridge());

        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CoreRestartCommand_Ok()
    {
        var handler = new CoreRestartCommand();
        var request = new CliRequest { Cmd = "core.restart", Id = "8" };

        var response = await handler.ExecuteAsync(request, new FakeBridge());

        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AppUpdateCommand_Ok()
    {
        var handler = new AppUpdateCommand();
        var request = new CliRequest { Cmd = "app.update", Id = "9" };

        var response = await handler.ExecuteAsync(request, new FakeBridge());

        response.Success.Should().BeTrue();
    }
}
