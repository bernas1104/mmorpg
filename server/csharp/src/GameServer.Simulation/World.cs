
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

    public EntityId SpawnNPC(TilePosition position)
    {
        if (!Map.IsWalkable(position))
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"Position {position} is out of bounds or not walkable."
            );

        var id = new EntityId(IdCounter++);
        var npc = new Entity(id, EntityKind.NPC, position);

        _entities[id] = npc;

        return id;
    }

    public Entity? GetEntity(EntityId id) => _entities.TryGetValue(id, out var entity)
        ? entity
        : null;

    /// <summary>
    /// Every entity of the given kind, in ascending <see cref="EntityId"/> order.
    /// </summary>
    /// <remarks>
    /// The sort is the contract, not a nicety. Callers -- the ai think pass above all -- draw
    /// from the simulation's single generator once per entity in whatever order they appear, so
    /// this sequence's order decides how many draws happen and in what sequence, and therefore
    /// becomes part of the simulation's identity. Dictionary enumeration order is explicitly not
    /// guaranteed by .NET and does shift when entities are added or removed, so iterating the raw
    /// values would make behaviour correct only by accident and until the next edit.
    /// </remarks>
    public IEnumerable<Entity> GetAllOfKind(EntityKind kind) => _entities.Values
        .Where(entity => entity.Kind == kind)
        .OrderBy(entity => entity.Id.Value);

    public IEnumerable<Entity> GetAll() => [.. _entities.Values.OrderBy(entity => entity.Id.Value)];

    public IEnumerable<Entity> GetAllAliveNPCs() => GetAllOfKind(EntityKind.NPC)
        .Where(entity => entity.LifecycleState == LifecycleState.Alive);

    public IEnumerable<Entity> GetAllDead() =>
        [.. GetAll().Where(entity => entity.LifecycleState == LifecycleState.Dead)];

    public IEnumerable<Entity> GetAllRemoved() =>
        [.. GetAll().Where(entity => entity.LifecycleState == LifecycleState.Removed)];

    public bool HasEntityOnTile(TilePosition tilePosition)
        => GetAll().Any(
            entity => entity.TilePosition == tilePosition
                && entity.LifecycleState == LifecycleState.Alive
        );

    public void RemoveMarkedEntities()
    {
        foreach (var id in _entities.Values.ToArray()
            .Where(entity => entity.LifecycleState == LifecycleState.Removed)
            .Select(entity => entity.Id)
        ) _entities.Remove(id);
    }

    public static World CreateFromSnapshot(WorldSnapshot snapshot)
    {
        var world = new World(snapshot.Map)
        {
            IdCounter = snapshot.IdCounter
        };


        foreach (var entitySnapshot in snapshot.Entities)
        {
            var entity = Entity.CreateFromSnapshot(entitySnapshot);
            world._entities.Add(entity.Id, entity);
        }

        return world;
    }
}
