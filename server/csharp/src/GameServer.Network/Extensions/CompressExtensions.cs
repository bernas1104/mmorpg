namespace GameServer.Network.Extensions;

public static class CompressExtensions
{
    public static ushort CompressUnitFloat(this float value, bool accurateMode = false)
    {
        var scale = accurateMode ? 65535f : 255f;

        // Clamp to ensure 0.0-1.0
        float clamped = Math.Clamp(value, 0f, 1f);

        // Scale and cast to integer
        // We multiply by 255 to fill the byte range
        return (ushort)(clamped * scale);
    }

    public static float DecompressUnitFloat(this ushort value, bool accurateMode = false)
        => value / (accurateMode ? 65535f : 255f);
}
