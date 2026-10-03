using GameServer.Simulation;

using GameServer.Unit.Tests.Support;

namespace GameServer.Unit.Tests.Simulation;

public sealed class LifecycleTest
{
    private const int Seed = 12345;
    private const int TicksToTrace = 300;
    private static readonly TilePosition NeighbourSpawn = new(2, 2);
    private static readonly TilePosition OpenCorpseSpawn = new(2, 3);
    private static readonly TilePosition WandererSpawn = new(10, 10);
    private static readonly TilePosition FarCorpseSpawn = new(15, 15);

    private static readonly string[,] SealedPairRows =
    {
        { "#", "#", "#", "#", "#" },
        { "#", "#", ".", "#", "#" },
        { "#", "#", ".", "#", "#" },
        { "#", "#", "#", "#", "#" },
        { "#", "#", "#", "#", "#" },
    };

    private static readonly TilePosition SealedAttackerSpawn = new(2, 1);
    private static readonly TilePosition SealedTargetSpawn = new(2, 2);

    [Fact]
    public void GivenLethalDamage_WhenApplied_ThenEntityIsDeadOnTheSameTickAndStillPresent()
    {
        // Arrange
        var world = CreateSealedWorld();
        var simulation = new GameServer.Simulation.Simulation(world, Seed);
        var targetId = world.SpawnNpc(SealedTargetSpawn);
        var attackerId = world.SpawnPlayer(SealedAttackerSpawn);
        var target = world.GetEntity(targetId)!;

        // Act
        var tickOfDeath = -1L;

        for (var tick = 0; tick < AttackTickBudget && tickOfDeath < 0; tick++)
        {
            var stateBeforeTick = target.LifecycleState;

            simulation.Enqueue(new AttackCommand(attackerId, targetId));
            simulation.Tick();

            if (stateBeforeTick == LifecycleState.Alive && target.LifecycleState == LifecycleState.Dead)
                tickOfDeath = simulation.TickNumber - 1; // Tick() advances the counter at its end
        }

        // Assert
        tickOfDeath.Should().BeGreaterThanOrEqualTo(0, "the killing blow never landed");
        target.Health.Should().Be(0);
        target.LifecycleState.Should().Be(LifecycleState.Dead);
        world.GetEntity(targetId).Should().BeSameAs(target, "a corpse is still present, only removed later");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(97)]
    public void GivenDeadNpc_WhenQueriedInsideTheCorpseWindow_ThenItIsStillPresentAndDead(int ticksIntoWindow)
    {
        // Arrange
        var world = CreateSealedWorld();
        var simulation = new GameServer.Simulation.Simulation(world, Seed);
        var targetId = world.SpawnNpc(SealedTargetSpawn);
        var attackerId = world.SpawnPlayer(SealedAttackerSpawn);
        KillWithAttacks(world, simulation, attackerId, targetId);

        // Act
        for (var i = 0; i < ticksIntoWindow; i++) simulation.Tick();

        // Assert
        var corpse = world.GetEntity(targetId);
        corpse.Should().NotBeNull($"tick {ticksIntoWindow} is inside the corpse window");
        corpse.LifecycleState.Should().Be(LifecycleState.Dead);
        corpse.Health.Should().Be(0);
    }

    [Fact]
    public void GivenDeadNpc_WhenAttackedThroughTheCommandQueue_ThenAttackIsRejectedAndHealthUnchanged()
    {
        // Arrange
        var world = CreateSealedWorld();
        var test = new TestSimulation(world, Seed);
        var targetId = world.SpawnNpc(SealedTargetSpawn);
        var attackerId = world.SpawnPlayer(SealedAttackerSpawn);
        KillWithAttacks(world, test.Simulation, attackerId, targetId);

        // Act
        test.Simulation.Enqueue(new AttackCommand(attackerId, targetId));
        var log = test.Tick(1);

        // Assert
        world.GetEntity(targetId)!.Health.Should().Be(0);
        log.Should().Contain($"attack command failed for attacker {attackerId} on target {targetId}"
            + $" with result {AttackResult.InvalidDead}");
    }

    [Fact]
    public void GivenDeadNpc_WhenMovedThroughTheCommandQueue_ThenMoveIsRejectedAndPositionUnchanged()
    {
        // Arrange
        var world = CreateSealedWorld();
        var test = new TestSimulation(world, Seed);
        var targetId = world.SpawnNpc(SealedTargetSpawn);
        var attackerId = world.SpawnPlayer(SealedAttackerSpawn);
        KillWithAttacks(world, test.Simulation, attackerId, targetId);

        // Act
        test.Simulation.Enqueue(new MoveCommand(targetId, Direction.North));
        var log = test.Tick(1);

        // Assert
        world.GetEntity(targetId)!.TilePosition.Should().Be(SealedTargetSpawn);
        log.Should().Contain($"move command failed for entity {targetId} with result {MoveResult.InvalidDead}");
    }

    [Fact]
    public void GivenNeighbourOfACorpse_WhenItMoves_ThenItMayTakeTheCorpseTile()
    {
        // Arrange
        var world = CreateWorld();
        var simulation = new GameServer.Simulation.Simulation(world, Seed);
        var corpseId = world.SpawnNpc(OpenCorpseSpawn);
        var neighbourId = world.SpawnNpc(NeighbourSpawn);
        world.GetEntity(corpseId)!.TakeDamage(world.GetEntity(corpseId)!.Health);

        // Act
        var result = Movement.TryMove(world, neighbourId, Direction.South, simulation.TickNumber);

        // Assert
        result.Should().Be(MoveResult.Success);
        world.GetEntity(neighbourId)!.TilePosition.Should().Be(OpenCorpseSpawn);
    }

    [Fact]
    public void GivenNpcsKilledAtDifferentTicks_WhenTheirWindowsElapse_ThenEveryCorpseWindowIsTheSameLength()
    {
        // Arrange & Act
        var survivors = new List<int>();

        foreach (var ticksBeforeDeath in new[] { 0, 1, 5, 50, 100, 500 })
        {
            var world = CreateSealedWorld();
            var simulation = new GameServer.Simulation.Simulation(world, Seed);
            var targetId = world.SpawnNpc(SealedTargetSpawn);
            var attackerId = world.SpawnPlayer(SealedAttackerSpawn);
            for (var i = 0; i < ticksBeforeDeath; i++) simulation.Tick();
            KillWithAttacks(world, simulation, attackerId, targetId);

            var ticksSurvived = 0;
            while (world.GetEntity(targetId) is not null && ticksSurvived < Entity.DefaultRemovalTicks * 2)
            {
                simulation.Tick();
                ticksSurvived++;
            }

            world.GetEntity(targetId)
                .Should().BeNull($"the corpse killed {ticksBeforeDeath} ticks in never expired");
            survivors.Add(ticksSurvived);
        }

        // Assert
        survivors.Distinct().Should().Equal(
            [survivors[0]],
            "the corpse window must not depend on WHEN the entity died"
        );

        survivors[0].Should().BeInRange(
            (int)Entity.DefaultRemovalTicks - 1,
            (int)Entity.DefaultRemovalTicks,
            "the corpse window should last about DefaultRemovalTicks ticks"
        );
    }

    [Fact]
    public void GivenACorpse_WhenItIsDamagedAgain_ThenTheRemovalWindowIsNotExtended()
    {
        // Arrange
        var world = CreateSealedWorld();
        var simulation = new GameServer.Simulation.Simulation(world, Seed);
        var targetId = world.SpawnNpc(SealedTargetSpawn);
        var attackerId = world.SpawnPlayer(SealedAttackerSpawn);
        KillWithAttacks(world, simulation, attackerId, targetId);
        var corpse = world.GetEntity(targetId)!;
        var windowAtDeath = corpse.RemovalTick;

        // Act
        for (var i = 0; i < 40; i++) simulation.Tick();
        var windowBeforeSecondHit = corpse.RemovalTick;
        corpse.TakeDamage(1);
        var windowAfterSecondHit = corpse.RemovalTick;

        // Assert
        windowBeforeSecondHit.Should().Be(windowAtDeath - 40, "the window counts down while the corpse lies there");
        windowAfterSecondHit.Should().Be(
            windowBeforeSecondHit,
            "damage on an already-dead entity must not push the removal tick back out -- otherwise a "
            + "target that keeps being hit can be kept in the world indefinitely"
        );
    }

    [Fact]
    public void GivenRemovedNpc_WhenCommandsAreIssued_ThenTheEntityIsAbsentAndBothCommandsAreRejected()
    {
        // Arrange
        var world = CreateSealedWorld();
        var test = new TestSimulation(world, Seed);
        var targetId = world.SpawnNpc(SealedTargetSpawn);
        var attackerId = world.SpawnPlayer(SealedAttackerSpawn);
        KillWithAttacks(world, test.Simulation, attackerId, targetId);
        test.Tick((int)Entity.DefaultRemovalTicks);

        // Assert
        world.GetEntity(targetId).Should().BeNull();

        // Act
        test.Simulation.Enqueue(new MoveCommand(targetId, Direction.North));
        test.Simulation.Enqueue(new AttackCommand(attackerId, targetId));
        var log = test.Tick(1);

        // Assert
        log.Should().Contain($"move command failed for entity {targetId} with result {MoveResult.InvalidEntity}");
        log.Should().Contain($"attack command failed for attacker {attackerId} on target {targetId}"
            + $" with result {AttackResult.InvalidEntity}");
        world.GetEntity(attackerId)!.TilePosition.Should().Be(SealedAttackerSpawn);
    }

    [Fact]
    public void GivenRemovedNpc_WhenANewNpcIsSpawned_ThenItsIdIsNotReused()
    {
        // Arrange
        var world = CreateSealedWorld();
        var simulation = new GameServer.Simulation.Simulation(world, Seed);
        var firstId = world.SpawnNpc(SealedTargetSpawn);
        var attackerId = world.SpawnPlayer(SealedAttackerSpawn);
        KillWithAttacks(world, simulation, attackerId, firstId);
        for (var i = 0; i < (int)Entity.DefaultRemovalTicks; i++) simulation.Tick();

        // Act
        var secondId = world.SpawnNpc(SealedTargetSpawn);

        // Assert
        secondId.Value.Should().BeGreaterThan(firstId.Value);
        world.GetEntity(firstId).Should().BeNull("the retired id must not resolve to the new entity");
        world.GetEntity(secondId).Should().NotBeNull();
    }

    [Fact]
    public void GivenSeveralNpcsKilledOnTheSameTick_WhenTheWindowElapses_ThenAllOfThemAreRemoved()
    {
        // Arrange
        var world = CreateWorld();
        var simulation = new GameServer.Simulation.Simulation(world, Seed);
        var corpses = new List<EntityId>();
        for (var x = 1; x <= 6; x++)
        {
            var id = world.SpawnNpc(new TilePosition(x, 5));
            var entity = world.GetEntity(id)!;
            entity.TakeDamage(entity.Health);
            corpses.Add(id);
        }

        // Assert
        for (var i = 0; i < (int)Entity.DefaultRemovalTicks / 2; i++) simulation.Tick();
        foreach (var id in corpses)
            world.GetEntity(id).Should().NotBeNull($"corpse {id} vanished before its window ended");

        // Act
        for (var i = 0; i < (int)Entity.DefaultRemovalTicks; i++) simulation.Tick();

        // Assert
        foreach (var id in corpses)
            world.GetEntity(id).Should().BeNull($"corpse {id} outlived the corpse window");
    }

    [Fact]
    public void GivenACorpseOnTheMap_WhenAnotherNpcWanders_ThenThePathIsUnaffectedByTheCorpse()
    {
        // Arrange & Act
        var withoutCorpse = TraceWandererPath(withCorpse: false);
        var withCorpse = TraceWandererPath(withCorpse: true);

        // Assert
        withCorpse.Should().Equal(
            withoutCorpse,
            "a corpse must not consume draws from the simulation's single generator -- if it did, "
            + "how many bodies happened to be lying around would silently move every other npc"
        );
    }

    private static World CreateWorld() => new(Map.FromRows(TestMaps.GetWallBoundedTiles(20, 20)));

    private static World CreateSealedWorld() => new(Map.FromRows(SealedPairRows));

    private static int AttackTickBudget =>
        (Entity.DefaultMaxHealth / Combat.AttackDamage + 2) * (int)Combat.AttackCooldownTicks;

    private static List<TilePosition> TraceWandererPath(bool withCorpse)
    {
        var world = CreateWorld();
        var simulation = new GameServer.Simulation.Simulation(world, Seed);
        var npcId = world.SpawnNpc(WandererSpawn);

        if (withCorpse)
        {
            var corpseId = world.SpawnNpc(FarCorpseSpawn);
            var corpse = world.GetEntity(corpseId)!;
            corpse.TakeDamage(corpse.Health);
        }

        var npc = world.GetEntity(npcId)!;
        var path = new List<TilePosition>(TicksToTrace);

        for (var tick = 0; tick < TicksToTrace; tick++)
        {
            simulation.Tick();
            path.Add(npc.TilePosition);
        }

        return path;
    }

    private static long KillWithAttacks(
        World world,
        GameServer.Simulation.Simulation simulation,
        EntityId attackerId,
        EntityId targetId
    )
    {
        for (var tick = 0; tick < AttackTickBudget; tick++)
        {
            simulation.Enqueue(new AttackCommand(attackerId, targetId));
            simulation.Tick();

            if (world.GetEntity(targetId)?.LifecycleState == LifecycleState.Dead)
                return simulation.TickNumber - 1; // Tick() advances the counter at its end
        }

        throw new InvalidOperationException(
            $"target {targetId} was still alive after {AttackTickBudget} ticks of attacks"
        );
    }
}
