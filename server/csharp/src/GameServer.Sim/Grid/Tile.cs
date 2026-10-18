namespace GameServer.Sim;

public readonly struct Tile(bool walkable)
{
    public readonly bool Walkable { get; } = walkable;
}
