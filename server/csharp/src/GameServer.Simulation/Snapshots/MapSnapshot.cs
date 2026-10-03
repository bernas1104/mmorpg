namespace GameServer.Simulation;

/// <summary>
/// Immutable, plain-data representation of the map: width, height, and walkability as rows of
/// '.' (walkable) and '#' (wall) joined by newlines. The rows are one string so record equality
/// is structural for free -- a live <see cref="Map"/> reference would compare by identity.
/// </summary>
public sealed record MapSnapshot(int Width, int Height, string Rows)
{
    public Map ToMap()
    {
        var rows = new string[Height, Width];

        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                rows[y, x] = Rows[y * (Width + 1) + x] == '.' ? "." : "#";

        return Map.FromRows(rows);
    }
}
