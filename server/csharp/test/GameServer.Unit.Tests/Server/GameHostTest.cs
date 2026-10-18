using System.Text;

using GameServer.Server;
using GameServer.Sim;
using GameServer.Unit.Tests.Support;

namespace GameServer.Unit.Tests.Server;

using Simulation = Sim.Simulation;

public sealed class GameHostTest
{
    private const int Seed = 12345;
    private const int TicksToRun = 200;
    private static readonly EntityId PlayerId = new(1);
    private static readonly EntityId NpcId = new(2);

    [Fact]
    public void GivenACommandPushedBeforeAdvance_WhenTheHostAdvances_ThenItIsAppliedInThatTick()
    {
        var world = TestWorlds.Open();
        world.SpawnPlayer(PlayerId, new TilePosition(5, 5));
        var (host, ingress, simulation, log) = CreateHost(world);

        ingress.TryWrite(new ClientCommandInput(new MoveCommand(PlayerId, Direction.East)));

        OnSimThread(host.Advance);

        log.Lines.Should().Contain($"tick 0: received MoveCommand(EntityId: {PlayerId}, Direction: East)");
        world.GetEntity(PlayerId)!.TilePosition.Should().Be(new TilePosition(6, 5));
        simulation.TickNumber.Should().Be(1);
    }

    [Fact]
    public void GivenACommandPushedDuringTick_WhenTheHostAdvances_ThenItIsAppliedOneTickLater()
    {
        var world = TestWorlds.Open();
        world.SpawnPlayer(PlayerId, new TilePosition(5, 5));
        var ingress = new Ingress();
        var log = new TriggerWriter(line =>
        {
            if (line.Contains($"received MoveCommand(EntityId: {PlayerId}, Direction: East)"))
                ingress.TryWrite(new ClientCommandInput(new MoveCommand(PlayerId, Direction.East)));
        });
        var simulation = new Simulation(world, Seed, log);
        var host = new GameHost(ingress, simulation, FixedTimestepLoop.ThreadName);

        ingress.TryWrite(new ClientCommandInput(new MoveCommand(PlayerId, Direction.East)));

        OnSimThread(host.Advance);

        world.GetEntity(PlayerId)!.TilePosition.Should().Be(new TilePosition(6, 5));
        log.Lines.Where(line => line.StartsWith("tick 1:")).Should().BeEmpty();

        OnSimThread(host.Advance);

        log.Lines.Should().Contain($"tick 1: received MoveCommand(EntityId: {PlayerId}, Direction: East)");
        log.Lines.Count(line => line.Contains("received")).Should().Be(2, "the pushed command waits a full tick, then joins the next batch");
    }

    [Fact]
    public void GivenAPlayerCommandAdmittedByAdvance_WhenAnNpcCommandIsRaisedInTheSameTick_ThenThePlayerCommandIsProcessedFirst()
    {
        var world = TestWorlds.SingleCorridor();
        world.SpawnPlayer(PlayerId, new TilePosition(2, 1));
        world.SpawnNpc(NpcId, new TilePosition(2, 2));
        var (host, ingress, _, log) = CreateHost(world);

        for (var i = 0; i < TicksToRun; i++)
        {
            ingress.TryWrite(new ClientCommandInput(new MoveCommand(PlayerId, Direction.North)));
            OnSimThread(host.Advance);
        }

        var contestedTicks = log.Lines
            .Where(line => line.Contains("received"))
            .GroupBy(line => line.Split(':')[0])
            .Where(tick => tick.Any(line => line.Contains($"EntityId: {PlayerId},"))
                && tick.Any(line => line.Contains($"EntityId: {NpcId},")))
            .ToList();

        contestedTicks
            .Should()
            .NotBeEmpty("the npc must have been admitted in the same tick as the player for the comparison to be meaningful");

        foreach (var (_, lines) in contestedTicks.Select(tick => (tick.Key, tick.ToList())))
        {
            lines.FindIndex(line => line.Contains($"EntityId: {PlayerId},"))
                .Should()
                .BeLessThan(
                    lines.FindIndex(line => line.Contains($"EntityId: {NpcId},")),
                    "both commands share the movement phase, so processing order is sequence order, and the host admitted the player's command before the think pass raised the npc's"
                );
        }
    }

    [Fact]
    public void GivenAJoinRequest_WhenTheHostAdvances_ThenThePlayerIsSpawned()
    {
        var world = TestWorlds.Open();
        var (host, ingress, _, log) = CreateHost(world);

        ingress.TryWrite(new ClientRequest(new Request(RequestType.Join, PlayerId, new TilePosition(5, 5))));

        OnSimThread(host.Advance);

        world.GetEntity(PlayerId).Should().NotBeNull();
        world.GetEntity(PlayerId)!.Kind.Should().Be(EntityKind.Player);
        world.GetEntity(PlayerId)!.TilePosition.Should().Be(new TilePosition(5, 5));
        log.Lines.Should().Contain(line => line.Contains("host event processed") && line.Contains("PlayerSpawned"));
    }

    [Fact]
    public void GivenALeaveRequest_WhenTheHostAdvances_ThenThePlayerIsDespawned()
    {
        var world = TestWorlds.Open();
        world.SpawnPlayer(PlayerId, new TilePosition(5, 5));
        var (host, ingress, _, _) = CreateHost(world);

        ingress.TryWrite(new ClientRequest(new Request(RequestType.Leave, PlayerId, null)));

        OnSimThread(host.Advance);

        world.GetEntity(PlayerId).Should().BeNull();
    }

    [Fact]
    public void GivenTheHostOwnedByTheSimThread_WhenAnotherThreadCallsAdvance_ThenItFailsLoudly()
    {
        var world = TestWorlds.Open();
        world.SpawnPlayer(PlayerId, new TilePosition(5, 5));
        var (host, _, simulation, _) = CreateHost(world);

        Exception? failure = null;
        var intruder = new Thread(() =>
        {
            try { host.Advance(); }
            catch (Exception e) { failure = e; }
        });
        intruder.Start();
        intruder.Join();

        failure.Should().BeOfType<InvalidOperationException>();
        failure!.Message.Should().Contain(FixedTimestepLoop.ThreadName);
        simulation.TickNumber.Should().Be(0, "the guard fires before any simulation work happens");
        world.GetEntity(PlayerId)!.TilePosition.Should().Be(new TilePosition(5, 5));
    }

    [Fact]
    public void GivenAnAdvanceAlreadyInProgress_WhenAdvanceReenters_ThenItFailsLoudly()
    {
        var world = TestWorlds.Open();
        world.SpawnPlayer(PlayerId, new TilePosition(5, 5));
        var ingress = new Ingress();
        GameHost? host = null;
        var log = new TriggerWriter(line =>
        {
            if (line.Contains("received"))
                host!.Advance();
        });
        var simulation = new Simulation(world, Seed, log);
        host = new GameHost(ingress, simulation, FixedTimestepLoop.ThreadName);

        ingress.TryWrite(new ClientCommandInput(new MoveCommand(PlayerId, Direction.East)));

        var failure = OnSimThread(host.Advance);

        failure.Should().BeOfType<InvalidOperationException>();
        failure!.Message.Should().Contain("already in progress");
        simulation.TickNumber.Should().Be(0, "the failed tick must not have advanced the simulation");
        world.GetEntity(PlayerId)!.TilePosition.Should().Be(new TilePosition(5, 5));
    }

    private static (GameHost Host, Ingress Ingress, Simulation Simulation, TriggerWriter Log) CreateHost(World world)
    {
        var log = new TriggerWriter();
        var simulation = new Simulation(world, Seed, log);
        var ingress = new Ingress();
        var host = new GameHost(ingress, simulation, FixedTimestepLoop.ThreadName);

        return (host, ingress, simulation, log);
    }

    private static Exception? OnSimThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception e) { failure = e; }
        })
        { Name = FixedTimestepLoop.ThreadName };
        thread.Start();
        thread.Join();

        return failure;
    }

    private sealed class TriggerWriter(Action<string>? onLine = null) : TextWriter
    {
        private readonly List<string> _lines = [];

        public IReadOnlyList<string> Lines => _lines;

        public override Encoding Encoding => Encoding.Unicode;

        public override void WriteLine(string? value)
        {
            _lines.Add(value ?? string.Empty);
            onLine?.Invoke(value ?? string.Empty);
        }
    }
}
