using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string host, int port, string? str = null, string? hex = null)
    {
        Console.WriteLine(new { host, port, str, hex });

        Socket socket = new(SocketType.Stream, ProtocolType.Tcp) { ReceiveTimeout = 3000 };
        socket.Connect(host, port);
        switch (str, hex)
        {
            case (string s, null):
                var recv = RR(Encoding.Default.GetBytes(s));
                Console.WriteLine(Encoding.Default.GetString(recv));
                break;
            case (null, string h):
                RR(Convert.FromHexString(Regex.Replace(h, "[^0-9a-zA-Z]", "")));
                break;
            default: throw new("요청할 str이나 16진수 hex중 하나를 포함해야합니다.");
        }
        byte[] RR(byte[] bin)
        {
            Console.WriteLine(new { send = BitConverter.ToString(bin), len = socket.Send(bin) });
            Console.WriteLine(new { recv = BitConverter.ToString(bin = Recv().ToArray()) });
            return bin;
            IEnumerable<byte> Recv()
            {
                for (var res = new byte[1]; ;)
                { try { if (socket.Receive(res) is not 1) throw new("Receive Zero"); } catch { yield break; } yield return res[0]; }
            }
        }
    }
}