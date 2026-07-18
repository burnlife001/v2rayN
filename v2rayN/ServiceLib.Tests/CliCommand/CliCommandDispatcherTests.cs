using AwesomeAssertions;
using ServiceLib.Handler;
using ServiceLib.Handler.CliCommands;
using ServiceLib.Models.Dto.CliCommand;
using ServiceLib.Services;
using Xunit;

namespace ServiceLib.Tests.CliCommand;

public class CliCommandDispatcherTests
{
    private class EchoCommand : ICliCommand
    {
        public string Name => "test.echo";

        public Task<CliResponse> ExecuteAsync(CliRequest request, ICliCommandBridge bridge) =>
            Task.FromResult(CliResponse.Ok(request.Id, request.Args.GetValueOrDefault("msg")));
    }

    [Fact]
    public async Task DispatchAsync_KnownCommand_ReturnsOk()
    {
        var dispatcher = new CliCommandDispatcher([new EchoCommand()]);
        var request = new CliRequest
        {
            Cmd = "test.echo",
            Args = new Dictionary<string, object?> { ["msg"] = "hi" },
            Id = "1",
        };

        var response = await dispatcher.DispatchAsync(request, null!);

        response.Success.Should().BeTrue();
        response.Id.Should().Be("1");
        response.Data.Should().Be("hi");
    }

    [Fact]
    public async Task DispatchAsync_UnknownCommand_ReturnsFail()
    {
        var dispatcher = new CliCommandDispatcher([new EchoCommand()]);
        var request = new CliRequest { Cmd = "unknown", Id = "2" };

        var response = await dispatcher.DispatchAsync(request, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("Unknown command");
    }

    [Fact]
    public async Task DispatchAsync_MissingCommand_ReturnsFail()
    {
        var dispatcher = new CliCommandDispatcher([new EchoCommand()]);
        var request = new CliRequest { Id = "3" };

        var response = await dispatcher.DispatchAsync(request, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("Missing command");
    }

    [Fact]
    public async Task DispatchAsync_HandlerThrows_ReturnsFailWithExceptionMessage()
    {
        var dispatcher = new CliCommandDispatcher([new ThrowingCommand()]);
        var request = new CliRequest { Cmd = "throw", Id = "4" };

        var response = await dispatcher.DispatchAsync(request, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("boom");
    }

    private class ThrowingCommand : ICliCommand
    {
        public string Name => "throw";

        public Task<CliResponse> ExecuteAsync(CliRequest request, ICliCommandBridge bridge) =>
            throw new InvalidOperationException("boom");
    }
}
