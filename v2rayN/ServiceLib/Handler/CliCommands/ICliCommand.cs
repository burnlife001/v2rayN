using ServiceLib.Models.Dto.CliCommand;

namespace ServiceLib.Handler.CliCommands;

public interface ICliCommand
{
    string Name { get; }

    Task<CliResponse> ExecuteAsync(CliRequest request, ICliCommandBridge bridge);
}
