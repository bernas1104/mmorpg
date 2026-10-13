namespace GameServer.Simulation;

public sealed class HostEventQueue
{
    private List<QueuedHostEvent> _queue = [];
    private long _nextSequence = 0;

    public void Enqueue(HostEvent hostEvent) =>
        _queue.Add(new QueuedHostEvent(hostEvent, _nextSequence++));

    public IReadOnlyList<QueuedHostEvent> Drain()
    {
        var batch = _queue;
        _queue = [];
        return batch;
    }
}

public sealed record QueuedHostEvent(HostEvent HostEvent, long Sequence)
{
    public override string ToString() => HostEvent.ToString();
}
