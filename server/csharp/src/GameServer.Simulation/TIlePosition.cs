namespace GameServer.Simulation;

public readonly record struct TilePosition(int X, int Y)
{
    public TilePosition Step(Directions d) => d switch
    {
        Directions.North => this with { Y = Y - 1 },
        Directions.South => this with { Y = Y + 1 },
        Directions.East => this with { X = X + 1 },
        Directions.West => this with { X = X - 1 },
        _ => throw new ArgumentOutOfRangeException(nameof(d), $"Not expected direction value: {d}")
    };
}
