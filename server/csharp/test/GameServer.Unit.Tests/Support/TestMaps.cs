namespace GameServer.Unit.Tests.Support;

public static class TestMaps
{
    /// <summary>
    /// A <c>width × height</c> map with a one-tile wall border and an open interior, encoded as
    /// <c>[row = y, column = x]</c> rows for <see cref="GameServer.Simulation.Map.FromRows"/>.
    /// </summary>
    public static string[,] GetWallBoundedTiles(int width, int height)
    {
        string[,] tiles = new string[height, width];

        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    tiles[y, x] = "#";
                    continue;
                }

                tiles[y, x] = ".";
            }

        return tiles;
    }
}
