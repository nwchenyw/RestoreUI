using System.IO.Pipes;
using System.Text;

namespace TopCPR.Core;

public static class PipeClient
{
    public static string Send(string token, string command)
    {
        using var client = new NamedPipeClientStream(".", PipeProtocol.PipeName, PipeDirection.InOut);
        client.Connect(4000);

        using var writer = new StreamWriter(client, Encoding.UTF8, 1024, true) { AutoFlush = true };
        using var reader = new StreamReader(client, Encoding.UTF8, true, 1024, true);

        writer.WriteLine($"AUTH:{token}|{command}");
        return reader.ReadLine() ?? string.Empty;
    }
}
