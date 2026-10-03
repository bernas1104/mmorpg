using GameServer.Simulation;
using GameServer.Simulation.Commands;
using GameServer.Simulation.Enums;

namespace GameServer.Unit.Tests.Simulation;

public sealed class CombatTest
{
    private readonly string[,] _mapRows = new string[,]
    {
        { ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", "." }
    };

    private readonly Map _map;
    private readonly World _world;
    private readonly GameServer.Simulation.Simulation _simulation;

    public CombatTest()
    {
        _map = Map.FromRows(_mapRows);
        _world = new World(_map);
        _simulation = new GameServer.Simulation.Simulation(_world, 12345);
    }

    [Fact]
    public void GivenAttackerAndTarget_WhenAttackerIsSameAsTarget_ThenAttackFails()
    {
        // Arrange
        var attackerId = _world.SpawnPlayer(new TilePosition(2, 2));
        var targetId = attackerId;

        var attackCommand = new AttackCommand(attackerId, targetId);

        _simulation.Enqueue(attackCommand);

        // Act
        _simulation.Tick();

        // Assert
        var target = _world.GetEntity(targetId)!;
        target.Health.Should().Be(target.MaxHealth);
    }

    [Theory]
    [InlineData(1, 1, EntityKind.Player)]
    [InlineData(1, 2, EntityKind.Player)]
    [InlineData(1, 3, EntityKind.Player)]
    [InlineData(2, 1, EntityKind.Player)]
    [InlineData(2, 3, EntityKind.Player)]
    [InlineData(3, 1, EntityKind.Player)]
    [InlineData(3, 2, EntityKind.Player)]
    [InlineData(3, 3, EntityKind.Player)]
    [InlineData(1, 1, EntityKind.NPC)]
    [InlineData(1, 2, EntityKind.NPC)]
    [InlineData(1, 3, EntityKind.NPC)]
    [InlineData(2, 1, EntityKind.NPC)]
    [InlineData(2, 3, EntityKind.NPC)]
    [InlineData(3, 1, EntityKind.NPC)]
    [InlineData(3, 2, EntityKind.NPC)]
    [InlineData(3, 3, EntityKind.NPC)]
    public void GivenAttackerAndTarget_WhenInRange_ThenAttackSucceeds(
        int targetX,
        int targetY,
        EntityKind targetKind
    )
    {
        // Arrange
        var attackerId = _world.SpawnPlayer(new TilePosition(2, 2));
        var targetId = targetKind == EntityKind.Player
            ? _world.SpawnPlayer(new TilePosition(targetX, targetY))
            : _world.SpawnNPC(new TilePosition(targetX, targetY));

        var attackCommand = new AttackCommand(attackerId, targetId);

        _simulation.Enqueue(attackCommand);

        // Act
        _simulation.Tick();

        // Assert
        var target = _world.GetEntity(targetId)!;
        target.Health.Should().Be(target.MaxHealth - Combat.AttackDamage);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 2)]
    [InlineData(0, 3)]
    [InlineData(0, 4)]
    [InlineData(1, 0)]
    [InlineData(1, 4)]
    [InlineData(2, 0)]
    [InlineData(2, 4)]
    [InlineData(3, 0)]
    [InlineData(3, 4)]
    [InlineData(4, 0)]
    [InlineData(4, 1)]
    [InlineData(4, 2)]
    [InlineData(4, 3)]
    [InlineData(4, 4)]
    public void GivenAttackerAndTarget_WhenOutOfRange_ThenAttackFails(
        int targetX,
        int targetY
    )
    {
        // Arrange
        var attackerId = _world.SpawnPlayer(new TilePosition(2, 2));
        var targetId = _world.SpawnPlayer(new TilePosition(targetX, targetY));

        var attackCommand = new AttackCommand(attackerId, targetId);

        _simulation.Enqueue(attackCommand);

        // Act
        _simulation.Tick();

        // Assert
        var target = _world.GetEntity(targetId)!;
        target.Health.Should().Be(target.MaxHealth);
    }

    [Fact]
    public void GivenAttackerAndTarget_WhenAttackerIsExhausted_ThenAttackFails()
    {
        // Arrange
        var attackerId = _world.SpawnPlayer(new TilePosition(2, 2));
        var targetId = _world.SpawnPlayer(new TilePosition(2, 3));

        var attackCommand = new AttackCommand(attackerId, targetId);

        // Act
        _simulation.Enqueue(attackCommand);
        _simulation.Tick();

        // Attempt to attack again immediately, attacker should be exhausted
        _simulation.Enqueue(attackCommand);
        _simulation.Tick();

        // Assert
        var target = _world.GetEntity(targetId)!;
        target.Health.Should().Be(target.MaxHealth - Combat.AttackDamage); // Only the first attack should have succeeded
    }

    [Fact]
    public void GivenAttackerAndTarget_WhenTargetHealthGoesBelowZero_ThenHealthIsClamped()
    {
        // Arrange
        var attackerId = _world.SpawnPlayer(new TilePosition(2, 2));
        var targetId = _world.SpawnPlayer(new TilePosition(2, 3));

        var attackCommand = new AttackCommand(attackerId, targetId);

        // Act
        for (int i = 0; i < Combat.AttackCooldownTicks * 11; i++) // Attack multiple times to ensure target health goes below zero
        {
            if (i % Combat.AttackCooldownTicks == 0)
                _simulation.Enqueue(attackCommand);

            _simulation.Tick();
        }

        // Assert
        var target = _world.GetEntity(targetId)!;
        target.Health.Should().Be(0); // Health should be clamped at zero
    }

    [Fact]
    public void GivenDeadAttacker_WhenAttacking_ThenAttackFails()
    {
        // Arrange
        var deadAttackerId = _world.SpawnPlayer(new TilePosition(3, 3));
        var deadAttacker = _world.GetEntity(deadAttackerId);
        deadAttacker!.TakeDamage(deadAttacker.Health);

        var targetId = _world.SpawnPlayer(new TilePosition(3, 4));

        // Act
        var result = Combat.TryAttack(_world, deadAttackerId, targetId, 0);

        // Assert
        result.Should().Be(AttackResult.InvalidDead);
    }

    [Fact]
    public void GivenDeadTarget_WhenAttacked_ThenAttackFails()
    {
        // Arrange
        var attackerId = _world.SpawnPlayer(new TilePosition(3, 3));
        var targetId = _world.SpawnPlayer(new TilePosition(3, 4));
        var target = _world.GetEntity(targetId);
        target!.TakeDamage(target.Health);

        // Act
        var result = Combat.TryAttack(_world, attackerId, targetId, 0);

        // Assert
        result.Should().Be(AttackResult.InvalidDead);
    }
}
