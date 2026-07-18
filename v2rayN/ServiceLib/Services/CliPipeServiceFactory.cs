using ServiceLib.Handler;
using ServiceLib.Handler.CliCommands;

namespace ServiceLib.Services;

public static class CliPipeServiceFactory
{
    public static CliPipeService Create(string exePath)
    {
        var bridge = CliCommandBridge.Instance;
        var dispatcher = new CliCommandDispatcher(
        [
            new NodeListCommand(),
            new NodeSwitchCommand(),
            new SubListCommand(),
            new SubUpdateCommand(),
            new CoreRestartCommand(),
            new AppUpdateCommand(),
        ]);

        var pipeName = CliPipeService.GetPipeName(exePath);
        return new CliPipeService(dispatcher, bridge, pipeName);
    }
}
