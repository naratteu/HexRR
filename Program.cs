using Cocona;
using Cocona.Command.Binder;
using Cocona.Lite;
using ConsoleTables;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

var builder = CoconaLiteApp.CreateBuilder();
builder.Services.AddSingleton<ICoconaValueConverter, IPEndPointConverter>();
await builder.Build().RunAsync(Program.Main);

class IPEndPointConverter : CoconaValueConverter, ICoconaValueConverter
{
    object? ICoconaValueConverter.ConvertTo(Type t, string? value)
    => typeof(IPEndPoint) == t && IPEndPoint.TryParse(value ?? "", out var ip) ? ip : ConvertTo(t, value);
}

partial class Program
{
    /// <summary>주소로 바이트요청 후 바이트응답을 출력합니다.</summary>
    /// <param name="host">(host, port), ip, uri중 하나의 조합으로 주소를 입력해야 합니다.</param>
    /// <param name="port"></param>
    /// <param name="ip">ip:port 형태로 입력합니다.</param>
    /// <param name="uri">http://host(:80), ftp:// ..</param>
    /// <param name="hex">전송할 바이트를 16진수로 입력합니다. 예외적인 문자는 무시됩니다.</param>
    /// <param name="str">전송할 문자열을 입력합니다. hex, (str, encoding)중 하나의 조합으로 입력해야 합니다.</param>
    /// <param name="encoding">문자열의 인코딩을 지정합니다. 입력하지 않을경우 시스템 기본값을 사용합니다.</param>
    /// <param name="showEncodings">입력가능한 인코딩목록을 출력합니다.</param>
    static async Task Main(string? host = null, int? port = null, IPEndPoint? ip = null, Uri? uri = null, string? hex = null, string? str = null, string? encoding = null, bool showEncodings = false)
    {
        if (showEncodings)
        {
            Console.WriteLine(ConsoleTable.From(TrimEx(Encoding.GetEncodings().Select(info => new { info.Name, info.DisplayName, info.CodePage }))).ToMarkDownString());
            static IEnumerable<T> TrimEx<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(IEnumerable<T> e) => e;
            return;
        }
        Console.WriteLine(new { host, port, ip, uri, hex, str, encoding });

        using Socket socket = new(SocketType.Stream, ProtocolType.Tcp) { ReceiveTimeout = 3000 };
        try
        {
            await ((host, port, ip, uri) switch
            {
                ({ } _host, { } _port, null, null) => socket.ConnectAsync(_host, _port),
                (null, null, { } _ip, null) => socket.ConnectAsync(_ip),
                (null, null, null, { } _uri) => socket.ConnectAsync(_uri.Host, _uri.Port),
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(new { ex.Message });
        }
        var (data, enc) = (hex, str, encoding) switch
        {
            ({ } _hex, null, null) => (_hex, HexEncoding.Inst),
            (null, { } _str, null) => (_str, Encoding.Default),
            (null, { } _str, { } e) when int.TryParse(e, out var codepage) => (_str, Encoding.GetEncoding(codepage)),
            (null, { } _str, { } e) => (_str, Encoding.GetEncoding(e)),
        };
        byte[] bin = enc.GetBytes(data);
        object len;
        try { len = socket.Send(bin); } catch(Exception ex) { len = ex.Message; }
        Console.WriteLine(new { send = BitConverter.ToString(bin), len });
        Console.WriteLine(new { recv = BitConverter.ToString(bin = Recv().ToArray()), len = bin.Length });
        Console.WriteLine(enc.GetString(bin));
        IEnumerable<byte> Recv()
        {
            for (var res = new byte[1]; ;)
            { try { if (socket.Receive(res) is not 1) throw new("Receive Zero"); } catch { yield break; } yield return res[0]; }
        }
    }
}

partial class HexEncoding : ASCIIEncoding
{
    internal readonly static HexEncoding Inst = new();

    [GeneratedRegex("[^0-9a-zA-Z]")] private static partial Regex HexRegex();
    public override byte[] GetBytes(string s) => System.Convert.FromHexString(HexRegex().Replace(s, ""));
    public override string GetString(byte[] b) => System.Convert.ToHexString(b);
}