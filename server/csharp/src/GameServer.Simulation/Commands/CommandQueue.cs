namespace GameServer.Simulation;

public sealed class CommandQueue
{
    private List<QueuedCommand> _commandQueue = [];
    private long _nextSequence = 0;

    public void Enqueue(Command command) =>
        _commandQueue.Add(new QueuedCommand(command, _nextSequence++));

    public IReadOnlyList<QueuedCommand> Drain()
    {
        var batch = _commandQueue;
        _commandQueue = [];
        return batch;
    }
}

public sealed record QueuedCommand(Command Command, long Sequence)
{
    public override string ToString() => Command.ToString();
}
