using GameServer.Simulation;
using GameServer.Unit.Tests.Support;

namespace GameServer.Unit.Tests.Simulation;

using Simulation = GameServer.Simulation.Simulation;

public sealed class CombatTest
{
    private readonly World _world;
    private readonly Simulation _simulation;

    public CombatTest()
    {
        _world = TestWorlds.Empty(5, 5);
        _simulation = new Simulation(_world, 12345);
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
    [InlineData(1, 1, EntityKind.Npc)]
    [InlineData(1, 2, EntityKind.Npc)]
    [InlineData(1, 3, EntityKind.Npc)]
    [InlineData(2, 1, EntityKind.Npc)]
    [InlineData(2, 3, EntityKind.Npc)]
    [InlineData(3, 1, EntityKind.Npc)]
    [InlineData(3, 2, EntityKind.Npc)]
    [InlineData(3, 3, EntityKind.Npc)]
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
            : _world.SpawnNpc(new TilePosition(targetX, targetY));

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
        result.Should().Be(AttackResult.EntityDead);
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
        result.Should().Be(AttackResult.EntityDead);
    }
}
