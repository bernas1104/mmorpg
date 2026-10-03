namespace GameServer.Simulation;

public sealed record WorldSnapshot(
    Map Map,
    int IdCounter,
    IReadOnlyList<EntitySnapshot> Entities
)
{
    public static WorldSnapshot Of(World world) => new(
        world.Map,
        world.IdCounter,
        [.. EntitySnapshot.FromEntities(world.GetAll())]
    );
}
