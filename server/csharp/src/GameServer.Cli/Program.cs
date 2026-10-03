using System.Diagnostics;
using System.Runtime.InteropServices;

using GameServer.Simulation;

string[,] mapRows = new string[,]
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

const int exitOk = 0;
const int exitUsageError = 2;

void PrintUsage()
{
    Console.WriteLine("Usage: dotnet run --project src/GameServer.Cli -- [--seed <n>]");
    Console.WriteLine("  --seed <n>     Seed the simulation RNG with <n> instead of fresh entropy.");
    Console.WriteLine("                 Any 32-bit integer, 0 included; the value used is always printed at startup.");
    Console.WriteLine("  -h, --help     Show this help and exit.");
    Console.WriteLine();
    Console.WriteLine("Commands are read from stdin, e.g.: echo \"move 1 North\"");
}

// Host arguments (argv) configure the run itself and are therefore resolved here, once,
// BEFORE the Simulation is constructed. Two deliberate non-invocations:
//
//   - argv is NOT routed through CommandParser. That parser is the in-band command
//     channel (stdin) and speaks the simulation's command language ("move <id> <dir>");
//     a process flag is a different language, and merging them means a typo'd flag comes
//     back as "Unknown command" from a parser that had no business seeing it.
//   - argv is NOT read through PollInput, which only ever looks at stdin. PollInput stays
//     exactly as it was: it is the tick-loop's input producer, nothing else.
//
// The ordering is the whole reason this is up here rather than at the top of PollInput:
// by the time PollInput first runs, `simulation` is already seeded, so a "--seed 1" that
// arrived in-band would be accepted and then change nothing -- the worst kind of bug,
// one that looks like it worked.
//
// Unknown arguments are an error, not a shrug: silently ignoring a misspelled --seed
// would hand back a run that is unreproducible while claiming it was seeded.
int? seedOverride = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--seed":
            if (i + 1 >= args.Length)
            {
                Console.Error.WriteLine("Error: --seed requires a value. Usage: --seed <n>");
                return exitUsageError;
            }

            var seedText = args[++i];

            if (!int.TryParse(seedText, out var parsedSeed))
            {
                Console.Error.WriteLine($"Error: --seed expects an integer, got '{seedText}'.");
                return exitUsageError;
            }

            // Any integer is a valid seed, 0 included: Rng remaps zero off the generator's
            // absorbing state rather than refusing to exist, so there is nothing to reject here
            // and no usage error to invent.
            seedOverride = parsedSeed;
            break;

        case "-h":
        case "--help":
            PrintUsage();
            return 0;

        default:
            Console.Error.WriteLine($"Error: unknown argument '{args[i]}'.");
            PrintUsage();
            return exitUsageError;
    }
}

// Entropy is read here and only here (see the milestone plan: the CLI mints a seed when
// the caller didn't supply one), and the startup line below records whichever value was
// used so the run can be replayed.
var seed = seedOverride ?? (int)DateTime.UtcNow.Ticks;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
using PosixSignalRegistration? sigTerm = OperatingSystem.IsWindows()
    ? null
    : PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => { ctx.Cancel = true; cts.Cancel(); });

var simulation = new Simulation(new World(Map.FromRows(mapRows)), seed, Console.Out);
simulation.World.SpawnPlayer(new TilePosition(4, 4));
simulation.World.SpawnPlayer(new TilePosition(5, 5));
simulation.World.SpawnPlayer(new TilePosition(2, 4));
simulation.World.SpawnNpc(new TilePosition(9, 9));
simulation.World.SpawnNpc(new TilePosition(10, 10));

var worldSnapshot = WorldSnapshot.Of(simulation.World);

List<CommandLogEntry> commandLog = [];

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

            var parsedDirection = Enum.TryParse<Direction>(parts[2], true, out var direction)
                && Enum.IsDefined(direction);

            if (!parsedId || !parsedDirection)
            {
                Console.WriteLine("Invalid move command. Usage: move <id> <direction>");
                return;
            }

            var moveCommand = new MoveCommand(new EntityId(id), direction);
            simulation.Enqueue(moveCommand);
            commandLog.Add(new CommandLogEntry(simulation.TickNumber, moveCommand));
            return;
        case "attack":
            if (parts.Length != 3)
            {
                Console.WriteLine("Invalid attack command. Usage: attack <attackerId> <targetId>");
                return;
            }

            var parsedAttackerId = int.TryParse(parts[1], out var attackerId);
            var parsedTargetId = int.TryParse(parts[2], out var targetId);

            if (!parsedAttackerId || !parsedTargetId)
            {
                Console.WriteLine("Invalid attack command. Usage: attack <attackerId> <targetId>");
                return;
            }

            var attackCommand = new AttackCommand(new EntityId(attackerId), new EntityId(targetId));
            simulation.Enqueue(attackCommand);
            commandLog.Add(new CommandLogEntry(simulation.TickNumber, attackCommand));
            return;
        case "snapshot":
        case "dump":
            var snapshot = WorldSnapshot.Of(simulation.World);
            Console.WriteLine($"--- Snapshot @ Tick {simulation.TickNumber} ---");
            Console.WriteLine($"Map: {snapshot.Map.Width}x{snapshot.Map.Height}, IdCounter: {snapshot.IdCounter}");
            Console.WriteLine($"Entities ({snapshot.Entities.Count()}):");
            foreach (var entity in snapshot.Entities.OrderBy(e => e.Id.Value))
            {
                Console.WriteLine(
                    $"  [{entity.Id.Value}] {entity.Kind} @ ({entity.TilePosition.X},{entity.TilePosition.Y}) | " +
                    $"HP: {entity.Health}/{entity.MaxHealth} | State: {entity.LifecycleState} | " +
                    $"NextMove: {entity.NextMoveAllowedTick} | NextAttack: {entity.NextAttackAllowedTick}" +
                    (entity.TicksUntilRemoval.HasValue ? $" | TicksUntilRemoval: {entity.TicksUntilRemoval.Value}" : string.Empty)
                );
            }

            Console.WriteLine("---");
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

Console.WriteLine($"Simulation started. Tick {simulation.TickNumber}, Seed {simulation.RngSeed}");

while (!cts.Token.IsCancellationRequested)
{
    PollInput();

    long now = Stopwatch.GetTimestamp();
    accumulator += now - previous;
    previous = now;

    int ticksThisFrame = 0;
    while (accumulator >= tickDurationTicks && ticksThisFrame < maxCatchUpTicks)
    {
        Console.WriteLine($"Tick {simulation.TickNumber}");
        simulation.Tick();

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

return exitOk;
