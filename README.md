# PixelTerminalUI

[English](README.md) | [Русский](README.ru.md)

![.NET Version](https://img.shields.io/badge/.NET-8.0-blue?style=flat-square&logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-Backend--Driven%20UI-orange?style=flat-square)
![State](https://img.shields.io/badge/State-True%20Stateless-brightgreen?style=flat-square)
![MongoDB](https://img.shields.io/badge/Database-MongoDB-47A248?style=flat-square&logo=mongodb)

A **Stateless UI engine** based on **Backend-Driven UI (BDUI)** architecture designed for text terminals (TUI) running on .NET 8.

The framework flattens declarative component trees of screen forms into flat pixel matrices, completely abstracting the business logic from the transport layer. This allows streaming the user interface via any protocol (JSON/HTTP, gRPC, TCP sockets) to thin clients (rugged handheld computers/PDAs, standard OS consoles, custom mobile apps) without holding session state inside the server memory.

📖 **[Architectural Whitepaper: Engine Evolution from Stateful Telnet to Stateless BDUI](docs/evolution.md)** — a deep-dive analysis into legacy enterprise constraints, Garbage Collector optimizations, and low-level bitwise frame packing.

---

## ⚡ Key Features

* 🧵 **True Stateless Design:** Each request from a thin client is processed as an isolated, atomic transaction. The server no longer retains heavy object graphs of active forms, nested delegates, or raw sockets in RAM between user interactions.
* 📦 **Lightweight UI Components:** Screen forms and control inputs are built entirely on top of C# records (`record`). They act as mutable data containers to optimize updates without triggering excessive heap allocations, while remaining perfectly serializable for external distributed caches.
* ⏳ **Asynchronous Commands:** Navigation rules and validation flows are encapsulated into decoupled `ICommand` handlers backed by `ValueTask`. This guarantees ultra-responsive execution loops when communicating with databases, external APIs, and cloud resources.
* 💾 **Process Lineage Tracking:** Out-of-the-box infrastructure to transparently serialize, persist, and recover active steps (execution breakpoints) of command state machines into an external database (MongoDB/Redis) across any available cluster node.

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

The framework exposes a fluent API extensions layer to register the core stateless renderer and wire up the session repository state layer:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Initialize PixelTerminalUI core rendering engine
builder.Services.AddPixelTerminalUI();
builder.Services.AddPixelTerminalStartup<WelcomeScreen>();

// Configure distributed user session state persistence inside MongoDB
builder.Services.AddMongoUserSessionRepository(
    "mongodb://localhost:27017",
    "TerminalGameDb",
    setup => setup
        .RegisterScreen<WelcomeScreen>()
        .RegisterScreen<GamePlayScreen>()
        .RegisterCommand<StartGameCommand>()
);
```

---

## 🗺️ Roadmap

- [ ] **Double Buffering:** Persist the previous viewport matrix frame snapshot on the server side to compute a differential layout diff.
- [ ] **Delta Network Transport:** Maximize network bandwidth efficiency by transmitting only changed layout pixel coordinates instead of broadcasting full matrices.
- [ ] **Binary Bit Packing:** Pack the character `char` literal, `ConsoleColor` attributes, and inversion flags into a single 4-byte primitive `uint` via fast bitwise operations to shrink raw payloads 22-fold.
