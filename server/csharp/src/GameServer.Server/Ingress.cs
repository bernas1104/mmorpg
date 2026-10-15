using System.Threading.Channels;

namespace GameServer.Server;

public sealed class Ingress
{
    private readonly Channel<ClientInput> _clientInputChannel;

    public Ingress()
    {
        _clientInputChannel = Channel.CreateBounded<ClientInput>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
            }
        );
    }

    public bool TryWrite(ClientInput input) => _clientInputChannel.Writer.TryWrite(input);

    public IReadOnlyList<ClientInput> Drain()
    {
        List<ClientInput> inputs = [];

        while (_clientInputChannel.Reader.TryRead(out var command))
            inputs.Add(command);

        return inputs;
    }
}
