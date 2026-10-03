using GameServer.Simulation;
using GameServer.Unit.Tests.Support;

namespace GameServer.Unit.Tests.Simulation;

using Simulation = GameServer.Simulation.Simulation;

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
    private readonly Simulation _simulation;

    public MovementTest()
    {
        _map = Map.FromRows(_mapRows);
        _world = new(_map);
        _simulation = new(_world, 1);
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
    public void GivenPlayerWhoseCooldownHasElapsed_WhenMovingAgain_ThenPlayerMoves()
    {
        // Arrange
        var playerId = _world.SpawnPlayer(new TilePosition(10, 10));

        // Act & Assert -- the first move succeeds and sets the cooldown
        Movement.TryMove(_world, playerId, Direction.North, currentTick: 0)
            .Should().Be(MoveResult.Success);

        // ... a move inside the window is rejected ...
        Movement.TryMove(_world, playerId, Direction.North, currentTick: Movement.MoveCooldownTicks / 2)
            .Should().Be(MoveResult.OnCooldown);

        // ... and a move at or past the cooldown succeeds again.
        Movement.TryMove(_world, playerId, Direction.North, currentTick: Movement.MoveCooldownTicks)
            .Should().Be(MoveResult.Success);

        _world.GetEntity(playerId)!.TilePosition.Should().Be(new TilePosition(10, 8));
    }

    [Fact]
    public void GivenNonExistentPlayer_WhenMoving_ThenReturnsUnknownEntity()
    {
        // Arrange && Act && Assert
        Movement.TryMove(_world, new EntityId(999), Direction.North, 0)
            .Should()
            .Be(MoveResult.UnknownEntity);
    }

    [Fact]
    public void GivenDeadPlayer_WhenMoving_ThenReturnsEntityDead()
    {
        // Arrange
        var deadPlayerId = _world.SpawnPlayer(new TilePosition(5, 5));
        var deadPlayer = _world.GetEntity(deadPlayerId);
        deadPlayer!.TakeDamage(deadPlayer.Health);

        // Act
        var result = Movement.TryMove(_world, deadPlayerId, Direction.North, 0);

        // Assert
        result.Should().Be(MoveResult.EntityDead);
        deadPlayer.TilePosition.Should().Be(new TilePosition(5, 5));
    }
}
