using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using ServiceLib.Handler;
using ServiceLib.Handler.CliCommands;
using ServiceLib.Models.Dto.CliCommand;
using ServiceLib.Services;
using Xunit;

namespace ServiceLib.Tests.CliCommand;

public class CliPipeServiceTests : IDisposable
{
    private static readonly byte[] s_newline = "\n"u8.ToArray();

    private readonly CliPipeService _service;
    private readonly string _pipeName = $"v2rayN-cli-test-{Guid.NewGuid():N}";

    private class EchoCommand : ICliCommand
    {
        public string Name => "echo";

        public Task<CliResponse> ExecuteAsync(CliRequest request, ICliCommandBridge bridge) =>
            Task.FromResult(CliResponse.Ok(request.Id, request.Args.GetValueOrDefault("msg")));
    }

    public CliPipeServiceTests()
    {
        var dispatcher = new CliCommandDispatcher([new EchoCommand()]);
        _service = new CliPipeService(dispatcher, null!, _pipeName);
        _service.Start();
    }

    public void Dispose()
    {
        _service.StopAsync().Wait();
    }

    [Fact]
    public async Task SendCommand_ReceivesResponse()
    {
        await using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000);

        var request = new CliRequest
        {
            Cmd = "echo",
            Args = new Dictionary<string, object?> { ["msg"] = "hello" },
            Id = "1",
        };
        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
        await client.WriteAsync(payload);
        await client.WriteAsync(s_newline);
        await client.FlushAsync();

        var responseLine = await ReadLineAsync(client);
        responseLine.Should().NotBeNullOrWhiteSpace();
        var response = JsonSerializer.Deserialize<CliResponse>(responseLine);
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.ToString().Should().Be("hello");
        response.Id.Should().Be("1");
    }

    [Fact]
    public async Task SendInvalidJson_ReturnsFailWithError()
    {
        await using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000);

        var payload = Encoding.UTF8.GetBytes("{not-json");
        await client.WriteAsync(payload);
        await client.WriteAsync(s_newline);
        await client.FlushAsync();

        var responseLine = await ReadLineAsync(client);
        responseLine.Should().NotBeNullOrWhiteSpace();
        var response = JsonSerializer.Deserialize<CliResponse>(responseLine);
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Error.Should().Contain("Invalid JSON");
    }

    private static async Task<string?> ReadLineAsync(PipeStream pipe)
    {
        var buffer = new byte[256];
        var accumulated = new List<byte>(256);
        while (true)
        {
            var n = await pipe.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (n == 0)
            {
                break;
            }
            for (var i = 0; i < n; i++)
            {
                if (buffer[i] == (byte)'\n')
                {
                    accumulated.AddRange(buffer.Take(i));
                    return Encoding.UTF8.GetString(accumulated.ToArray());
                }
                accumulated.Add(buffer[i]);
            }
        }
        return accumulated.Count == 0 ? null : Encoding.UTF8.GetString(accumulated.ToArray());
    }
}
