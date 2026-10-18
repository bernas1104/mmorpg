using GameServer.Protocol;
using GameServer.Protocol.BitStream;

using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Running game server client loop...");

try
{
    Socket tcpSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    tcpSocket.Bind(new IPEndPoint(IPAddress.Any, 3001));
    tcpSocket.Connect("127.0.0.1", 3000);

    Console.WriteLine("Server listening on port 3001...");

    var out_address = string.Empty;
    var out_port = ushort.MinValue;
    var buffer = new byte[1024];

    while (true)
    {
        var lineRead = Console.ReadLine();

        if (!string.IsNullOrEmpty(lineRead))
        {
            var message = Encoding.UTF8.GetBytes(lineRead);
            tcpSocket.Send(Encoding.UTF8.GetBytes(lineRead));
        }

        var receivedBytes = tcpSocket.Receive(buffer);

        if (receivedBytes > 0)
        {
            Console.WriteLine($"Received {receivedBytes} bytes from {out_address}:{out_port} message: {Encoding.UTF8.GetString(buffer, 0, receivedBytes)}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
    return -1;
}

return 0;

// var bitWriter = new BitWriter();

// bitWriter.WriteBool(true);
// bitWriter.Write(4, 8);

// var bitReader = new BitReader(bitWriter.GetData());

// var readBool = bitReader.ReadBool();
// var readInt = bitReader.Read(out int num, 8);

// Console.WriteLine(readBool);
// Console.WriteLine(readInt ? num : "Error while reading the integer");
