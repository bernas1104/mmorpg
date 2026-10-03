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

        if (tiles is not null && (tiles.GetLength(0) != width || tiles.GetLength(1) != height))
        {
            throw new ArgumentException(
                $"Tile grid shape [{tiles.GetLength(0)}, {tiles.GetLength(1)}] " +
                $"does not match map dimensions [{width}, {height}].",
                nameof(tiles)
            );
        }

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
        int width = rows.GetLength(1);
        int height = rows.GetLength(0);
        Tile[,] tiles = new Tile[width, height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                _ = rows[y, x] switch
                {
                    "." => tiles[x, y] = new Tile(true),
                    "#" => tiles[x, y] = new Tile(false),
                    _ => throw new ArgumentException($"Invalid tile character: {rows[y, x]}")
                };

        return new Map(width, height, tiles);
    }

    public static Map FromTiles(int width, int height, Tile[,] tiles) => new(width, height, tiles);

    public MapSnapshot ToSnapshot()
    {
        var builder = new System.Text.StringBuilder(Height * (Width + 1));

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
                builder.Append(_tiles[x, y].Walkable ? '.' : '#');

            if (y < Height - 1) builder.Append('\n');
        }

        return new MapSnapshot(Width, Height, builder.ToString());
    }
}
