namespace GameServer.Protocol;

public readonly struct PacketHeader(ushort sequence, ushort ack, uint ackBitfield)
{
    public readonly ushort Sequence = sequence;     // ID of the packet
    public readonly ushort Ack = ack;               // ID of the last received packet
    public readonly uint AckBitfield = ackBitfield; // History of the previous 32 received packets

    public static readonly uint MAX_PACKET_SIZE = 1400;
}
