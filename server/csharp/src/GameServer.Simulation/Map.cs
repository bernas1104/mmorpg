namespace GameServer.Simulation;

public sealed class Map(int width, int height)
{
    public int Width { get; init; } = width;
    public int Height { get; init; } = height;
    public Tile[,] Tiles { get; } = new Tile[width, height];

    public bool IsWalkable(int x, int y) => Tiles[x, y].IsWalkable;

    public bool IsInBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
}

public struct Tile
{
    public bool IsWalkable;
}
