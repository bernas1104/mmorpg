using System.Runtime.CompilerServices;

namespace GameServer.Protocol;

public sealed class ReliabilitySystem
{
    private ushort _localSequence = 0;
    private ushort _remoteSequence = 0;
    private uint _ackBitfield = 0;

    public byte[] WritePacket(byte[] gameData)
    {
        PacketHeader header = new(
            _localSequence++,
            _remoteSequence,
            _ackBitfield
        );

        byte[] packet = new byte[Unsafe.SizeOf<PacketHeader>() + gameData.Length];

        Buffer.BlockCopy(BitConverter.GetBytes(header.Sequence), 0, packet, 0, sizeof(ushort));
        Buffer.BlockCopy(BitConverter.GetBytes(header.Ack), 0, packet, sizeof(ushort), sizeof(ushort));
        Buffer.BlockCopy(BitConverter.GetBytes(header.AckBitfield), 0, packet, sizeof(ushort) * 2, sizeof(uint));
        Buffer.BlockCopy(gameData, 0, packet, sizeof(ushort) * 2 + sizeof(uint), gameData.Length);

        return packet;
    }

    public byte[] ReadPacket(byte[] packet)
    {
        if (packet.Length < Unsafe.SizeOf<PacketHeader>()) return [];           // Junk data

        byte[] header = new byte[Unsafe.SizeOf<PacketHeader>()];                // Allocate space for the header
        Buffer.BlockCopy(packet, 0, header, 0, Unsafe.SizeOf<PacketHeader>());  // Copy the header portion from the packet into the header array

        // Parse the header from the copied header array
        PacketHeader parsedHeader = new(
            BitConverter.ToUInt16(header, 0),
            BitConverter.ToUInt16(header, sizeof(ushort)),
            BitConverter.ToUInt32(header, sizeof(ushort) * 2)
        );

        UpdateAckBitfield(parsedHeader.Sequence);                               // Update the acknowledgment bitfield with the received sequence

        // If the received sequence is newer than the current remote sequence, update the remote sequence
        if (SequenceOps.IsSequenceNewer(parsedHeader.Sequence, _remoteSequence))
            _remoteSequence = parsedHeader.Sequence;

        // Allocate space for the payload and copy it from the packet
        byte[] payload = new byte[packet.Length - Unsafe.SizeOf<PacketHeader>()];
        // Copy the payload from the packet into the allocated payload array
        Buffer.BlockCopy(packet, Unsafe.SizeOf<PacketHeader>(), payload, 0, payload.Length);

        return payload;
    }

    private void UpdateAckBitfield(ushort sequence)
    {
        int shift = sequence - _remoteSequence;

        // Sequence is older than the current remote sequence
        if (shift < 0)
        {
            _ackBitfield |= (uint)1 << (-shift - 1);    // Mark the bit corresponding to the old sequence
            return;
        }

        // Shift to make room. Set the bit corresponding to the new sequence as acknowledged.
        _ackBitfield = _ackBitfield << shift | (uint)1 << (shift - 1);
    }
}
