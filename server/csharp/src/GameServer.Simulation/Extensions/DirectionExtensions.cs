namespace GameServer.Simulation.Extensions;

public static class DirectionExtensions
{
    public static ReadOnlySpan<Direction> All => [Direction.North, Direction.South, Direction.East, Direction.West];
}
