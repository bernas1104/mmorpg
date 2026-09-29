using System.Diagnostics;
using System.Runtime.InteropServices;

using GameServer.Simulation;
using GameServer.Simulation.Commands;
using GameServer.Simulation.Constants;

string[,] mapRows = new string[,]
{
    { ".", ".", ".", ".", "." },
    { ".", "#", "#", "#", "." },
    { ".", "#", ".", "#", "." },
    { ".", "#", "#", "#", "." },
    { ".", ".", ".", ".", "." }
};

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
using PosixSignalRegistration? sigTerm = OperatingSystem.IsWindows()
    ? null
    : PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => { ctx.Cancel = true; cts.Cancel(); });

var simulation = new Simulation(new World(Map.FromRows(mapRows)));

const int maxCatchUpTicks = 5;
long tickDurationTicks = Stopwatch.Frequency * SimulationConstants.TickDurationMs / 1000;

long previous = Stopwatch.GetTimestamp();
long accumulator = 0;
long nextTickDue = previous + tickDurationTicks;

void CommandParser(string? input)
{
    if (string.IsNullOrWhiteSpace(input)) return;

    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    switch (parts[0])
    {
        case "move":
            if (parts.Length != 3)
            {
                Console.WriteLine("Invalid move command. Usage: move <id> <direction>");
                return;
            }

            var parsedId = int.TryParse(parts[1], out var id);

            var parsedDirection = Enum.TryParse<Direction>(parts[2], true, out var direction);

            if (!parsedId || !parsedDirection)
            {
                Console.WriteLine("Invalid move command. Usage: move <id> <direction>");
                return;
            }

            simulation.Enqueue(new MoveCommand(new EntityId(id), direction));
            return;
        default:
            Console.WriteLine("Unknown command");
            return;
    }
}

// Which input source we are reading from, decided once. This is a different question
// from "is input ready?" and it has to be answered first: Console.KeyAvailable asks the
// console driver whether a keystroke is pending, and it THROWS when stdin is redirected,
// because a pipe is not a keyboard and the question has no meaning for it.
var inputIsConsole = !Console.IsInputRedirected;

// Latches true once the redirected stream is exhausted. Console.In.Peek() returns -1
// from then on forever, so without this the loop would call into the stream API 20x a
// second for the rest of the run and do nothing with the answer.
var streamEOF = false;

// Input is a producer, not part of the pacing loop: it must never be a place the loop
// can stall. The poll therefore runs before the clock is sampled below, so any time
// spent blocked here is attributed as ordinary elapsed time and absorbed by the existing
// catch-up guard rather than corrupting the pacing math.
//
// Never let input stop the world: an unexpected exception is logged and swallowed. This
// trades diagnosability for uptime, so the log line below is now the primary diagnostic
// when the poll misbehaves -- keep it specific. Keep this catch in the CLI driver and
// never in Sim/, where a catch-all would quietly swallow rule violations.
void PollInput()
{
    try
    {
        if (inputIsConsole)
        {
            // Known limitation (accepted): KeyAvailable is true as soon as the FIRST
            // character lands, so the ReadLine below blocks until Enter is pressed. The
            // tick clock stops while a line is half-typed. That is fine for a debug
            // console -- you are typing at it deliberately -- but it is a stall, not a
            // no-op, and it will show up later as a clamped catch-up burst.
            if (!Console.KeyAvailable) return;

            var input = Console.ReadLine();
            CommandParser(input);
        }
        else
        {
            if (streamEOF) return;

            // Known limitation (measured, not theoretical): Console.In.Peek() is a
            // BLOCKING probe, not a non-blocking one. It returns promptly only when a
            // byte is already buffered or the stream is already at EOF.
            //
            // Consequences to be aware of when reading the tick log:
            //   - With a paced producer this stalls the tick loop for as long as the
            //     producer is quiet (measured: >4s).
            //   - That stall becomes a catch-up burst, and the burst drains every command
            //     enqueued during the stall on ONE tick. Two commands piped with a delay
            //     between them are both processed on tick 0. This is correct for the
            //     simulation -- the queue is precisely the mechanism that makes batching
            //     safe -- but do not read it as a queue bug.
            //
            // Harmless for `echo ... | dotnet run`, because the data is buffered before
            // startup. If you ever script against a slow producer, delete this branch and
            // read stdin once at startup into a list, polled by index instead.
            if (Console.In.Peek() < 0)
            {
                streamEOF = true;
                return;
            }

            var input = Console.ReadLine();
            CommandParser(input);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error while polling input: {ex.Message}");
    }
}

Console.WriteLine($"Simulation started. Tick {simulation.TickNumber}");

while (!cts.Token.IsCancellationRequested)
{
    PollInput();

    long now = Stopwatch.GetTimestamp();
    accumulator += now - previous;
    previous = now;

    int ticksThisFrame = 0;
    while (accumulator >= tickDurationTicks && ticksThisFrame < maxCatchUpTicks)
    {
        simulation.Tick();
        Console.WriteLine($"Tick {simulation.TickNumber}");

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

Console.WriteLine($"Simulation stopped. Tick {simulation.TickNumber}");
