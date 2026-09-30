namespace GameServer.Simulation;

public sealed class World(Map map)
{
    public Map Map { get; } = map;
    public int IdCounter { get; private set; } = 0;
    private readonly Dictionary<EntityId, Entity> _entities = [];

    public EntityId SpawnPlayer(TilePosition position)
    {
        if (!Map.IsWalkable(position))
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"Position {position} is out of bounds or not walkable."
            );

        var id = new EntityId(IdCounter++);
        var player = new Entity(id, EntityKind.Player, position);

        _entities[id] = player;

        return id;
    }

    public Entity? GetEntity(EntityId id) => _entities.TryGetValue(id, out var entity)
        ? entity
        : null;

    public bool HasEntityOnTile(TilePosition tilePosition)
        => _entities.Values.Any(entity => entity.TilePosition == tilePosition);
}
