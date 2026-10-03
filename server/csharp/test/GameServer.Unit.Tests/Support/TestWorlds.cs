namespace GameServer.Unit.Tests.Support;

using GameServer.Simulation;

public static class TestWorlds
{
    public static World Open(int width = 20, int height = 20)
        => new(Map.FromRows(TestMaps.GetWallBoundedTiles(width, height)));

    public static World SingleCorridor() => new(Map.FromRows(SingleCorridorRows));

    public static World WalledIn() => new(Map.FromRows(WalledInRows));

    public static World SealedPair() => new(Map.FromRows(SealedPairRows));

    public static World Empty(int width = 20, int height = 20)
    {
        var rows = new string[height, width];

        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                rows[y, x] = ".";

        return new World(Map.FromRows(rows));
    }

    /// <summary>
    /// A fresh world with the same map and the same entities spawned in the same order. Entity
    /// state beyond kind and position is not carried over -- clones are for comparing two runs
    /// from the same starting point, not for restoring checkpoints.
    /// </summary>
    public static World Clone(World source)
    {
        var map = source.Map;
        var rows = new string[map.Height, map.Width];

        for (var y = 0; y < map.Height; y++)
            for (var x = 0; x < map.Width; x++)
                rows[y, x] = map.IsWalkable(new TilePosition(x, y)) ? "." : "#";

        var clone = new World(Map.FromRows(rows));

        foreach (var entity in source.GetAll())
        {
            if (entity.Kind == EntityKind.Player) clone.SpawnPlayer(entity.TilePosition);
            else if (entity.Kind == EntityKind.Npc) clone.SpawnNpc(entity.TilePosition);
        }

        return clone;
    }

    private static readonly string[,] SingleCorridorRows =
    {
        { "#", "#", "#", "#", "#" },
        { "#", ".", ".", ".", "#" },
        { "#", "#", ".", "#", "#" },
        { "#", "#", "#", "#", "#" },
        { "#", "#", "#", "#", "#" },
    };

    private static readonly string[,] WalledInRows =
    {
        { "#", "#", "#", "#", "#" },
        { "#", "#", "#", "#", "#" },
        { "#", "#", ".", "#", "#" },
        { "#", "#", "#", "#", "#" },
        { "#", "#", "#", "#", "#" },
    };

    private static readonly string[,] SealedPairRows =
    {
        { "#", "#", "#", "#", "#" },
        { "#", "#", ".", "#", "#" },
        { "#", "#", ".", "#", "#" },
        { "#", "#", "#", "#", "#" },
        { "#", "#", "#", "#", "#" },
    };
}
