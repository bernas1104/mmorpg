using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using GameServer.Protocol.Extensions;

namespace GameServer.Protocol.BitStream;

public sealed class BitReader(byte[] buffer)
{
    private readonly byte[] _buffer = buffer;
    private int _bitIndex = 0;                  // Current bit index in the *last* byte (0-7)
    private ulong _byteIndex = 0;               // Current byte index in the buffer

    public bool Read<T>(out T out_data, int? bits = null) where T : IBinaryInteger<T>
    {
        var bitCount = bits ?? Unsafe.SizeOf<T>() * 8;

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bitCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bitCount, sizeof(ulong) * 8);

        out_data = default!;

        if (!CanRead(bitCount)) return false;

        ulong value = 0;
        int bitsRead = 0;

        while (bitsRead < bitCount)
        {
            // Read from current byte
            ulong byteVal = _buffer[_byteIndex];

            // Shift down to remove bits we've already processed in this byte
            byteVal >>= _bitIndex;

            int spaceInByte = 8 - _bitIndex;
            int bitsToGrab = Math.Min(bitCount - bitsRead, spaceInByte);

            // Mask out just the bits we want
            ulong mask = (1UL << bitsToGrab) - 1;
            ulong fragment = byteVal & mask;

            // Add fragment to our result
            value |= fragment << bitsRead;

            // Advance state
            bitsRead += bitsToGrab;
            _bitIndex += bitsToGrab;

            if (_bitIndex >= 8)
            {
                _bitIndex = 0;
                _byteIndex++;
            }
        }

        out_data = T.CreateTruncating(value);

        return true;
    }

    public bool ReadBool()
    {
        return Read(out byte result, 1)
            ? result != 0
            : throw new InvalidOperationException("Unable to read bool value from the bit stream.");
    }

    public float ReadFloat(bool accurateMode = false)
    {
        return Read(out ushort value, accurateMode ? sizeof(ushort) * 8 : sizeof(byte) * 8)
            ? value.DecompressUnitFloat(accurateMode)
            : throw new InvalidOperationException("Unable to read float value from the bit stream.");
    }

    public string ReadString()
    {
        var length = Read(out int value, sizeof(int) * 8)
            ? value
            : throw new InvalidOperationException("Unable to read string length from the bit stream.");

        if (length == 0) return string.Empty;

        var bytes = new byte[length];
        for (int i = 0; i < length; i++)
        {
            bytes[i] = Read(out byte sChar, 8)
                ? sChar
                : throw new InvalidOperationException("Unable to read string character from the bit stream.");
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private bool CanRead(int bits)
    {
        ulong totalBits = (ulong)_buffer.Length * 8;
        ulong currentBitPos = _byteIndex * 8 + (ulong)_bitIndex;

        return (currentBitPos + (ulong)bits) <= totalBits;
    }
}
