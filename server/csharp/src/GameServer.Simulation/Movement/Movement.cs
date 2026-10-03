
namespace GameServer.Simulation;

public static class Movement
{
    public const int MoveCooldownTicks = 10;

    public static MoveResult TryMove(
        World world,
        EntityId entityId,
        Direction direction,
        long currentTick
    )
    {
        var entity = world.GetEntity(entityId);
        if (entity is null)
            return MoveResult.InvalidEntity;

        if (entity.LifecycleState != LifecycleState.Alive)
            return MoveResult.InvalidDead;

        var targetPosition = entity.TilePosition.Step(direction);

        if (!world.Map.IsWalkable(targetPosition))
            return MoveResult.InvalidTarget;

        if (world.HasEntityOnTile(targetPosition))
            return MoveResult.TileOccupied;

        if (currentTick < entity.NextMoveAllowedTick)
            return MoveResult.InvalidExhaustion;

        entity.MoveTo(targetPosition);
        entity.UpdateNextMoveAllowedTick(currentTick + MoveCooldownTicks);

        return MoveResult.Success;
    }
}
