namespace GameServer.Simulation;

public sealed class Entity(EntityId id, EntityKind kind, Tile position)
{
    public EntityId Id { get; } = id;
    public EntityKind Kind { get; } = kind;
    public Tile Position { get; private set; } = position;
}
