using GameServer.Simulation;

namespace GameServer.Server;

public sealed class GameHost(Ingress ingress, Simulation.Simulation simulation, string threadName)
{
    public Ingress Ingress { get; } = ingress;
    private readonly Simulation.Simulation _simulation = simulation;
    private bool _advancing;
    private readonly string _threadName = threadName;

    public void Advance()
    {
        AssertThread();
        AssertNotAdvanced();
        _advancing = true;

        try
        {
            var inputs = Ingress.Drain();

            foreach (var input in inputs)
            {
                switch (input.Type)
                {
                    case ClientInputType.Request:
                        _simulation.EnqueueHostEvent(HandleRequest((ClientRequest)input));
                        break;
                    case ClientInputType.Command:
                        _simulation.EnqueueCommand(((ClientCommandInput)input).Command);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown input type: {input.Type}");
                }
            }

            _simulation.Tick();
            Publish();

            _advancing = false;
        }
        catch
        {
            _advancing = false;
            throw;
        }
    }

    private HostEvent HandleRequest(ClientRequest input)
    {
        switch (input.Request.Type)
        {
            case RequestType.Join:
                if (input.Request.TilePosition is null)
                    throw new InvalidOperationException("Tile position is required for join request.");

                return new PlayerSpawned(
                    _simulation.TickNumber,
                    input.Request.EntityId,
                    input.Request.TilePosition!.Value
                );
            case RequestType.Leave:
                return new PlayerDespawned(
                    _simulation.TickNumber,
                    input.Request.EntityId
                );
            default:
                throw new InvalidOperationException($"Unknown request type: {input.Request.Type}");
        }
    }

    private void Publish()
    {
        AssertThread();
        // Stub method for publishing game state
    }

    private void AssertThread()
    {
        if (Thread.CurrentThread.Name != _threadName)
            throw new InvalidOperationException($"This method must be called from the {_threadName} thread.");
    }

    private void AssertNotAdvanced()
    {
        if (_advancing)
            throw new InvalidOperationException("Advance is already in progress.");
    }
}
