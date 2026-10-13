namespace GameServer.Simulation;

public sealed record WorldSnapshot(
    MapSnapshot Map,
    IReadOnlyList<EntitySnapshot> Entities
)
{
    public static WorldSnapshot Of(World world) => new(
        world.Map.ToSnapshot(),
        [.. EntitySnapshot.FromEntities(world.GetAll())]
    );

    public bool Equals(WorldSnapshot? other) =>
        other is not null
        && Map == other.Map
        && Entities.SequenceEqual(other.Entities);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Map);

        foreach (var entity in Entities)
            hash.Add(entity);

        return hash.ToHashCode();
    }
}
