using GameServer.Server;
using GameServer.Unit.Tests.Support;

namespace GameServer.Unit.Tests.Server;

public sealed class TickSchedulerTest
{
    private const int TickDurationMs = TickScheduler.DefaultTickDurationMs;

    [Fact]
    public void GivenNoElapsedTime_WhenAdvanceRuns_ThenNoTickIsDueAndTheLoopSleepsAFullTick()
    {
        var scheduler = new TickScheduler(new FakeTimeProvider());

        var schedule = scheduler.Advance();

        schedule.Ticks.Should().Be(0);
        schedule.SleepMs.Should().Be(TickDurationMs);
    }

    [Fact]
    public void GivenLessThanOneTickOfElapsedTime_WhenAdvanceRuns_ThenTimeAccumulatesTowardTheNextTick()
    {
        var clock = new FakeTimeProvider();
        var scheduler = new TickScheduler(clock);

        clock.AdvanceMs(TickDurationMs / 2);
        var partial = scheduler.Advance();

        partial.Ticks.Should().Be(0);

        clock.AdvanceMs(TickDurationMs / 2);
        var full = scheduler.Advance();

        full.Ticks.Should().Be(1);
    }

    [Fact]
    public void GivenOneTickOfElapsedTime_WhenAdvanceRuns_ThenExactlyOneTickIsDue()
    {
        var clock = new FakeTimeProvider();
        var scheduler = new TickScheduler(clock);

        clock.AdvanceMs(TickDurationMs);

        scheduler.Advance().Ticks.Should().Be(1);
    }

    [Fact]
    public void GivenABacklogLargerThanTheCatchUpCap_WhenAdvanceRuns_ThenAtMostMaxCatchUpTicksAreDue()
    {
        var clock = new FakeTimeProvider();
        var scheduler = new TickScheduler(clock);

        clock.AdvanceMs(TickDurationMs * 10);

        scheduler.Advance().Ticks.Should().Be(TickScheduler.MaxCatchUpTicks);
    }

    [Fact]
    public void GivenADroppedBacklog_WhenAdvanceRunsAgainWithoutTimePassing_ThenNoTicksAreDueAndTheLoopSleeps()
    {
        var clock = new FakeTimeProvider();
        var scheduler = new TickScheduler(clock);
        clock.AdvanceMs(TickDurationMs * 100);
        scheduler.Advance();

        var schedule = scheduler.Advance();

        schedule.Ticks.Should().Be(0, "the excess backlog was dropped, so the loop must not hot-loop through it");
        schedule.SleepMs.Should().Be(TickDurationMs);
    }
}
