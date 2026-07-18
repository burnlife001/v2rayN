namespace ServiceLib.Models.Dto.CliCommand;

public class CliRequest
{
    public string Cmd { get; set; } = string.Empty;

    public Dictionary<string, object?> Args { get; set; } = [];

    public string Version { get; set; } = string.Empty;

    public string Id { get; set; } = Guid.NewGuid().ToString();
}
