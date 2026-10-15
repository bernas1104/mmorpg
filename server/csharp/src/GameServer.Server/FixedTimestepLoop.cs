namespace GameServer.Server;

public sealed class FixedTimestepLoop : IDisposable
{
    public const string ThreadName = "FixedTimestepLoopThread";
    private readonly TickScheduler _scheduler;
    private readonly CancellationTokenSource _cts = new();
    public CancellationToken CancellationToken { get; }
    private Thread? _thread;
    private bool _disposed;

    public FixedTimestepLoop(TickScheduler scheduler)
    {
        _scheduler = scheduler;
        CancellationToken = _cts.Token;
    }

    public void Start(AdvanceDelegate advance, SleepDelegate sleep)
    {
        _thread = new(() => Run(advance, sleep, CancellationToken))
        {
            IsBackground = true,
            Name = ThreadName
        };
        _thread.Start();
    }

    public void Stop()
    {
        _cts.Cancel();
        _thread?.Join();
    }

    public void Run(AdvanceDelegate advance, SleepDelegate sleep, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var schedule = _scheduler.Advance();

            for (int i = 0; i < schedule.Ticks; i++)
                advance();

            if (schedule.SleepMs > 0)
                sleep(schedule.SleepMs);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _cts.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

public delegate void AdvanceDelegate();

public delegate void SleepDelegate(int milliseconds);
