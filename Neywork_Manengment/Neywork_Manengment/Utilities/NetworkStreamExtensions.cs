using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Neywork_Manengment.Utilities
{
    public static class NetworkStreamExtensions
    {
        public static async Task<byte[]> ReadExactAsync(this NetworkStream stream, int length, CancellationToken ct = default)
        {
            var buffer = new byte[length];
            var read = 0;
            while (read < length)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(read, length - read), ct);
                if (n == 0) throw new IOException("Remote closed while reading.");
                read += n;
            }
            return buffer;
        }
    }
}
