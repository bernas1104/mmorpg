using GameServer.Server;
using GameServer.Unit.Tests.Support;

namespace GameServer.Unit.Tests.Server;

public sealed class FixedTimestepLoopTest
{
    [Fact]
    public void GivenAFixedTimestepLoop_WhenItRuns_ThenItAdvancesPerDueTickAndSleepsTheRemainder()
    {
        var clock = new FakeTimeProvider();
        var scheduler = new TickScheduler(clock);
        using var loop = new FixedTimestepLoop(scheduler);
        using var cts = new CancellationTokenSource();
        var advances = 0;
        var sleeps = new List<int>();

        loop.Run(
            () =>
            {
                advances++;
                if (advances == 3) cts.Cancel();
            },
            ms =>
            {
                sleeps.Add(ms);
                clock.AdvanceMs(ms);
            },
            cts.Token
        );

        advances.Should().Be(3, "the first pass has no elapsed time, then each slept tick falls due");
        sleeps.Should().Equal(
            [TickScheduler.DefaultTickDurationMs, TickScheduler.DefaultTickDurationMs, TickScheduler.DefaultTickDurationMs, TickScheduler.DefaultTickDurationMs],
            "every pass must sleep a full tick rather than spin"
        );
    }

    [Fact]
    public void GivenAFixedTimestepLoop_WhenStarted_ThenItRunsOnItsOwnDedicatedThread()
    {
        var clock = new FakeTimeProvider();
        var scheduler = new TickScheduler(clock);
        using var loop = new FixedTimestepLoop(scheduler);
        string? observedThreadName = null;
        var advances = 0;
        using var passReached = new AutoResetEvent(false);
        using var gate = new AutoResetEvent(false);

        loop.Start(
            () => advances++,
            ms =>
            {
                observedThreadName ??= Thread.CurrentThread.Name;
                clock.AdvanceMs(ms);
                passReached.Set();
                gate.WaitOne();
            }
        );

        passReached.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue("the loop thread must reach its first sleep");
        observedThreadName.Should().Be(FixedTimestepLoop.ThreadName);

        gate.Set();
        passReached.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue("the second pass must fall due after one tick of accumulated time");
        advances.Should().Be(1);

        gate.Set();
        passReached.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue();
        advances.Should().Be(2, "each paced pass runs exactly one due tick");

        gate.Set();
        loop.Stop();
    }
}
