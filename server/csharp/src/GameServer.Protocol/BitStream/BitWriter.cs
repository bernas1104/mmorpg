using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using GameServer.Protocol.Extensions;

namespace GameServer.Protocol.BitStream;

public sealed class BitWriter(uint initialCapacity = 128)
{
    private byte[] _buffer = new byte[initialCapacity];
    private int _bitIndex = 0;   // Current bit index in the *last* byte (0-7)
    private int _byteIndex = 0;

    public void Write<T>(T data, int? bits = null) where T : IBinaryInteger<T>
    {
        var bitCount = bits ?? Unsafe.SizeOf<T>() * 8;

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bitCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bitCount, sizeof(ulong) * 8);

        ulong value = ulong.CreateTruncating(data) & ulong.MaxValue >> (64 - bitCount);

        while (bitCount > 0)
        {
            // Start new byte
            if (_bitIndex == 0)
            {
                if (_byteIndex >= _buffer.Length)
                    Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, 1));

                _buffer[_byteIndex++] = 0;
            }

            // Calculate how many bits fit in the current byte
            int spaceInByte = 8 - _bitIndex;
            int bitsToWrite = Math.Min(spaceInByte, bitCount);

            // Shift value to align with current bit index
            // Write from low to high (Little Endian)
            _buffer[_byteIndex - 1] |= (byte)(value << _bitIndex);

            // Advance state
            value >>= bitsToWrite;                      // Discard the bits we've just written
            bitCount -= bitsToWrite;
            _bitIndex = (_bitIndex + bitsToWrite) % 8;
        }
    }

    public void WriteBool(bool value) => Write(value ? 1 : 0, 1);

    public void WriteFloat(float value, bool accurateMode = false)
    {
        if (accurateMode)
            Write(value.CompressUnitFloat(true), sizeof(ushort) * 8);
        else
            Write((byte)value.CompressUnitFloat(), sizeof(byte) * 8);
    }

    public void WriteString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Write(bytes.Length, sizeof(int) * 8);       // Write the length of the string first
        foreach (var b in bytes)
            Write(b, 8);                            // Write each byte of the string
    }

    public byte[] GetData() => [.. _buffer[.._byteIndex]];

    public uint GetTotalBits() => (uint)(_byteIndex * 8 - (8 - _bitIndex) % 8);
}
