namespace ServiceLib.Models.Dto.CliCommand;

public class CliResponse
{
    public bool Success { get; set; }

    public object? Data { get; set; }

    public string? Error { get; set; }

    public string Id { get; set; } = string.Empty;

    public static CliResponse Ok(string id, object? data = null) =>
        new() { Success = true, Id = id, Data = data };

    public static CliResponse Fail(string id, string error) =>
        new() { Success = false, Id = id, Error = error };
}
