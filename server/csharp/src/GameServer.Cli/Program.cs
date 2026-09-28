using System.Diagnostics;

using GameServer.Simulation;
using GameServer.Simulation.Constants;

var running = true;
var simulation = new Simulation(new World(new Map(512, 512)));

const int maxCatchUpTicks = 5;
long tickDurationTicks = Stopwatch.Frequency * SimulationConstants.TICK_RATE_MS / 1000;

long previous = Stopwatch.GetTimestamp();
long accumulator = 0;

while (running)
{
    long now = Stopwatch.GetTimestamp();
    accumulator += now - previous;
    previous = now;

    int ticksThisFrame = 0;
    while (accumulator >= tickDurationTicks && ticksThisFrame < maxCatchUpTicks)
    {
        simulation.Tick();
        accumulator -= tickDurationTicks;
        ticksThisFrame++;
    }

    // Too far behind: drop the backlog rather than spiral
    // Might wanna log a warning here
    if (accumulator >= tickDurationTicks)
        accumulator = 0;

    long remaining = tickDurationTicks - accumulator;
    int sleepMs = (int)(remaining * 1000 / Stopwatch.Frequency);

    if (sleepMs > 1) Thread.Sleep(sleepMs - 1);
}
