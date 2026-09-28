namespace GameServer.Simulation;

public sealed class Map
{
    public int Width { get; init; }
    public int Height { get; init; }
    public Tile[,] Tiles { get; }

    public Map(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height, nameof(height));

        Width = width;
        Height = height;
        Tiles = new Tile[width, height];

        for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                Tiles[x, y] = new Tile(true);
    }

    public bool IsWalkable(TilePosition position) => Tiles[position.X, position.Y].Walkable;

    public bool IsInBounds(TilePosition position) => position.X >= 0 && position.Y >= 0
        && position.X < Width && position.Y < Height;
}

public readonly struct Tile(bool walkable)
{
    public readonly bool Walkable { get; } = walkable;
}
