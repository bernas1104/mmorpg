using AwesomeAssertions;

using GameServer.Simulation;

namespace GameServer.Unit.Tests.Simulation;

public sealed class ReplayTest
{
    private static World CreateEmptyWorld(int width = 20, int height = 20)
    {
        var mapRows = new string[height, width];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                mapRows[y, x] = ".";
            }
        }

        return new World(Map.FromRows(mapRows));
    }

    private static World CloneWorld(World source)
    {
        var map = source.Map;
        var mapRows = new string[map.Height, map.Width];
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                mapRows[y, x] = map.IsWalkable(new TilePosition(x, y)) ? "." : "#";
            }
        }

        var clone = new World(Map.FromRows(mapRows));
        foreach (var entity in source.GetAll())
        {
            if (entity.Kind == EntityKind.Player)
            {
                clone.SpawnPlayer(entity.TilePosition);
            }
            else if (entity.Kind == EntityKind.Npc)
            {
                clone.SpawnNpc(entity.TilePosition);
            }
        }

        return clone;
    }

    private static void AssertWorldsEqual(
        WorldSnapshot expected,
        WorldSnapshot actual,
        long expectedTick,
        long actualTick
    )
    {
        actualTick.Should().Be(expectedTick);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Replay_WanderScenarioWithFixedSeed_ProducesIdenticalFinalState()
    {
        const int seed = 12345;
        const int ticksToRun = 100;

        var initialWorld = CreateEmptyWorld();
        initialWorld.SpawnPlayer(new TilePosition(10, 10));
        initialWorld.SpawnNpc(new TilePosition(5, 5));

        var liveWorld = CloneWorld(initialWorld);
        var liveSim = new GameServer.Simulation.Simulation(liveWorld, seed);
        var log = new List<CommandLogEntry>();

        for (var tick = 0; tick < ticksToRun; tick++)
        {
            liveSim.Tick();
        }

        var replayWorld = WorldSnapshot.Of(initialWorld);
        var replay = new Replay(
            new RecordedRun(
                liveSim.TickNumber,
                seed,
                replayWorld,
                log
            )
        );

        var liveSimWorldSnapshot = WorldSnapshot.Of(liveSim.World);
        var finalReplayWorldSnapshot = replay.ReplayRecordedRun();

        AssertWorldsEqual(liveSimWorldSnapshot, finalReplayWorldSnapshot, liveSim.TickNumber, replay.Run.LastTick);
    }

    [Fact]
    public void Replay_CombatScenario_ProducesIdenticalFinalState()
    {
        const int seed = 42;
        const int ticksToRun = 60;

        var initialWorld = CreateEmptyWorld();
        var attacker = initialWorld.SpawnPlayer(new TilePosition(5, 5));
        var target = initialWorld.SpawnNpc(new TilePosition(6, 5));

        var liveWorld = CloneWorld(initialWorld);
        var liveSim = new GameServer.Simulation.Simulation(liveWorld, seed);
        var log = new List<CommandLogEntry>();

        for (var tick = 0; tick < ticksToRun; tick++)
        {
            var currentTick = liveSim.TickNumber;

            if (tick % 3 == 0 && tick < 30)
            {
                var attackCmd = new AttackCommand(attacker, target);
                log.Add(new CommandLogEntry(currentTick, attackCmd));
                liveSim.Enqueue(attackCmd);
            }



            liveSim.Tick();
        }

        var replayWorld = WorldSnapshot.Of(initialWorld);
        var replay = new Replay(
            new RecordedRun(
                liveSim.TickNumber,
                seed,
                replayWorld,
                log
            )
        );

        var liveSimWorldSnapshot = WorldSnapshot.Of(liveSim.World);
        var finalReplayWorldSnapshot = replay.ReplayRecordedRun();

        AssertWorldsEqual(liveSimWorldSnapshot, finalReplayWorldSnapshot, liveSim.TickNumber, replay.Run.LastTick);
    }

    [Fact]
    public void Replay_FuzzTestWithRandomValidCommands_ProducesIdenticalFinalState()
    {
        const int seed = 999;
        const int ticksToRun = 50;
        const int commandsPerTick = 3;

        var initialWorld = CreateEmptyWorld();
        initialWorld.SpawnPlayer(new TilePosition(1, 1));
        initialWorld.SpawnPlayer(new TilePosition(18, 1));
        initialWorld.SpawnNpc(new TilePosition(1, 18));
        initialWorld.SpawnNpc(new TilePosition(18, 18));
        initialWorld.SpawnNpc(new TilePosition(10, 10));

        var liveWorld = CloneWorld(initialWorld);
        var liveSim = new GameServer.Simulation.Simulation(liveWorld, seed);
        var log = new List<CommandLogEntry>();
        var rng = new Random(seed);

        var directions = Enum.GetValues<Direction>();

        for (var tick = 0; tick < ticksToRun; tick++)
        {
            var currentTick = liveSim.TickNumber;
            var entities = liveSim.World.GetAll().ToList();

            for (var c = 0; c < commandsPerTick && entities.Count > 0; c++)
            {
                var entity = entities[rng.Next(entities.Count)];

                if (entity.LifecycleState != LifecycleState.Alive)
                {
                    continue;
                }

                var commandType = rng.Next(2);
                if (commandType == 0)
                {
                    var dir = directions[rng.Next(directions.Length)];
                    var moveCmd = new MoveCommand(entity.Id, dir);
                    log.Add(new CommandLogEntry(currentTick, moveCmd));
                    liveSim.Enqueue(moveCmd);
                }
                else
                {
                    var potentialTargets = entities
                        .Where(e => e.Id != entity.Id && e.LifecycleState == LifecycleState.Alive)
                        .ToList();

                    if (potentialTargets.Count > 0)
                    {
                        var target = potentialTargets[rng.Next(potentialTargets.Count)];
                        var attackCmd = new AttackCommand(entity.Id, target.Id);
                        log.Add(new CommandLogEntry(currentTick, attackCmd));
                        liveSim.Enqueue(attackCmd);
                    }
                }
            }

            liveSim.Tick();
        }

        var replayWorld = WorldSnapshot.Of(initialWorld);
        var replay = new Replay(
            new RecordedRun(
                liveSim.TickNumber,
                seed,
                replayWorld,
                log
            )
        );

        var liveSimWorldSnapshot = WorldSnapshot.Of(liveSim.World);
        var finalReplayWorldSnapshot = replay.ReplayRecordedRun();

        AssertWorldsEqual(liveSimWorldSnapshot, finalReplayWorldSnapshot, liveSim.TickNumber, replay.Run.LastTick);
    }
}
