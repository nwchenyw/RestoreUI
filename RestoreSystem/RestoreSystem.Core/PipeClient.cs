using System.IO.Pipes;
using System.Text;
using RestoreSystem.Shared;

namespace RestoreSystem.Core;

public static class PipeClient
{
    public static string Send(RestoreCommand command, int timeoutMs = 4000)
    {
        return SendRaw(PipeConstants.ToPipeCommand(command), timeoutMs);
    }

    public static string SendAuthenticated(string authToken, RestoreCommand command, int timeoutMs = 4000)
    {
        return SendAuthenticatedRaw(authToken, PipeConstants.ToPipeCommand(command), timeoutMs);
    }

    public static string SendAuthenticatedRaw(string authToken, string command, int timeoutMs = 4000)
    {
        var payload = $"AUTH:{authToken}|{command}";
        return SendRaw(payload, timeoutMs);
    }

    public static string SendRaw(string command, int timeoutMs = 4000)
    {
        using var client = new NamedPipeClientStream(".", PipeConstants.PipeName, PipeDirection.InOut);
        client.Connect(timeoutMs);

        using var writer = new StreamWriter(client, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(client, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

        writer.WriteLine(command);
        return reader.ReadLine() ?? string.Empty;
    }
}
