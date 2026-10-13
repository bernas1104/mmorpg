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

    public bool Equals(WorldSnapshot? other) =>
        other is not null
        && Map == other.Map
        && IdCounter == other.IdCounter
        && Entities.SequenceEqual(other.Entities);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Map);
        hash.Add(IdCounter);

        foreach (var entity in Entities)
            hash.Add(entity);

        return hash.ToHashCode();
    }
}
