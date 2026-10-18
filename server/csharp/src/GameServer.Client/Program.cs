using GameServer.Network;
using GameServer.Network.BitStream;

// using System.Text;

// Console.WriteLine("Running game server client loop...");

// try
// {
//     UdpSocket udpSocket = new();
//     ReliabilitySystem reliabilitySystem = new();

//     if (!udpSocket.Bind(3001))
//     {
//         Console.WriteLine("Failed to bind UDP socket on port 3001");
//         return -1;
//     }

//     udpSocket.SetNonBlocking(false);

//     Console.WriteLine("Server listening on port 3001...");

//     var out_address = string.Empty;
//     var out_port = ushort.MinValue;
//     var buffer = new byte[1024];

//     while (true)
//     {
//         var lineRead = Console.ReadLine();

//         if (!string.IsNullOrEmpty(lineRead))
//         {
//             var message = Encoding.UTF8.GetBytes(lineRead);
//             var packetToSend = reliabilitySystem.WritePacket(message);

//             udpSocket.SendTo("127.0.0.1", 3000, packetToSend);
//         }

//         var receivedBytes = udpSocket.ReceiveFrom(ref buffer, ref out_address, ref out_port);

//         if (receivedBytes > 0)
//         {
//             var packetRead = reliabilitySystem.ReadPacket(buffer);
//             Console.WriteLine($"Received {receivedBytes} bytes from {out_address}:{out_port} message: {Encoding.UTF8.GetString(packetRead, 0, packetRead.Length)}");
//         }
//     }
// }
// catch (Exception ex)
// {
//     Console.WriteLine($"An error occurred: {ex.Message}");
//     return -1;
// }

// return 0;

var bitWriter = new BitWriter();

bitWriter.WriteBool(true);
bitWriter.Write(4, 8);

var bitReader = new BitReader(bitWriter.GetData());

var readBool = bitReader.ReadBool();
var readInt = bitReader.Read(out int num, 8);

Console.WriteLine(readBool);
Console.WriteLine(readInt ? num : "Error while reading the integer");
