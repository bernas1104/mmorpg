namespace GameServer.Simulation;

public sealed class Entity(EntityId id, EntityKind kind, TilePosition tilePosition)
{
    public EntityId Id { get; } = id;
    public EntityKind Kind { get; } = kind;
    public TilePosition TilePosition { get; private set; } = tilePosition;
}
