using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using ServiceLib.Models.Dto.CliCommand;

namespace v2rayN.Cli;

public class CliClient : IDisposable
{
    private static readonly byte[] s_newline = "\n"u8.ToArray();

    private readonly string _pipeName;

    public CliClient(string pipeName)
    {
        _pipeName = pipeName;
    }

    public async Task<CliResponse?> SendAsync(CliRequest request, int timeoutMs = 30000, CancellationToken ct = default)
    {
        await using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(timeoutMs, ct);

        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
        await pipe.WriteAsync(payload, ct);
        await pipe.WriteAsync(s_newline, ct);
        await pipe.FlushAsync(ct);

        var line = await ReadLineAsync(pipe, ct);
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        return JsonSerializer.Deserialize<CliResponse>(line);
    }

    public static async Task<bool> IsPipeAvailableAsync(string pipeName, int timeoutMs = 500, CancellationToken ct = default)
    {
        try
        {
            await using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(timeoutMs, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private static async Task<string?> ReadLineAsync(PipeStream pipe, CancellationToken ct)
    {
        var buffer = new byte[256];
        var accumulated = new List<byte>(256);
        while (true)
        {
            var n = await pipe.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
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
