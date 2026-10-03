namespace GameServer.Simulation;

public enum MoveResult
{
    Success,
    InvalidEntity,
    InvalidTarget,
    InvalidExhaustion,
    TileOccupied,
    InvalidDead
}
