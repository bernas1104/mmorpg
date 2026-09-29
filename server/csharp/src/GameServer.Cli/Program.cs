using System.Diagnostics;
using System.Runtime.InteropServices;

using GameServer.Simulation;
using GameServer.Simulation.Constants;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
using PosixSignalRegistration? sigTerm = OperatingSystem.IsWindows()
    ? null
    : PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => { ctx.Cancel = true; cts.Cancel(); });

var simulation = new Simulation(new World(Map.CreateEmpty(20, 20)));

const int maxCatchUpTicks = 5;
long tickDurationTicks = Stopwatch.Frequency * SimulationConstants.TICK_RATE_MS / 1000;

long previous = Stopwatch.GetTimestamp();
long accumulator = 0;
long nextTickDue = previous + tickDurationTicks;

Console.WriteLine($"Simulation started. Tick {simulation.TickCount}");

while (!cts.Token.IsCancellationRequested)
{
    long now = Stopwatch.GetTimestamp();
    accumulator += now - previous;
    previous = now;

    int ticksThisFrame = 0;
    while (accumulator >= tickDurationTicks && ticksThisFrame < maxCatchUpTicks)
    {
        simulation.Tick();
        Console.WriteLine($"Tick {simulation.TickCount}");

        accumulator -= tickDurationTicks;
        nextTickDue += tickDurationTicks;
        ticksThisFrame++;
    }

    // Too far behind: drop the backlog rather than spiral
    // Resync the deadline too, otherwise nextTickDue stays in the past and the loop spins hot.
    if (accumulator >= tickDurationTicks)
    {
        accumulator = 0;
        nextTickDue = now + tickDurationTicks;
    }

    // Invariant: when the backlog was not dropped, accumulator < tickDurationTicks implies
    // now < nextTickDue, so delayTicks is always positive here.
    long delayTicks = nextTickDue - now;
    int sleepMs = (int)(delayTicks * 1000 / Stopwatch.Frequency);

    if (sleepMs > 0) Thread.Sleep(sleepMs);
    else Thread.SpinWait(64);
}

Console.WriteLine($"Simulation stopped. Tick {simulation.TickCount}");
