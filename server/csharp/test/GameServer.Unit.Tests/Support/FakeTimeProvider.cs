namespace GameServer.Unit.Tests.Support;

/// <summary>
/// A manually advanced <see cref="TimeProvider"/> so tests can exercise time-based pacing
/// without sleeps or wall-clock dependence.
/// </summary>
public sealed class FakeTimeProvider : TimeProvider
{
    private long _timestamp;

    public override long TimestampFrequency { get; } = 1_000_000;

    public override long GetTimestamp() => _timestamp;

    public void AdvanceMs(int milliseconds) => AdvanceTicks(TimestampFrequency * milliseconds / 1000);

    public void AdvanceTicks(long ticks) => _timestamp += ticks;
}
