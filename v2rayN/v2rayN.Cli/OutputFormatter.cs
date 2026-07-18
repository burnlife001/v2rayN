using System.Text.Json;

namespace v2rayN.Cli;

public static class OutputFormatter
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public static void Print(object? data, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(data, s_jsonOptions));
            return;
        }

        if (data is JsonElement je)
        {
            PrintJsonElement(je);
            return;
        }

        if (data is System.Collections.IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                if (item == null)
                {
                    continue;
                }
                Console.WriteLine(item.ToString());
            }
            return;
        }

        Console.WriteLine(data?.ToString() ?? "(null)");
    }

    private static void PrintJsonElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                PrintJsonElement(item);
            }
            return;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            // prefer "Name" (nodes), fall back to "Remarks" (subs), otherwise raw
            if (element.TryGetProperty("Name", out var name))
            {
                Console.WriteLine(name.GetString());
            }
            else if (element.TryGetProperty("Remarks", out var remarks))
            {
                Console.WriteLine(remarks.GetString());
            }
            else
            {
                Console.WriteLine(element.ToString());
            }
            return;
        }

        Console.WriteLine(element.ToString());
    }

    public static void PrintError(string message)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Error: {message}");
        Console.ForegroundColor = prev;
    }
}
