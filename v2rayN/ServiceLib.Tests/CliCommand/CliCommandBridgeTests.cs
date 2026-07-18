using AwesomeAssertions;
using ServiceLib.Services;
using Xunit;

namespace ServiceLib.Tests.CliCommand;

public class CliCommandBridgeTests
{
    [Fact]
    public async Task SwitchNodeAsync_EmptyName_ThrowsArgumentException()
    {
        var bridge = new CliCommandBridge();
        var act = () => bridge.SwitchNodeAsync(string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_WithoutMainWindow_ReturnsFalse()
    {
        var bridge = new CliCommandBridge();
        var result = await bridge.UpdateSubscriptionAsync("id", false);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RestartCoreAsync_WithoutMainWindow_ReturnsFalse()
    {
        var bridge = new CliCommandBridge();
        var result = await bridge.RestartCoreAsync();
        result.Should().BeFalse();
    }

    [Fact]
    public void Instance_IsSingleton()
    {
        var a = CliCommandBridge.Instance;
        var b = CliCommandBridge.Instance;
        a.Should().BeSameAs(b);
    }
}
