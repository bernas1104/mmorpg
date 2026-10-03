namespace GameServer.Simulation;

public readonly struct Tile(bool walkable)
{
    public readonly bool Walkable { get; } = walkable;
}
