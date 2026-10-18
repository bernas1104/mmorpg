namespace GameServer.Unit.Tests.Server;

using GameServer.Server;
using GameServer.Sim;

public sealed class IngressTest
{
    private const int ProducerCount = 4;
    private const int CommandsPerProducer = 25;
    private const int IngressCapacity = 100;

    [Fact]
    public void GivenFakeClientProducers_WhenTheyWriteConcurrentlyAndTheSimThreadDrains_ThenEveryCommandDrainsExactlyOnceInProducerOrder()
    {
        var ingress = new Ingress();

        var producers = Enumerable
            .Range(0, ProducerCount)
            .Select(producer => new Thread(() =>
            {
                for (uint seq = 0; seq < CommandsPerProducer; seq++)
                    ingress.TryWrite(
                        new ClientCommandInput(
                            new MoveCommand(new EntityId((uint)producer * CommandsPerProducer + seq), Direction.East)
                        )
                    );
            }))
            .ToList();

        producers.ForEach(producer => producer.Start());
        producers.ForEach(producer => producer.Join());

        var drained = ingress.Drain().Cast<ClientCommandInput>().ToList();

        drained
            .Should()
            .HaveCount(ProducerCount * CommandsPerProducer, "every write finds room in the channel, so none may be lost");
        drained
            .Select(input => input.Command)
            .Cast<MoveCommand>()
            .Select(command => command.EntityId.Value)
            .Should()
            .OnlyHaveUniqueItems("each command must drain exactly once");

        for (var producer = 0; producer < ProducerCount; producer++)
        {
            drained
                .Select(input => input.Command)
                .Cast<MoveCommand>()
                .Select(command => command.EntityId.Value)
                .Where(id => id / CommandsPerProducer == (uint)producer)
                .Should()
                .Equal(
                    Enumerable.Range(0, CommandsPerProducer).Select(seq => (uint)producer * CommandsPerProducer + (uint)seq),
                    $"producer {producer}'s commands must drain in the order it wrote them"
                );
        }

        ingress.Drain().Should().BeEmpty("draining must consume everything, leaving nothing for the next tick");
    }

    [Fact]
    public void GivenAFullIngress_WhenAProducerTriesToWrite_ThenTheInputIsDroppedWithoutBlockingTheProducer()
    {
        var ingress = new Ingress();

        for (uint id = 0; id < IngressCapacity; id++)
            ingress.TryWrite(new ClientCommandInput(new MoveCommand(new EntityId(id), Direction.East))).Should().BeTrue();

        var dropped = new ClientCommandInput(new MoveCommand(new EntityId(uint.MaxValue), Direction.East));
        ingress.TryWrite(dropped).Should().BeTrue();

        var drained = ingress.Drain().Cast<ClientCommandInput>().ToList();

        drained.Should().HaveCount(IngressCapacity);
        drained.Should().NotContain(dropped);
    }
}
