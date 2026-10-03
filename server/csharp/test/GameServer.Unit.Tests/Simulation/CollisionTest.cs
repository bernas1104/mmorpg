using GameServer.Simulation;
using GameServer.Unit.Tests.Support;

namespace GameServer.Unit.Tests.Simulation;

using Simulation = GameServer.Simulation.Simulation;

public sealed class CollisionTest
{
    private readonly World _world;
    private readonly Simulation _simulation;

    public CollisionTest()
    {
        _world = TestWorlds.Open();
        _simulation = new Simulation(_world, 1);
    }

    [Fact]
    public void GivenOccupiedTarget_WhenMoving_ThenTheMoveIsRejectedAndTheEntityDoesNotMove()
    {
        // Arrange
        var playerId = _world.SpawnPlayer(new TilePosition(5, 5));
        _world.SpawnPlayer(new TilePosition(5, 4));

        // Act
        var result = Movement.TryMove(_world, playerId, Direction.North, 0);

        // Assert
        result.Should().Be(MoveResult.TargetOccupied);

        _world.GetEntity(playerId)!.TilePosition.Should().Be(new TilePosition(5, 5));
    }

    [Fact]
    public void GivenTwoPlayersTargetingTheSameTile_WhenBothMoveDirectly_ThenTheLoserIsRejectedBecauseTheTargetIsOccupied()
    {
        // Arrange
        var winnerId = _world.SpawnPlayer(new TilePosition(5, 4));
        var loserId = _world.SpawnPlayer(new TilePosition(5, 6));

        // Act
        var winnerResult = Movement.TryMove(_world, winnerId, Direction.South, 0);
        var loserResult = Movement.TryMove(_world, loserId, Direction.North, 0);

        // Assert
        winnerResult.Should().Be(MoveResult.Success);
        loserResult.Should().Be(MoveResult.TargetOccupied);

        _world.GetEntity(winnerId)!.TilePosition.Should().Be(new TilePosition(5, 5));
        _world.GetEntity(loserId)!.TilePosition.Should().Be(new TilePosition(5, 6));
    }

    [Fact]
    public void GivenTwoPlayersTargetingTheSameTile_WhenBothMoveInOneTick_ThenFirstEnqueuedWinsRegardlessOfEntityId()
    {
        // Arrange
        var secondEnqueuedId = _world.SpawnPlayer(new TilePosition(5, 4));
        var firstEnqueuedId = _world.SpawnPlayer(new TilePosition(5, 6));

        // Guard: fail loudly rather than silently stop testing id-vs-arrival order if a
        // future edit to this fixture reverses the spawns.
        secondEnqueuedId.Value.Should().BeLessThan(firstEnqueuedId.Value);

        _simulation.Enqueue(new MoveCommand(firstEnqueuedId, Direction.North));
        _simulation.Enqueue(new MoveCommand(secondEnqueuedId, Direction.South));

        // Act
        _simulation.Tick();

        // Assert
        _world.GetEntity(firstEnqueuedId)!.TilePosition.Should().Be(new TilePosition(5, 5));
        _world.GetEntity(secondEnqueuedId)!.TilePosition.Should().Be(new TilePosition(5, 4));
    }

    [Fact]
    public void GivenPlayerAndNpcContestingTheSameTile_WhenTheTickIsApplied_ThenThePlayerAlwaysWins()
    {
        const int seeds = 100;

        var contestedTicks = 0;

        for (int seed = 0; seed < seeds; seed++)
        {
            // Arrange
            var world = TestWorlds.SingleCorridor();
            var npcId = world.SpawnNpc(new TilePosition(2, 2));
            var playerId = world.SpawnPlayer(new TilePosition(1, 1));
            var test = new TestSimulation(world, seed);

            // Act
            test.Simulation.Enqueue(new MoveCommand(playerId, Direction.East));
            var log = test.Tick(1);

            var npcMoved = log.Contains($"received MoveCommand(EntityId: {npcId}");
            if (!npcMoved) continue;

            // Assert
            contestedTicks++;

            log.Should().Contain(
                $"move command failed for entity {npcId} with result {MoveResult.TargetOccupied}",
                "the player holds the tile, so the npc's identical claim is refused by the shared "
                + "collision rule rather than by anything npc-specific"
            );

            world.GetEntity(playerId)!.TilePosition.Should().Be(new TilePosition(2, 1));
            world.GetEntity(npcId)!.TilePosition.Should().Be(new TilePosition(2, 2));
        }

        contestedTicks.Should().BeGreaterThan(
            0,
            "no seed produced a contested tick, so the tie-break rule was never actually exercised"
        );
    }
}
