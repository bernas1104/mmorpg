namespace GameServer.Simulation;

public readonly record struct TilePosition(int X, int Y)
{
    public TilePosition Step(Direction d) => d switch
    {
        Direction.North => this with { Y = Y - 1 },
        Direction.South => this with { Y = Y + 1 },
        Direction.East => this with { X = X + 1 },
        Direction.West => this with { X = X - 1 },
        _ => throw new ArgumentOutOfRangeException(nameof(d), $"Not expected direction value: {d}")
    };
}
