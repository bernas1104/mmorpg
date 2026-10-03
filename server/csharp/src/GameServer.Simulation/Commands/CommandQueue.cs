namespace GameServer.Simulation;

public sealed class CommandQueue
{
    private List<QueuedCommand> _pending = [];
    private long _nextSequence = 0;

    public void Enqueue(Command command) =>
        _pending.Add(new QueuedCommand(command, _nextSequence++));

    public IReadOnlyList<QueuedCommand> Drain()
    {
        var batch = _pending;
        _pending = [];
        return batch;
    }
}

public sealed record QueuedCommand(Command Command, long Sequence)
{
    public override string ToString() => Command.ToString();
};
