# Distributed Rendering in the Console: Porting a TUI Engine State Machine to gRPC and Redis

[English](article.md) | [Русский](article.ru.md)

## Introduction

While working on a warehouse management system (WMS), I encountered the specifics of text-based terminal interfaces (Terminal UIs). In such systems, the data exchange logic is built using the Backend-Driven UI (BDUI) paradigm, but in its most extreme, text-based form. Instead of transmitting a component tree or HTML markup, the server handles all the graphical work and returns a ready-made text matrix of symbols and colors to the client.

### Visual Example of a Form

Within the warehouse process, the form on the screen sequentially requested data from the employee, switching focus between input widgets or displaying a selection menu. A typical goods receipt form screen looked something like this:
```
+------------------------------------+
|             RECEIVING              |  <- TextWidget (header, usually inverted)
|                                    |
| PALLET: PO-10294                   |  <- TextWidget
| ITEM CODE: 2000192834017........   |  <- TextWidget + TextEntryWidget
| TARGET ZONE: A-10-02............   |  <- TextWidget + TextEntryWidget
|                                    |
| CHOOSE CONDITION:                  |  <- TextWidget (multiline)
| 1. GOOD CONDITION                  |
| 2. DAMAGED / DEFECT                |
| OPTION: 1..                        |  <- TextWidget + TextEntryWidget/ComboEntryWidget
|                                    |
| ITEM QUANTITY: .....               |  <- TextWidget + TextEntryWidget (focus is here)
|                                    |
|                                    |
| SCAN ITEM QUANTITY                 |  <- TextWidget (multiline hint for the active TextEntryWidget)
| ENTER - SKIP    ESC - BACK         |
+------------------------------------+
```

Although the engine was originally designed for a text-based interface, end users (warehouse workers) rarely interacted with the system directly via Telnet clients:

* **Developers and testers** used a direct Telnet connection via the console for quick debugging, business logic verification, and manual interface testing.
* **Line personnel (warehouse workers)** worked with industrial data collection terminals (DCTs) from Zebra, Honeywell, and other brands. A custom mobile app written in Xamarin was installed on the DCTs running Android.

This Xamarin app connected to the server, received text on the screen, and rendered it on the mobile display identically to the console view. At the same time, the mobile client enriched the text interface with platform-specific features unavailable in standard Telnet:
1. **Sound notifications:** Playing different sounds for successful barcode scanning and for validation errors.
2. **Color indicators:** Highlighting critical errors or warnings in bright colors to attract attention.
3. **HTML page display**: Often used for product pages.

### Why Backend-Driven UI (BDUI)?

The Backend-Driven UI (BDUI) concept is firmly entrenched in web and mobile development, where the client receives component metadata from the server and transforms it into an interface. However, when applying this paradigm to text-based terminal UIs, the standard approach of passing a widget tree becomes meaningless. To keep the client as lightweight and "dumb" as possible, the server should handle all the graphical work and return a ready-made text matrix of symbols and colors.

The automation of large warehouse complexes (WMS) still relies on a stack that can cause culture shock for a modern web developer. The logic for interaction between the operator and the system is often built through standard Telnet consoles running on industrial data collection terminals (DCTs), such as premium Zebras or budget Chinese Urovos.

In this architecture, the DCT doesn't contain a single line of business logic. It operates as a "dumb client," sending a network request to the server for literally every sneeze—every physical key press by the user. The server accepts this input, loops through events, recalculates the interface state, and sends back a strictly deterministic byte array over the network: the characters and colors that need to be changed on the screen.

Using a text matrix instead of HTML pages or mobile apps has a strict pragmatics:
* **Ignoring the geometry of the device zoo**: Screen matrices are rigidly fixed to standard coordinates (e.g., 40x12 or 80x25). This allows the server to abstract itself from the physical dimensions, resolutions, and aspect ratios of displays from different Zebra and Urovo product lines. The interface is guaranteed to look consistent and predictable on any device.
* **Centralized Deployment**: Changing validation steps or screen logic does not require rolling out updates through mobile device management (MDM) systems. With a distributed fleet of mobile terminals across the network, there is always a risk that some terminals will not be updated, will be stuck on an interim version, or will fail during the update process. With Telnet architecture, all changes are deployed exclusively to the server, and operators instantly see the new logic without touching the terminals themselves.

### `PixelTerminalUI` Engine

*Note: `PixelTerminalUI` is a completely independent, open-source project developed in my spare time. It was built from scratch, is not a replica of any commercial systems, and contains no code or confidential information belonging to my previous employer.*

The engine described in this article is a server-side text matrix renderer. Its key feature is its abstraction over the session storage layer via the `ITerminalSessionRepository` interface. This allows for the deployment of both local in-memory configurations and fully-fledged distributed storage systems based on Redis, PostgreSQL, or MongoDB.

The primary goal of this Proof of Concept (PoC) is to design a distributed state machine capable of processing each individual user input on any independent backend server instance, without maintaining persistent state in RAM.

## Chapter 1. Limitations of Legacy Architectures and the Deadlock of the Stateful Model

When designing terminal management systems, the traditional approach relies on the Stateful model. Session state is either tightly bound to a persistent TCP connection or stored within the server's `ConcurrentDictionary`.

Below is an implementation fragment illustrating how the brute-force object-oriented design creates critical limitations for scaling:
```csharp
public abstract class TerminalScreen
{
    public string Name { get; set; } = nameof(TerminalScreen);
    public int Height { get; set; }
    public int Width { get; set; }
    public SessionInfo? SessionInfo { get; set; }
    
    // Direct reference to parent screen (keeping the object graph on the heap)
    public TerminalScreen? ParentScreen { get; set; } 
    
    public List<TextWidget> Widgets { get; set; } = [];
    public TextEntryWidget? FocusedEntryWidget { get; set; }
    
    // Synchronous logic and navigation transition delegates
    public Func<bool>? ShowValidation { get; set; }
    public Action? ShowMainMenu { get; set; }

    // A transition method that causes deep nesting of the Call Stack
    public void ShowScreen(TerminalScreen screen)
    {
        try
        {
            SessionInfo.CurrentScreen = screen;
            screen.SessionInfo = SessionInfo;
            screen.ParentScreen = this; // Link references (creates a two-way connection / closure)
            screen.Init();
            screen.Show(); // Thread enters the nested method and blocks here
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }
}
```

This model has three inherent architectural flaws:
- **Object graph retention**: The `ParentScreen = this` property forces the runtime to store the user's entire navigation history on the heap. This memory is not freed while the session is active.
- **Thread blocking in a nested Call Stack**: Calling `screen.Show()` within `ShowScreen` causes the execution flow to fall deep into the screen hierarchy. The thread is physically blocked by the server, waiting for the next input from a specific terminal.
- **Zero scaling**: Since state and execution flow are tied to a specific machine, horizontal scaling of the backend (Scale Out) becomes impossible. An instance crash means immediate data loss for all sessions connected to it.

To overcome these limitations, it was necessary to completely rewrite the state management logic, break the synchronous Call Stack, and move the context of the steps to a distributed storage layer. We'll discuss how this is implemented using a command state machine, Redis, and a high-performance binary gRPC pipeline below.

## Chapter 2. Distributed Architecture and Command State Machines

When it comes to Backend-Driven UI (BDUI), mobile development immediately comes to mind: the server returns a component tree (JSON/Protobuf), and the iOS or Android client parses and renders this tree. But when we descend to the level of text terminals, this model breaks down. Passing widget metadata means forcing the thin client to decide how to arrange, color, and invert them. To preserve the "dumb" terminal paradigm, the server should return not a component tree, but a **ready-made frame pixel array or its delta**.

The main problem with classic (stateful) terminal engines is maintaining the session in the memory of a specific server. The execution thread is blocked waiting for user input, and the complete screen object graphs and command histories reside in the heap, permanently tying the user to a single backend instance.

To make the system distributed and eliminate the memory affinity (In-Memory State) of a specific server, we needed to solve three problems:
- Completely unload the screen state from server memory after each response.
- Teach the system to process the first user input on Server 1 and the next click on Server 2 without losing context.
- Eliminate memory allocations in hot execution paths of the state machine to avoid throttling the garbage collector (GC).

### Anatomy of a Distributed Command and Moving Away from a Rigid Call Stack

To break up the Call Stack and free server threads from waiting for input, the logic of multi-step interfaces was migrated to asynchronous commands controlled by a numeric state token (`RawState`).

Here's what the basic framework of widget and command contracts looks like:
```csharp
public record TextWidget
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Value { get; set; }
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Visible { get; set; }
    public bool Inverted { get; set; }
    public virtual bool Editable { get; set; } = false;
    public int? TabIndex { get; set; }
    public ConsoleColor Foreground { get; set; } = ConsoleColor.White;
    public ConsoleColor Background { get; set; } = ConsoleColor.Black;
}

public record TextEntryWidget : TextWidget
{
    public override bool Editable { get; set; } = true;
    public bool Required { get; set; } = true;
    public char EmptyEnterSymbol { get; set; } = '.';
    public string? Hint { get; set; }
    public CommandBase? Command { get; set; }
}

public abstract record TerminalScreen
{
    public required Guid Id { get; set; }
    public required Guid SessionId { get; set; }
    public required string Name { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public IEnumerable<TextWidget> Widgets { get; set; } = [];
    public Guid? FocusedEntryWidgetId { get; set; }
    public bool EnableDoubleBuffering { get; init; } = false;
}

public interface ICommand
{
    Guid Id { get; }
    Guid WidgetId { get; set; }
    int RawState { get; set; }
    ValueTask<bool> ExecuteAsync(ICommandContext context);
}
```

When the user submits input, the backend doesn't keep the running method in memory. The pipeline works declaratively:
- A lightweight hashset (Redis Hash) is retrieved from Redis using the `SessionId` key. This hashset contains only the raw state data and the current numeric ID of the command step.
- The screen factory assembles the `TerminalScreen` object from scratch.
- The `int` retrieved from Redis is passed to the `RawState` property of the command, instantly restoring the logical context.

### Enumeration Optimization: Combating Boxing with `Unsafe.As`

Writing a state machine using raw ints in business code is a guaranteed way to get confused by indexes and shoot yourself in the foot. For code readability, steps must be strongly typed enumerations (`enum`).

However, in C#, casting a generic enumeration type to an integer (and vice versa) in generic classes using standard methods like `(int)(object)State` or `Convert.ToInt32(State)` results in value boxing. The runtime is forced to allocate memory on the heap to wrap the `enum` in an object. In a high-intensity input loop, when hundreds of users are simultaneously typing on the terminal, these micro-allocations instantly fill the memory with garbage and cause micro-freezes due to the Garbage Collector.

To overcome this runtime limitation without sacrificing performance, we used hard bitwise copying of references through raw memory with forced method inlining:
```csharp
public abstract class CommandBase : ICommand
{
    public abstract Guid Id { get; }
    public abstract Guid WidgetId { get; set; }
    public abstract int RawState { get; set; }
    public abstract ValueTask<bool> ExecuteAsync(ICommandContext context);
}

public abstract class Command<TEnum> : CommandBase where TEnum : struct, Enum
{
    public abstract TEnum State { get; set; }

    public override int RawState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            TEnum localState = State;
            return Unsafe.As<TEnum, int>(ref localState);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            int localValue = value;
            State = Unsafe.As<int, TEnum>(ref localValue);
        }
    }
}
```

How it works under the hood: The `Unsafe.As` instruction takes a reference to a memory location containing a `TEnum` value type and forces the JIT compiler to interpret these bits directly as `int` (and vice versa). Thanks to `MethodImplOptions.AggressiveInlining`, this call is completely absorbed into the calling code, turning into a direct processor instruction for working with registers.

### Implementing a multi-step scenario in action

Let's see how this architecture unfolds using the example of a Sequence command for input processing. The `ExecuteAsync` method executes atomically and returns immediately, without blocking the server thread:
```csharp
public enum MultiStepState
{
    Initial = 0,
    Processing = 1,
    Finalizing = 2
}

public sealed class SequenceExecutionCommand : Command<MultiStepState>
{
    public override MultiStepState State { get; set; } = MultiStepState.Initial;
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }

    public override ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        if (context == null) return ValueTask.FromResult(false);

        switch (State)
        {
            case MultiStepState.Initial:
                // Phase 1: Business Validation of Primary Input
                if (string.Equals(context.InputValue, "ABORT", StringComparison.OrdinalIgnoreCase))
                {
                    context.ErrorMessage = "Operation interrupted.";
                    return ValueTask.FromResult(false);
                }

                // We move the team to the next step and exit immediately.
                State = MultiStepState.Processing;
                return ValueTask.FromResult(true);

            case MultiStepState.Processing:
                // Phase 2: Deep Validation of the Transaction Key
                if (context.InputValue.Length < 3)
                {
                    context.ErrorMessage = "The token is too short.";
                    return ValueTask.FromResult(false);
                }

                State = MultiStepState.Finalizing;
                return ValueTask.FromResult(true);

            case MultiStepState.Finalizing:
                // Phase 3: Finalize and clean up the navigation context
                return ValueTask.FromResult(true);

            default:
                return ValueTask.FromResult(false);
        }
    }
}
```

From a runtime perspective, the transition looks like an O(1) jump through the enumeration index table. The command calculated the step, switched the state in memory, completed execution, and the updated `int` was returned to the Redis Hash protected by optimistic session version locking.

> **Problem synchronizing backend and state versions**
> If, during the execution of a multi-step scenario (for example, with 5 steps in step 3), a new backend version is deployed where the `enum MultiStepState` has changed (steps have been added or removed), the old `int` from Redis, when deserialized via `Unsafe.As`, will either result in an invalid business logic state or an application crash. Protection against on-the-fly state schema migration is completely ignored in the article.

#### A Look into the Future: Syntactic Sugar vs. Runtime Performance

From the end developer's perspective, declarative Fluent chains in the Workflow Core style (e.g., `.StartWith<Step1>().Next<Step2>().Then<Step3>()`) would be much more elegant than cumbersome switch-case blocks within a single command class.

However, it's important to understand the cost of such abstraction in a distributed system. Under the hood of any "beautiful" Fluent API, there inevitably resides a state machine that jumps between state tokens. For ultra-low-latency systems (where every nanosecond matters), the classic switch provides the fastest possible enumeration table navigation in O(1) time without constructing intermediate chains of objects in memory.

The ideal development vector here seems to be the intersection of Fluent descriptions with Source Generators (code generation): when the developer describes a business process in beautiful Fluent code, the compiler unfolds it into the same flat, aggressively inlined switch case at build time, preserving both code purity and Zero Allocation at runtime.

### State Machine Relationship with Frame Rendering and Error Handling

This approach imposes strict rules on adjacent system layers — rendering and error handling:

1. **Hybrid Delta Calculation (Double Buffering)**:

The business logic of commands is completely isolated from the transport layer. When a command successfully changes a step and returns `true`, control passes to the shared `HandleInputAsync` pipeline. If the `EnableDoubleBuffering = true` option is enabled in the configuration, the engine deploys an optimization algorithm: it retrieves the previous (historical) frame snapshot from Redis, compares it with the newly rendered screen in a loop, extracts the point delta of mutations, saves the new matrix in the cache, and sends a compact binary gRPC delta packet to the client.

2. **Server-Driven UI Paradigm for Errors**:

The network contract does not have REST-like JSON responses with error codes. If the validation step within a command fails (returns `false` and writes a string to `context.ErrorMessage`), the engine does not throw exceptions. It creates a new error screen record on the fly, draws this text directly in the pixel matrix on the server side, and this modified frame is sent to the gRPC channel as usual. The client remains completely "dumb" - it simply obediently displays on the physical console screen what the backend sends.

## Chapter 3. Battle for Bytes: Pooling and Bitmasks

Providing Externalized Session State on the backend inevitably increases the load on the infrastructure layers. If the engine allocates memory for new data arrays, parses heavy text structures, and sends them over the network for every user input, the system will become saturated with allocations (GC Pressure), and frame times will go far beyond the target 30 milliseconds.

To minimize overhead in the hot rendering and network marshalling paths, three optimization mechanisms were implemented:
- Bitwise compression of character and color metadata into a single `uint` primitive.
- Reuse of flat render buffers via an object pool (`ArrayPool<T>`).
- Protecting the network contract from the peculiarities of Protobuf binary stream deserialization.

### Compact Byte Layout: Frame Packing via Bitmasks

Transmitting an array of `Pixel` objects (even a `readonly record struct`) over the network is inefficient in terms of bandwidth. The pixel data (`char`, `Foreground`/`Background` colors, and inversion flags) was packed into a single 32-bit integer (`uint`) using bitmasks and shifts with forced inlining of methods:
```csharp
public static class PixelBitPacker
{
    private const uint CharMask = 0xFFFF;
    private const uint ColorMask = 0x7F;
    private const uint FlagsMask = 0x03;
    
    private const int CharShift = 16;
    private const int ForegroundShift = 9;
    private const int BackgroundShift = 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Pack(char character, byte foreground, byte background, byte flags)
    {
        return (uint)character << CharShift
               | (foreground & ColorMask) << ForegroundShift
               | (background & ColorMask) << BackgroundShift
               | flags & FlagsMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Unpack(uint packed, out char character, out byte foreground, out byte background, out byte flags)
    {
        character = (char)(packed >> CharShift & CharMask);
        foreground = (byte)(packed >> ForegroundShift & ColorMask);
        background = (byte)(packed >> BackgroundShift & ColorMask);
        flags = (byte)(packed & FlagsMask);
    }
}
```

Since the JIT compiler injects all `Pack`/`Unpack` logic directly into the calling loops, the packing operation is performed at the processor register level. The entire graphics frame is converted into a flat, lightweight array of `uint[]` primitives.

#### Performance Microbenchmark Results (`BenchmarkDotNet`)

The comparison was between the serialization of high-level pixel models (Legacy) and the serialization of a packed flat `uint[]` array (Optimized):

| Method                 | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ProcessLegacyBuffer    | 1.957 μs | 0.0119 μs | 0.0100 μs |  1.00 | 2.7618 |   5.65 KB |        1.00 |
| ProcessOptimizedBuffer | 1.807 μs | 0.0036 μs | 0.0030 μs |  0.92 | 0.9289 |    1.9 KB |        0.34 |

##### Analysis of results:
* **Memory allocations (Allocated):** Replacing the object array with a primitive array reduced memory allocations in the managed heap by **3x** (from 5.65 KB to 1.9 KB). This significantly reduces the frequency of garbage collector (GC) runs in high-load scenarios.
* **Computational speed (Mean):** Buffer processing and preparation speed increased by **8%** due to the elimination of allocation overhead and the execution of packing operations directly in processor registers. The main performance gain at this stage is achieved by reducing the load on the serializer and minimizing network packet size.

### Optimizing hot rendering paths via `ArrayPool`

In the traditional interface rendering model, each new frame generates a fresh array of objects in the heap. To prevent constant load on the garbage collector, the new engine renderer has been switched to symmetrical leasing of buffers from the shared `ArrayPool<Pixel>.Shared` pool.

Memory allocation occurs only once — for the final flattened array of compressed data, which is sent to the gRPC channel:
```csharp
Pixel[] pooledBuffer = ArrayPool<Pixel>.Shared.Rent(totalCellsCount);
uint[] currentFlatBuffer = new uint[totalCellsCount];

try
{
    renderer.Draw(screen, pooledBuffer);
    
    for (int index = 0; index < totalCellsCount; index++)
    {
        Pixel currentPixel = pooledBuffer[index];
        byte inversionFlag = (byte)(currentPixel.IsInverted ? 1 : 0);

        currentFlatBuffer[index] = PixelBitPacker.Pack(
            currentPixel.Symbol,
            (byte)currentPixel.Foreground,
            (byte)currentPixel.Background,
            inversionFlag);
    }
}
finally
{
    ArrayPool<Pixel>.Shared.Return(pooledBuffer);
}
```

A specific feature of `ArrayPool` is that the `Rent` method can return an array whose actual length exceeds the requested `totalCellsCount`. To prevent buffer overflows and "dirty" tails from previous rendering operations from appearing in the frame, the rendering logic within `renderer.Draw` is strictly abstracted from the physical size of the array. The engine operates exclusively within the logical `Width` and `Height` boundaries of the current screen:
```csharp
int width = screen.Width;
int height = screen.Height;

// Initialize the screen with default empty space, using fill pixels specified by the flat array offset formula.
for (int y = 0; y < height; y++)
{
    for (int x = 0; x < width; x++)
    {
        buffer[y * width + x] = new Pixel(' ', false, ConsoleColor.White);
    }
}
```

#### Performance benchmark results (`BenchmarkDotNet`)

Measurements were conducted for standard mobile computer screen resolutions (`40x12`, `40x25`, `80x12`, and `80x25`) in the `Release` configuration:

| Method                             | Width | Height | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|----------------------------------- |------ |------- |----------:|----------:|----------:|------:|--------:|-----:|--------:|----------:|------------:|
| NewRenderWithArrayPoolAndFlatArray | 40    | 12     |  1.146 μs | 0.0024 μs | 0.0021 μs |  0.27 |    0.00 |    1 |  0.9518 |   1.95 KB |        0.26 |
| OldRenderWithTwoDimensionalArray   | 40    | 12     |  4.180 μs | 0.0583 μs | 0.0517 μs |  1.00 |    0.02 |    2 |  3.7155 |   7.61 KB |        1.00 |
|                                    |       |        |           |           |           |       |         |      |         |           |             |
| NewRenderWithArrayPoolAndFlatArray | 40    | 25     |  2.337 μs | 0.0167 μs | 0.0148 μs |  0.27 |    0.00 |    1 |  1.9455 |   3.98 KB |        0.25 |
| OldRenderWithTwoDimensionalArray   | 40    | 25     |  8.648 μs | 0.1519 μs | 0.1269 μs |  1.00 |    0.02 |    2 |  7.6904 |  15.73 KB |        1.00 |
|                                    |       |        |           |           |           |       |         |      |         |           |             |
| NewRenderWithArrayPoolAndFlatArray | 80    | 12     |  2.249 μs | 0.0101 μs | 0.0090 μs |  0.28 |    0.00 |    1 |  1.8654 |   3.82 KB |        0.25 |
| OldRenderWithTwoDimensionalArray   | 80    | 12     |  8.105 μs | 0.0408 μs | 0.0362 μs |  1.00 |    0.01 |    2 |  7.3700 |  15.11 KB |        1.00 |
|                                    |       |        |           |           |           |       |         |      |         |           |             |
| NewRenderWithArrayPoolAndFlatArray | 80    | 25     |  4.666 μs | 0.0095 μs | 0.0084 μs |  0.28 |    0.00 |    1 |  3.8452 |   7.88 KB |        0.25 |
| OldRenderWithTwoDimensionalArray   | 80    | 25     | 16.948 μs | 0.0432 μs | 0.0383 μs |  1.00 |    0.00 |    2 | 15.2588 |  31.36 KB |        1.00 |

##### Result Analysis:
* **Memory Allocations (Allocated):** On the maximum `80x25` grid, heap allocations decreased from 31.36 KB to 7.88 KB (exactly 4x). The rendering middleware now minimizes allocations: the only remaining heap allocation is the resulting `uint[]` transport array, which is required by the gRPC serializer and cannot be returned to the pool until sent over the network. The remaining 7.88 KB is the unavoidable allocation of the resulting `uint[]` transport array (`80 * 25 * 4 bytes ≈ 8 KB`), which is required by the serializer and physically cannot be returned to the pool until sent over the network.
* **Mean Computational Speed:** The new approach is consistently **3.6 times faster** (Ratio `0.27 - 0.28`). This CPU speedup is achieved by eliminating allocation cycles and using the *Bounds Checking Elimination* optimization in the JIT compiler. When traversing a one-dimensional `pooledBuffer[index]`, the runtime completely disables hidden array bounds checks.

### Double Buffering: Hybrid delta calculation via Redis

Rendering strategy management has been moved to the IoC container configuration level when registering engine dependencies in `Program.cs`. The developer can flexibly switch the network traffic optimization mode using the `EnableDoubleBuffering` flag:

```csharp
builder.Services.AddPixelTerminalUI(options =>
{
    options.EnableDoubleBuffering = true;
    options.DoubleBufferingThreshold = 0.3;
});
```

When the `EnableDoubleBuffering` option is enabled, the server doesn't send the full pixel array to the client for each change. Instead, it calculates a per-pixel difference (delta).

Since we use an **Externalized Session State** architecture, the server instance itself doesn't store frames in RAM. The historical buffer of the previous screen is retrieved from the distributed `Redis Hash` storage for each request. The server calculates the delta, sends it to the client, and the client locally applies these changes to its matrix.

#### Network Overhead Mathematics

Let's visually calculate the network overhead mathematics in the Protobuf binary protocol (gRPC) using a 40x12 screen (480 cells in total):

*   **Option A: Transmit the entire screen (`FullFrameResponse`)**
    In the Protobuf contract, a full frame is described as a repeating field of primitives `repeated uint32 screen_buffer = 1;`. At the byte stream level, Protobuf doesn't waste memory on delimiters. Each `uint32` number is encoded using a variable-length varint (occupying 1 to 5 bytes depending on the value). For our packed pixels, this averages out to 4 bytes per cell + 1-2 bytes of the field's service tag for the entire array.
    *The final "bare" frame size:* 480 cells × 4 bytes ≈ **1.9 KB**. This weight is fixed and does not depend on the interface dynamics.

*   **Option B: Transmit only point changes (`DeltaResponse`)**
    Each mutation is described as a message consisting of two fields: `int32 index = 1;` and `uint32 packed_value = 2;`. Protobuf overhead is minimal here: 1 byte for the index tag + ~2 bytes for the index itself (Varint) + 1 byte for the value tag + ~4 bytes for the packed pixel. The length of the embedded message (1-2 bytes) is added on top.
    *The final size of one mutation:* only about **9-10 bytes**.

Now let's compare the raw numbers without compression: if we divide the total weight of a full screen (1920 bytes) by the weight of a single binary mutation (10 bytes), we get a raw breaking point of **192 cells**. Thus, if **more than 40% of the pixels** on the screen change, a single binary delta packet mathematically weighs more than sending the entire matrix.

However, gRPC runs on top of HTTP/2, which uses header compression (HPACK) and optional body compression (GZIP/Brotli) at the transport level by default. How does this affect our 40%?

- **A full frame** (`screen_buffer`) often contains long, continuous strings of identical numbers—empty spaces of the standard background color. Network archivers collapse such duplicates with colossal efficiency, compressing 1.9 KB down to almost a couple hundred bytes.
- **In a delta packet**, index values ​​(e.g., `162`, `163`, `164`) are sequential, but they are unique, making compression algorithms less efficient than with a monotonic array of spaces.

Considering real-world transport compression of binary streams, the economic benefit of a point delta begins to diminish around **30% of changes**.

This pragmatic threshold of **0.3 (30%)** is precisely what is fixed in our response rendering pipeline. If changes are small (the user enters characters into a field), an ultra-light binary delta is sent. If the screen is redrawn extensively (opening a new form, calling a menu), the engine immediately drops optimization and sends a flat full frame. When compressed, it weighs less than a delta packet overloaded with unique mutation indices.

## Chapter 4. Limitations, Tradeoffs, and the Cost of Solutions (Trade-Offs)

### HTTP vs. gRPC

#### Why HTTP/1.1 and JSON Were Not Suitable for Terminal Rendering

When designing the network layer for PixelTerminalUI, we initially relied on a standard stack: HTTP POST requests and text-based JSON. However, the specific nature of terminal UI (especially interactive scenarios like games) requires high input responsiveness—latency directly impacts the smoothness of frame rendering.

In practice, we encountered two fundamental issues with the HTTP/JSON stack:
- Unstable Latency (Jitter): Although the best individual HTTP responses were within an acceptable ~50–60 ms, the average frame time fluctuated wildly, reaching up to 150 ms. In scenarios where the user pauses between keystrokes, the operating system has time to cool the TCP socket. Each new network handshake resulted in FPS drops.
- Allocation overhead: Serializing the screen structure to text, allocating memory for JSON strings, and subsequent parsing on the client loaded the Garbage Collector (GC), leading to micro-freezes.

#### Transition to Code-First gRPC: A Data-Centric Approach

Transitioning the transport to gRPC (HTTP/2) allowed us to move away from OOP inheritance in contracts to a flat binary composition (TerminalResponse with optional Payload blocks). Local profiling of Kestrel logs showed a qualitative change in performance:
- **Response time stabilization**: Latency dropped to a predictable 30–34 ms per transaction. Due to the persistent HTTP/2 connection (multiplexing), peak latencies due to socket reopenings completely disappeared.
- **Frame Determinism**: The Latency graph has leveled out, providing the terminal with a stable refresh rate without jitter or microlag.

To achieve maximum performance, the `UseConstructor = false` optimization was applied to the `protobuf-net` configuration. By default, the library attempts to initialize deserializable types through their constructors. For C# records (`record`), this means calling hidden factory methods and checking immutability, which creates micro-allocations on the heap. Disabling constructor invocation forces the library to allocate memory for the object, bypassing the standard constructor (via `FormatterServices.GetUninitializedObject`), populating fields directly with a binary stream.

*Important architectural tradeoff:* With this approach, standard validators inside record constructors simply won't work. If your system requires strict business validation of incoming packets, you'll need to either move the logic to gRPC interceptors (middleware) or use specialized callback methods with the `[ProtoAfterDeserialization]` attribute.

#### Performance Benchmark Results (`BenchmarkDotNet`)

To clearly illustrate the cost of the text format, we compared the marshaling speed of a standard frame via `System.Text.Json` and a binary Code-First Protobuf with constructors disabled:

| Method                                | Mean     | Error   | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------- |---------:|--------:|--------:|------:|-----:|-------:|----------:|------------:|
| ExecuteCodeFirstProtobufSerialization | 519.7 ns | 1.09 ns | 0.91 ns |  0.58 |    1 |      - |       0 B |        0.00 |
| ExecuteSystemTextJsonSerialization    | 902.3 ns | 1.31 ns | 1.09 ns |  1.00 |    2 | 0.3557 |     744 B |        1.00 |

##### Analysis of results:
- **Memory Allocation (Allocated):** By eliminating intermediate strings and suppressing constructors, Protobuf serialization achieved a respectable **Zero Allocation (0 bytes)**. Meanwhile, `System.Text.Json` allocates 744 bytes of heap memory for token parsing, generating garbage in Gen0.
- **Processing Speed ​​(Mean):** The binary deserializer runs almost **twice as fast** (519 ns versus 902 ns). On the scale of a distributed WMS system, this saves millions of backend CPU cycles on parsing network packets.

### Fun Math: When Will Allocations Gobble Up Gigabytes?
Nevertheless, even the optimized figure of **63.32 KB** per full session cycle (creation, transitions, buffer writes, and deletions) represents a significant allocation on the .NET service side within a single transaction. For fun, we can hypothetically estimate the load that would cause gigabytes of memory consumption and evaluate whether the engine and Redis can handle it.

*   **.NET Heap Allocations**:
    Assuming the application processes a constant load of 1,000 RPS (requests per second), the total amount of heap memory allocated would be: `1,000 requests * 63.32 KB = 63.32 MB` per second.
    In one minute of continuous operation, the Garbage Collector would be forced to process and dispose of approximately **3.6 GB** of garbage. In reality, this is a manageable task for a modern CLR within the `Gen0` generation, but on weak cloud virtual machines, it could lead to increased latency due to garbage collection pauses.

*   **RAM consumption in Redis (In-Memory Storage):**
    Redis itself stores data in compressed binary form, and the net hash size of one active session is only about 5–8 KB (since the `uint[]` buffer is converted to a compact string, and screens contain minimal data). With 10,000 concurrent users (concurrent sessions), Redis will occupy `10,000 sessions * 8 KB = 80,000 KB` of RAM.
    A single-threaded Redis on a single CPU core will handle this load effortlessly, without even noticing the network traffic, as its performance ceiling for such operations is usually limited by the network card's throughput, not the CPU or RAM.

Therefore, the bottleneck as the load increases will be allocations for generating `uint[]` for gRPC, not Redis performance. For optimization, consider using `ArrayPool<uint>` or custom serializers.

### Optimization Prospects and Architectural Tradeoffs

Any Proof of Concept is a balance between conceptual clarity, hypothesis testing speed, and final performance. The current implementation of the PixelTerminalUI engine intentionally relies on a number of compromises, which have clear development vectors in a real-world enterprise production environment:

#### 1. Two-stage rendering vs. One-Pass with Span
In the current design, the engine first generates a frame in a leased `Pixel[] pooledBuffer` and then packs it into a `uint[]` transport array. From an extreme High-Performance perspective, this represents an optimization:
* **Current choice:** Separating the layers (geometry calculation separately, binary packing separately) is a deliberate choice to maintain the readability of the renderer code and avoid mixing widget business logic with low-level bit manipulation.
* **Development vector:** Combining these stages. Switching the renderer to write directly to `Span<uint>` (directly to an array rented from `ArrayPool<uint>`) will remove the intermediate `Pixel[]` buffer, get rid of the transport array allocation on the heap, and reduce frame allocations to absolute zero.

#### 2. Scaling the Step State Machine
Using a flat `switch-case` based on `Command<TEnum>` ensures the fastest O(1) state switching at runtime. However, for complex business processes (where a chain of warehouse receiving steps can contain dozens of branches), manually describing such transitions reduces code readability.
* **Solution:** In production use, the optimal solution to this problem is to use **C# Source Generators**. The developer can describe multi-step scenarios in an elegant declarative Fluent API, and the code generator will expand it at compile time into a similar, highly efficient, and flat `switch-case`, preserving both the clean architecture and Zero Allocation at runtime.

#### 3. Encapsulation Security in gRPC Serialization
The optimization of `UseConstructor = false` in `protobuf-net` allowed us to completely suppress allocations when assembling command objects from a binary stream. This imposes a strict requirement on the Domain Model design: field validation should not be tied to record constructors. Within the Backend-Driven UI architecture, this compromise is entirely justified, since all heavy data validation occurs server-side within the command execution context, not during transport packet deserialization.

#### 4. Text JSON in Redis vs. Pure Binary MsgPack

In the current implementation of the session storage layer, `TerminalScreen` is serialized into a JSON string before being sent to the distributed cache.

- **Current choice:** The use of JSON at the PoC stage is dictated solely by the convenience of quick debugging, logging, and manual inspection of key states in the Redis CLI console. On small screens (5–8 KB), the overhead of text parsing remains within acceptable limits.
- **Development vector:** An obvious step for the production version of the engine is to switch to true binary serialization of the session cache. Using `MessagePack` or `MemoryMarshal.Cast` (for directly writing the `uint[]` buffer as a raw byte array) will completely eliminate text overhead, reduce the load on the backend CPU when reading frame history, and reduce the data volume within Redis Hash severalfold.

### Historical Context: Why Not WebSockets + HTML/JS
This project is an attempt to rethink the real-world background of large legacy WMS systems that emerged 15-20 years ago. Back then, there were no modern web stacks or powerful processors inside data collection terminals (DCTs). The hardware was weak, and the warehouse Wi-Fi between the shielded racks was constantly interrupted.

Under these conditions, a custom-written server-driven UI, returning ready-made pixel matrices to the client, was the only way to make the system work. The client application on the DCT remained "dumb" and as lightweight as possible; it didn't waste battery power parsing heavy HTML/JS and UI logic, and the server handled all the work (focus, validation, widget rendering). If the connection was lost, the "dumb" client instantly restored the image from where it was.

In modern, new system development from scratch, using TUI matrices is foolish when Flutter, WebSockets, or gRPC are available. However, as a tool for evolution and rescuing existing legacy code, this approach is viable.

### Applicability in Real Business
Let's look at things pragmatically: implementing such an engine directly into a large warehouse or manufacturing facility is practically impossible. The existing business processes and codebases of legacy systems are so vast and intertwined that rewriting them with Externalized Session State would require colossal budgets and carry enormous risks of process downtime.

Therefore, `PixelTerminalUI` should be viewed not as a finished product for immediate migration to your production system, but as a proof-of-concept (PoC). The project proves that even old, seemingly dead-end Server-Driven UI architectures can be significantly optimized in terms of memory and traffic by applying modern .NET platform patterns (`ArrayPool`, `Span<T>`, bit masks, and hybrid frame delivery).
