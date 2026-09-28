namespace GameServer.Simulation;

public sealed class World(Map map)
{
    public Map Map { get; } = map;
    public Dictionary<EntityId, Entity> Entities { get; } = [];
    public int IdCounter { get; private set; } = 0;

    public EntityId SpawnPlayer(TilePosition position)
    {
        if (!Map.IsInBounds(position) || !Map.IsWalkable(position))
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"Position {position} is out of bounds or not walkable."
            );

        var id = new EntityId(IdCounter++);
        var player = new Entity(id, EntityKind.Player, position);

        Entities[id] = player;

        return id;
    }

    public Entity? GetEntity(EntityId id)
    {
        return Entities.TryGetValue(id, out var entity) ? entity : null;
    }
}
