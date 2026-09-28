namespace GameServer.Simulation;

public sealed class World(Map map)
{
    public Map Map { get; } = map;
    public Dictionary<EntityId, Entity> Entities { get; } = [];
    public int IdCounter { get; private set; } = 0;

    public EntityId SpawnPlayer(Tile position)
    {
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
