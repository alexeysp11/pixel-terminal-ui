# PixelTerminalUI

[English](README.md) | [Русский](README.ru.md)

![.NET Version](https://img.shields.io/badge/.NET-8.0-blue?style=square&logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-Backend--Driven%20UI-orange?style=square)
![State](https://img.shields.io/badge/State-True%20Stateless-brightgreen?style=square)
![Storage](https://img.shields.io/static/v1?label=Storage&message=Extensible%20%20Interface-driven&color=blue)

A **Stateless UI engine** based on **Backend-Driven UI (BDUI)** architecture designed for text terminals (TUI) running on .NET 8.

The framework flattens declarative component trees of screen forms into flat pixel matrices, completely abstracting the business logic from the transport layer. This allows streaming the user interface via any protocol (JSON/HTTP, gRPC, TCP sockets) to thin clients (rugged handheld computers/PDAs, standard OS consoles, custom mobile apps) without holding session state inside the server memory.

📖 **[Architectural Whitepaper: Engine Evolution from Stateful Telnet to Stateless BDUI](docs/evolution.md)** — a deep-dive analysis into legacy enterprise constraints, Garbage Collector optimizations, and low-level bitwise frame packing.

---

## ⚡ Key Features

* 🧵 **True Stateless Design:** Each request from a thin client is processed as an isolated, atomic transaction. The server no longer retains heavy object graphs of active forms, nested delegates, or raw sockets in RAM between user interactions.
* 📦 **Lightweight UI Components:** Screen forms and control inputs are built entirely on top of C# records (`record`). They act as mutable data containers to optimize updates without triggering excessive heap allocations, while remaining perfectly serializable for external distributed caches.
* ⏳ **Asynchronous Commands:** Navigation rules and validation flows are encapsulated into decoupled `ICommand` handlers backed by `ValueTask`. This guarantees ultra-responsive execution loops when communicating with databases, external APIs, and cloud resources.
* 💾 **Process Lineage Tracking:** Out-of-the-box infrastructure to transparently serialize, persist, and recover active steps (execution breakpoints) of command state machines into an external database (In-memory/Redis) across any available cluster node.

---

## 🚀 Quick Start

### 1. Define a Form and a Command

Interfaces in `PixelTerminalUI` are declared compositionally, while state transitions and steps are wired to isolated commands:

```csharp
// Isolated command for handling navigation rules
public sealed class StartGameCommand : Command<OneStepCommandState>
{
    public override OneStepCommandState State { get; set; } = OneStepCommandState.Initial;
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid ControlId { get; set; }

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        // Transition to the next screen form layout
        var nextScreen = new GamePlayScreen { Id = Guid.NewGuid(), SessionId = context.SessionId };
        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, nextScreen);
        return true;
    }
}

// Declarative definition of the startup welcome screen
public sealed record WelcomeScreen : TerminalScreen
{
    public WelcomeScreen()
    {
        Name = "WelcomeScreen";
        Width = 40;
        Height = 10;
        
        var inputId = Guid.NewGuid();
        Widgets = new List<TextWidget>
        {
            new TextWidget { Left = 2, Top = 2, Value = "WELCOME TO THE GRID" },
            new TextEntryWidget 
            { 
                Id = inputId,
                Left = 2, Top = 5, Width = 10,
                Hint = "PRESS ENTER TO START",
                Command = new StartGameCommand { ControlId = inputId }
            }
        };
        FocusedEntryWidgetId = inputId;
    }
}
```

### 2. Register Components in the DI Container

The framework provides a convenient Fluent API to register the rendering core and a high-performance distributed session repository based on **Redis**:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Initialize PixelTerminalUI core rendering engine
builder.Services.AddPixelTerminalUI();
builder.Services.AddPixelTerminalStartup<WelcomeScreen>();

// Connecting a distributed state store based on Redis Hash
builder.Services
    .AddTerminalRedisRepository("localhost:6379,abortConnect=false")
    .WithSessionTimeout(TimeSpan.FromHours(24))
    .RegisterCustomScreens(custom => custom
        // Registering custom polymorphic screens, commands, and widgets for your app
        .RegisterScreen<WelcomeScreen>()
        .RegisterScreen<GamePlayScreen>()
        .RegisterCommand<StartGameCommand>());
```

---

## 🕹️ Demo Application

To see the framework in action, you can run **The Lost Grid** — a text-based demo game written to showcase the capabilities of `PixelTerminalUI`.

![The Lost Grid Gameplay](docs/img/gameplay-demo.gif)

* 📖 [Detailed analysis of the game architecture and gameplay mechanics](docs/demo-game.md)
* 🖥️ **Server Part (API):** `examples/TheLostGrid.Server` — screen logic, commands, and validators.
* 📟 **Client Part (TUI):** `examples/TheLostGrid.Client` — a thin console client for rendering frames.

> ℹ️ **Network Loop:** Input is transaction-based. The client sends a single network payload only when the user presses `Enter` instead of streaming every keystroke.

### 🐳 Running in Docker

You can deploy a ready-made demo game and all the necessary infrastructure with a single command. The framework will automatically set up the engine core server, a high-performance distributed Redis cache, and a user-friendly web control panel:

```bash
# 1. Start the core server and Redis cache with one command in the background
docker compose up -d --build

#2. Connect using a thin TUI client directly from the server console
docker exec -it pixel_terminal_app env PIXEL_TERMINAL_SERVER_URL=http://localhost:8080 TERM=xterm-256color dotnet /app/client/TheLostGrid.Client.dll
```

After successfully deploying the containers, the following local endpoint will be available:
* 🖥️ **Redis Commander Panel:** `http://localhost:8082` — for visually analyzing the structure of fields within **Redis Hash** sessions in real time.

---

## 🗺️ Project Roadmap

The project is being developed as an experimental R&D sandbox. Current implementation status of key architectural nodes:

### ✅ Implemented
- [x] **Double Buffering:** Storing the previous session frame on the server to calculate the change delta and send only the changed pixels to the client.
- [x] **Binary Protocol (Bit Packing):** Packing the symbol, `ConsoleColor` colors, and pixel inverse into a single 4-byte `uint` using bitwise shifts (reducing network overhead).
- [x] **Redis Hash Persistence:** Migrating hot UI state and frame buffers from MongoDB to Redis Hash atomic fields to reduce memory allocations ([Issue #2](https://github.com/alexeysp11/pixel-terminal-ui/issues/2)).

### ⏳ In Development & Backlog
- [ ] **Server-Driven Focus & Inline Input Cursor**: Implementing server-side coordinate mapping for focused text entries. This enables rendering the blinking input cursor (`_`) directly inside the pixel matrix layout instead of handling transactions via the bottom console input line ([Issue #1](https://github.com/alexeysp11/pixel-terminal-ui/issues/1)).
- [ ] **Observability Extension:** Integration of the OpenTelemetry Lightweight Agent (OTLP) to automatically collect Kestrel metrics and trace command execution chains without adding codebase bloat.
