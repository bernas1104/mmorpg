namespace GameServer.Simulation.Snapshots;

public static class Snapshot
{
    public static WorldSnapshot CreateWorldSnapshot(World world)
    {
        return new WorldSnapshot(
            world.Map,
            world.IdCounter,
            [.. EntitySnapshot.FromEntities(world.GetAll())]
        );
    }
}
