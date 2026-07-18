using ServiceLib.Handler.CliCommands;
using ServiceLib.Models.Dto.CliCommand;
using ServiceLib.Services;

namespace ServiceLib.Handler;

public class CliCommandDispatcher
{
    private readonly IReadOnlyDictionary<string, ICliCommand> _commands;

    public CliCommandDispatcher(IEnumerable<ICliCommand> commands)
    {
        _commands = commands.ToDictionary(c => c.Name, c => c);
    }

    public async Task<CliResponse> DispatchAsync(CliRequest request, ICliCommandBridge bridge)
    {
        if (string.IsNullOrWhiteSpace(request.Cmd))
        {
            return CliResponse.Fail(request.Id, "Missing command.");
        }

        if (!_commands.TryGetValue(request.Cmd, out var command))
        {
            return CliResponse.Fail(request.Id, $"Unknown command: {request.Cmd}");
        }

        try
        {
            return await command.ExecuteAsync(request, bridge);
        }
        catch (Exception ex)
        {
            return CliResponse.Fail(request.Id, $"Command failed: {ex.Message}");
        }
    }
}
