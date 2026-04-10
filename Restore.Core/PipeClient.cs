using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Restore.Core
{
    public static class PipeClient
    {
        public static string SendCommand(string command, int timeoutMs = 4000)
        {
            using (var client = new NamedPipeClientStream(".", PipeConstants.PipeName, PipeDirection.InOut))
            {
                client.Connect(timeoutMs);

                using (var writer = new StreamWriter(client, Encoding.UTF8, 1024, true))
                using (var reader = new StreamReader(client, Encoding.UTF8, false, 1024, true))
                {
                    writer.AutoFlush = true;
                    writer.WriteLine(command ?? string.Empty);
                    return reader.ReadLine() ?? string.Empty;
                }
            }
        }
    }
}
