namespace GameServer.Simulation;

public sealed class Map
{
    public int Width { get; }
    public int Height { get; }
    private readonly Tile[,] _tiles;

    private Map(int width, int height, Tile[,]? tiles = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height, nameof(height));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            width * height,
            tiles?.Length ?? int.MaxValue,
            nameof(tiles.Length)
        );

        Width = width;
        Height = height;
        _tiles = tiles ?? new Tile[width, height];
    }

    public bool IsWalkable(TilePosition position) => IsInBounds(position)
        && _tiles[position.X, position.Y].Walkable;

    public bool IsInBounds(TilePosition position) => position.X >= 0 && position.Y >= 0
        && position.X < Width && position.Y < Height;

    public static Map CreateEmpty(int width, int height) => new(width, height);

    public static Map FromRows(string[,] rows)
    {
        int width = rows.GetLength(0);
        int height = rows.GetLength(1);
        Tile[,] tiles = new Tile[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                tiles[x, y] = rows[x, y] == "." ? new Tile(true) : new Tile(false);

        return new Map(width, height, tiles);
    }

    public static Map FromTiles(int width, int height, Tile[,] tiles) => new(width, height, tiles);
}

public readonly struct Tile(bool walkable)
{
    public readonly bool Walkable { get; } = walkable;
}
