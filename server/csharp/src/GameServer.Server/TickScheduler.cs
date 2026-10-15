namespace GameServer.Server;

public sealed class TickScheduler
{
    public const int DefaultTickDurationMs = 50;
    public const int MaxCatchUpTicks = 5;
    private readonly TimeProvider _timeProvider;
    private readonly long _timestampFrequency;
    private readonly long _tickDurationTicks;
    private readonly int _maxCatchUpTicks;
    private long _previous;
    private long _accumulator;
    private long _nextTickDue;

    public TickScheduler(
        TimeProvider timeProvider,
        int tickDurationMs = DefaultTickDurationMs,
        int maxCatchUpTicks = MaxCatchUpTicks
    )
    {
        _timeProvider = timeProvider;
        _timestampFrequency = timeProvider.TimestampFrequency;
        _tickDurationTicks = _timestampFrequency * tickDurationMs / 1000;
        _maxCatchUpTicks = maxCatchUpTicks;
        _previous = timeProvider.GetTimestamp();
        _nextTickDue = _previous + _tickDurationTicks;
    }

    public TickSchedule Advance()
    {
        long now = _timeProvider.GetTimestamp();
        _accumulator += now - _previous;
        _previous = now;

        int ticks = 0;
        while (_accumulator >= _tickDurationTicks && ticks < _maxCatchUpTicks)
        {
            _accumulator -= _tickDurationTicks;
            _nextTickDue += _tickDurationTicks;
            ticks++;
        }

        if (_accumulator >= _tickDurationTicks)
        {
            _accumulator = 0;
            _nextTickDue = now + _tickDurationTicks;
        }

        int sleepMs = (int)((_nextTickDue - now) * 1000 / _timestampFrequency);

        return new TickSchedule(ticks, sleepMs);
    }
}

public readonly record struct TickSchedule(int Ticks, int SleepMs);
