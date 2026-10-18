namespace GameServer.Sim;

public enum MoveResult
{
    Success,
    UnknownEntity,
    UnwalkableTarget,
    TargetOccupied,
    OnCooldown,
    EntityDead,
}
