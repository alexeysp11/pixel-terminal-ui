# 🎮 Demo Game: The Lost Grid (Architecture Overview)

[English](demo-game.md) | [Русский](demo-game.ru.md)

**The Lost Grid** is a text-based game created solely to demonstrate the rendering capabilities and inner workings of the `PixelTerminalUI` framework within a Backend-Driven UI approach.

### 👥 Gameplay & Mechanics
The game features two character classes that dictate how players interact with the environment:
* **Hacker** — specializes in breaching terminals and subverting network nodes.
* **Rigger** — deploys and controls drones to scan and scout the surrounding area.

---

### ⌨️ Interface and Terminal Control

The game interface is designed in the style of classic TUI systems:

![The Lost Grid Gameplay](img/gameplay-demo.gif)

* **Navigation Commands:** The player navigates text menus by entering control characters.
* **Hotkeys:** Quickly exit (`-q: Quit`), call help on available actions in the room (`-h: Help`), or step back (`-b: Back`).
* **Inline Input Cursor:** The server ships the focused widget's coordinates alongside every frame, and the client positions the terminal cursor right inside the rendered form widget — you type exactly where the cursor blinks, with no separate input line below the form anymore.
* **Data Input:** The characters the user enters are not sent to the server one by one. The client application assembles the string locally and sends it to the backend only when the `Enter` is pressed.

---

### ⚙️ Under the Hood

The project is split into two decoupled components communicating over the network:

1. **Client (`TheLostGrid.Client`)** — a thin terminal client. It contains no game logic, story strings, or rules. Its only job is to capture user input, transmit it to the server when `Enter` is pressed, and render the resulting pixel matrix received from the backend.
2. **Server (`TheLostGrid.Server`)** — handles game state transitions in a completely stateless manner:
   * Accepts incoming input commands from the client.
   * Fetches the current session state from a distributed cache (Redis).
   * Executes the respective step's business logic.
   * Compiles the new UI component tree into a flat pixel matrix and streams it back to the client.
   * Immediately frees up resources (no session state is kept in the backend's RAM).

---

### 🏗️ Commands & Screens Implementation

Gameplay flow is broken down into isolated command handlers (`Command`) and declarative layout definitions (`TerminalScreen`). The shared step state is represented by an enum (e.g., `OneStepCommandState`).

Below is the actual code from `examples/TheLostGrid.Server/Scenarios/Welcome/` (the screen definition shows a trimmed widget list for brevity — see the full 5-widget version in `WelcomeScreen.cs`):

#### 1. Isolated Navigation Command
```csharp
public sealed class WelcomeStartGameCommand : Command<OneStepCommandState>
{
    public override OneStepCommandState State { get; set; } = OneStepCommandState.Initial;
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        // Transition to the character creation screen
        CharacterCreationScreen nextScreen = new()
        {
            Id = Guid.NewGuid(),
            Name = nameof(CharacterCreationScreen),
            SessionId = context.SessionId,
            ParentScreenId = context.Screen.Id
        };

        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, nextScreen);
        return true;
    }
}
```

#### 2. Declarative Welcome Screen Layout (trimmed)
```csharp
public sealed record WelcomeScreen : TerminalScreen
{
    public WelcomeScreen()
    {
        Name = "WelcomeScreen";
        Width = 40;
        Height = 12;

        TextWidget titleLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "TitleLabel",
            Left = 11, Top = 3, Width = 17,
            Value = "THE LOST GRID TUI",
            Visible = true,
            Inverted = true
        };

        WelcomeStartGameCommand connectionCommand = new();

        TextEntryWidget hiddenInput = new()
        {
            Id = Guid.NewGuid(),
            Name = "ConnectionTriggerInput",
            Left = 2, Top = 8, Width = 36,
            Required = false,
            Hint = "CONNECT TO NEURAL NETWORK",
            Visible = true,
            Command = connectionCommand,
            Value = string.Empty
        };

        // Explicitly link the command back to the widget that triggers it
        connectionCommand.WidgetId = hiddenInput.Id;

        Widgets = [titleLabel, hiddenInput];
        FocusedEntryWidgetId = hiddenInput.Id;
    }
}
```
