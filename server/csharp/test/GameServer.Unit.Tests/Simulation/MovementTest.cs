using GameServer.Simulation;
using GameServer.Simulation.Commands;
using GameServer.Simulation.Enums;
using GameServer.Unit.Tests.Collections;

namespace GameServer.Unit.Tests.Simulation;

[Collection(ConsoleOutputCollection.Name)]
public sealed class MovementTest
{
    private readonly string[,] _mapRows = new string[,]
    {
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", "#", "#", "#", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", "#", ".", "#", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", "#", "#", "#", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
    };

    private readonly Map _map;
    private readonly World _world;
    private readonly GameServer.Simulation.Simulation _simulation;

    public MovementTest()
    {
        _map = Map.FromRows(_mapRows);
        _world = new(_map);
        _simulation = new(_world);
    }

    [Fact]
    public void GivenPlayerAtStartPosition_WhenMovingNorth_ThenPlayerMovesToExpectedPosition()
    {
        // Arrange
        var playerId = _world.SpawnPlayer(new TilePosition(10, 10));
        var moveCommand = new MoveCommand(playerId, Direction.North);

        _simulation.Enqueue(moveCommand);

        var expectedPosition = new TilePosition(10, 9);

        // Act
        _simulation.Tick();

        // Assert
        var player = _world.GetEntity(playerId);
        player.Should().NotBeNull();
        player.TilePosition.Should().Be(expectedPosition);
    }

    [Theory]
    [InlineData(Direction.North)]
    [InlineData(Direction.South)]
    [InlineData(Direction.East)]
    [InlineData(Direction.West)]
    public void GivenPlayerAtStartPosition_WhenMovingIntoNonWalkableTile_ThenPlayerDoesNotMove(Direction direction)
    {
        // Arrange
        var playerId = _world.SpawnPlayer(new TilePosition(2, 2));
        var moveCommand = new MoveCommand(playerId, direction);

        _simulation.Enqueue(moveCommand);

        var expectedPosition = new TilePosition(2, 2);

        // Act
        _simulation.Tick();

        // Assert
        var player = _world.GetEntity(playerId);
        player.Should().NotBeNull();
        player.TilePosition.Should().Be(expectedPosition);
    }

    [Fact]
    public void GivenPlayerAtStartPosition_WhenMovingOutOfBounds_ThenPlayerDoesNotMove()
    {
        // Arrange
        var playerId = _world.SpawnPlayer(new TilePosition(0, 0));
        var moveCommand = new MoveCommand(playerId, Direction.North);

        _simulation.Enqueue(moveCommand);

        var expectedPosition = new TilePosition(0, 0);

        // Act
        _simulation.Tick();

        // Assert
        var player = _world.GetEntity(playerId);
        player.Should().NotBeNull();
        player.TilePosition.Should().Be(expectedPosition);
    }

    [Fact]
    public void GivenPlayerAtStartPosition_WhenMovingWithinCooldown_ThenPlayerDoesNotMove()
    {
        // Arrange
        var playerId = _world.SpawnPlayer(new TilePosition(10, 10));
        var moveCommand = new MoveCommand(playerId, Direction.North);

        _simulation.Enqueue(moveCommand);
        _simulation.Tick();

        var expectedPosition = new TilePosition(10, 9);

        // Act
        _simulation.Enqueue(moveCommand);
        _simulation.Tick();

        // Assert
        var player = _world.GetEntity(playerId);
        player.Should().NotBeNull();
        player.TilePosition.Should().Be(expectedPosition);
    }

    [Fact]
    public void GivenNonExistentPlayer_WhenMoving_ThenReturnsInvalidEntity()
    {
        // Arrange && Act && Assert
        Movement.TryMove(_world, new EntityId(999), Direction.North, 0)
            .Should()
            .Be(MoveResult.InvalidEntity);
    }

    [Fact]
    public void GivenTargetTileOccupied_WhenMoving_ThenReturnsTileOccupiedAndDoesNotMove()
    {
        // Arrange
        var playerId = _world.SpawnPlayer(new TilePosition(5, 5));
        _world.SpawnPlayer(new TilePosition(5, 4));

        // Act
        var result = Movement.TryMove(_world, playerId, Direction.North, 0);

        // Assert
        result.Should().Be(MoveResult.TileOccupied);

        var player = _world.GetEntity(playerId);
        player.Should().NotBeNull();
        player.TilePosition.Should().Be(new TilePosition(5, 5));
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
        var winner = _world.GetEntity(firstEnqueuedId);
        winner.Should().NotBeNull();
        winner.TilePosition.Should().Be(new TilePosition(5, 5));

        var loser = _world.GetEntity(secondEnqueuedId);
        loser.Should().NotBeNull();
        loser.TilePosition.Should().Be(new TilePosition(5, 4));
    }

    [Fact]
    public void GivenTwoPlayersTargetingTheSameTile_WhenBothMoveDirectly_ThenLoserIsRejectedAsTileOccupied()
    {
        // Arrange
        var winnerId = _world.SpawnPlayer(new TilePosition(5, 4));
        var loserId = _world.SpawnPlayer(new TilePosition(5, 6));

        // Act
        var winnerResult = Movement.TryMove(_world, winnerId, Direction.South, 0);
        var loserResult = Movement.TryMove(_world, loserId, Direction.North, 0);

        // Assert
        winnerResult.Should().Be(MoveResult.Success);
        loserResult.Should().Be(MoveResult.TileOccupied);

        _world.GetEntity(winnerId)!.TilePosition.Should().Be(new TilePosition(5, 5));
        _world.GetEntity(loserId)!.TilePosition.Should().Be(new TilePosition(5, 6));
    }
}
