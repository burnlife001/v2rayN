using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Models.Dto.CliCommand;

namespace ServiceLib.Services;

public class CliPipeService
{
    private static readonly byte[] s_newline = "\n"u8.ToArray();

    private readonly CliCommandDispatcher _dispatcher;
    private readonly ICliCommandBridge _bridge;
    private readonly string _pipeName;
    private readonly int _maxInstances;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;

    public CliPipeService(CliCommandDispatcher dispatcher, ICliCommandBridge bridge, string pipeName, int maxInstances = 10)
    {
        _dispatcher = dispatcher;
        _bridge = bridge;
        _pipeName = pipeName;
        _maxInstances = maxInstances;
    }

    public string PipeName => _pipeName;

    public static string GetPipeName(string exePath)
    {
        var hash = Utils.GetMd5(exePath);
        return $"v2rayN-cli-v1-{hash}";
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_listenerTask != null)
        {
            try
            {
                await _listenerTask.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            catch
            {
                // ignore shutdown errors
            }
        }
        _cts?.Dispose();
        _cts = null;
        _listenerTask = null;
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    _maxInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(ct);
                await HandleClientAsync(pipe);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logging.SaveLog("CliPipeService", ex);
                try
                {
                    await Task.Delay(100, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe)
    {
        try
        {
            var line = await ReadLineAsync(pipe);
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            CliRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<CliRequest>(line);
            }
            catch (Exception ex)
            {
                await WriteResponseAsync(pipe, CliResponse.Fail(Guid.NewGuid().ToString(), $"Invalid JSON: {ex.Message}"));
                return;
            }

            request ??= new CliRequest { Id = Guid.NewGuid().ToString() };
            var response = await _dispatcher.DispatchAsync(request, _bridge);
            await WriteResponseAsync(pipe, response);
        }
        catch (Exception ex)
        {
            Logging.SaveLog("CliPipeService", ex);
        }
    }

    // Reads a single newline-terminated UTF-8 line from the pipe. Returns null on EOF.
    private static async Task<string?> ReadLineAsync(PipeStream pipe, int maxBytes = 1024 * 1024)
    {
        var buffer = new byte[256];
        var accumulated = new List<byte>(256);
        while (accumulated.Count < maxBytes)
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

    private static async Task WriteResponseAsync(PipeStream pipe, CliResponse response)
    {
        var json = JsonSerializer.Serialize(response);
        var bytes = Encoding.UTF8.GetBytes(json);
        await pipe.WriteAsync(bytes.AsMemory(0, bytes.Length));
        await pipe.WriteAsync(s_newline.AsMemory(0, s_newline.Length));
        await pipe.FlushAsync();
    }
}
