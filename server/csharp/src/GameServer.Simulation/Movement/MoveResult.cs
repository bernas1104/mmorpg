namespace GameServer.Simulation;

public enum MoveResult
{
    Success,
    UnknownEntity,
    UnwalkableTarget,
    TargetOccupied,
    OnCooldown,
    EntityDead,
}
