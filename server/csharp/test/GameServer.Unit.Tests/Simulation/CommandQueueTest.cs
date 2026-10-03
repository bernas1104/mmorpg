using GameServer.Simulation;

namespace GameServer.Unit.Tests.Simulation;

public sealed class CommandQueueTest
{
    [Fact]
    public void GivenACommand_WhenEnqueued_ThenItIsQueued()
    {
        // Arrange
        var queue = new CommandQueue();
        var moveCommand = new MoveCommand(new EntityId(0), Direction.North);

        // Act
        queue.Enqueue(moveCommand);

        // Assert
        var batch = queue.Drain();
        batch.Should().HaveCount(1);
        batch[0].Command.Should().Be(moveCommand);
    }

    [Fact]
    public void GivenQueuedCommands_WhenDrained_ThenAllAreReturnedAndTheQueueClears()
    {
        // Arrange
        var queue = new CommandQueue();
        var moveCommand1 = new MoveCommand(new EntityId(0), Direction.North);
        var moveCommand2 = new MoveCommand(new EntityId(1), Direction.South);
        queue.Enqueue(moveCommand1);
        queue.Enqueue(moveCommand2);

        // Act
        var batch = queue.Drain();

        // Assert
        batch.Should().HaveCount(2);
        batch[0].Command.Should().Be(moveCommand1);
        batch[1].Command.Should().Be(moveCommand2);
        queue.Drain().Should().BeEmpty();
    }
}
