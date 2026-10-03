namespace GameServer.Simulation;

public sealed record WorldSnapshot(
    MapSnapshot Map,
    int IdCounter,
    IReadOnlyList<EntitySnapshot> Entities
)
{
    public static WorldSnapshot Of(World world) => new(
        world.Map.ToSnapshot(),
        world.IdCounter,
        [.. EntitySnapshot.FromEntities(world.GetAll())]
    );
}
