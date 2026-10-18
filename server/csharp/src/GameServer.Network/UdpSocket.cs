using System.Net;
using System.Net.Sockets;

namespace GameServer.Network;

public sealed class UdpSocket
{
    private readonly Socket _socketHandler;

    public UdpSocket()
    {
        _socketHandler = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }

    ~UdpSocket()
    {
        _socketHandler.Close();
    }

    public bool Bind(ushort port)
    {
        try
        {
            _socketHandler.Bind(new IPEndPoint(IPAddress.Any, port));
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to bind UDP socket on port {port}: {ex.Message}");
            return false;
        }
    }

    public void SendTo(string address, ushort port, byte[] data)
    {
        var bytesSent = _socketHandler.SendTo(data, new IPEndPoint(IPAddress.Parse(address), port));

        if (bytesSent <= 0) Console.WriteLine($"Failed to send UDP packet to {address}:{port}");
    }

    public int ReceiveFrom(ref byte[] out_buffer, ref string out_address, ref ushort out_port)
    {
        EndPoint address = new IPEndPoint(IPAddress.Any, 0);

        int bytesRead;

        try
        {
            bytesRead = _socketHandler.ReceiveFrom(out_buffer, ref address);
        }
        catch (SocketException ex)
            when (ex.SocketErrorCode is SocketError.WouldBlock or SocketError.TryAgain)
        {
            return 0;
        }

        out_address = ((IPEndPoint)address).Address.ToString();
        out_port = (ushort)((IPEndPoint)address).Port;

        return bytesRead;
    }

    public void SetNonBlocking(bool isNonBlocking) => _socketHandler.Blocking = !isNonBlocking;
}
