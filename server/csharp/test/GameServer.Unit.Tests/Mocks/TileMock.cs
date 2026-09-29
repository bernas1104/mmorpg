using GameServer.Simulation;

namespace GameServer.Unit.Tests.Mocks;

public static class TileMock
{
    public static string[,] GetWallBoundedTiles(int width, int height)
    {
        string[,] tiles = new string[width, height];

        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
            {
                if (i == 0 || i == width - 1 || j == 0 || j == height - 1)
                {
                    tiles[i, j] = "#";
                    continue;
                }

                tiles[i, j] = ".";
            }

        return tiles;
    }
}
