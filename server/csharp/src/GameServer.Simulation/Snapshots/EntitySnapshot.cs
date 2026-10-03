
namespace GameServer.Simulation;

public sealed record EntitySnapshot(
    EntityId Id,
    EntityKind Kind,
    TilePosition TilePosition,
    long NextMoveAllowedTick,
    int MaxHealth,
    int Health,
    long NextAttackAllowedTick,
    LifecycleState LifecycleState,
    long? RemovalTick
)
{
    public static IEnumerable<EntitySnapshot> FromEntities(IEnumerable<Entity> entities)
    {
        return entities.Select(e => new EntitySnapshot(
            e.Id,
            e.Kind,
            e.TilePosition,
            e.NextMoveAllowedTick,
            e.MaxHealth,
            e.Health,
            e.NextAttackAllowedTick,
            e.LifecycleState,
            e.RemovalTick
        ));
    }
};
