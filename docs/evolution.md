# Architecture Evolution: From Stateful Telnet to Stateless BDUI

[English](evolution.md) | [Русский](evolution.ru.md)

## Legacy WMS Application

### Background

This project was inspired by my experience working as a .NET developer at an IT company, which was part of a major online digital electronics retailer. I worked in the WMS development department, creating applications for internal logistics and warehouses. One of the key systems was a legacy Text-Based UI application used to interface with mobile data terminals (MDTs).

### Author's Motivation and Prerequisites

As the company aimed to modernize and implement contemporary CI/CD practices, serious difficulties arose with automating the build and deployment of monolithic applications running on **.NET Framework 4.8**. Management began exploring options to migrate core projects to up-to-date versions of the platform (at that time, **.NET 6/8**), and I decided to take ownership of the old Telnet UI application.

To be honest, from the very beginning, this warehouse text UI application seemed inconvenient, overly specific, and even a bit strange to me. For any developer accustomed to a modern tech stack, a text user interface (TUI) in the 2020s looks like an archaic dinosaur:
* **Rigid constraints:** The interface is locked into a strict text grid (e.g., 24x16 characters).
* **Input specifics:** A complete absence of mouse interaction — all widget relies on arrow keys, Enter/Esc, hotkeys, and barcode scanner triggers.
* **Apparent simplicity:** At first, you think the system is just a basic string output via `Console.WriteLine`, with nothing interesting to explore.

The roots of this legacy project went back 15 years: it was originally written in **Visual Basic**, then decompiled, and roughly ported to C# (.NET Framework). Naturally, such a layer of history brought with it a mass of strange, controversial, and ambiguous solutions. 

However, as I maintained the project, I began diving into the internal mechanics of the engine: the screen and widget rendering systems, focus switching algorithms, navigation, delegate invocation logic for event handling, and rendering specifics. At some point, I caught myself thinking that I really liked the internal engineering of this engine. Of course, I am not talking about the WMS business logic (it is unlikely that pallet movements could drive anyone to ecstasy), but rather the ingenuity of the engineering solutions under the hood. I wanted to reimagine these ideas using a modern technology stack.

### Visual Screen Example

Within the warehouse workflow, an on-screen form sequentially requested data from the employee, switching focus between input widgets or displaying a selection menu. A typical product receiving form screen looked something like this:
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

Although the engine was initially designed for a text-based interface, end-users (warehouse workers) rarely interacted with the system directly via Telnet clients:

* **Developers and QA engineers** used a direct Telnet connection through the console for fast debugging, business logic validation, and manual UI testing.
* **Line staff (warehouse operators)** worked with rugged mobile data terminals (MDTs) from brands like Zebra, Honeywell, etc. These Android-powered MDTs ran a custom mobile application built with **Xamarin**. 

This Xamarin app connected to the server, received the text-based screen layout, and rendered it on the mobile display as a "one-to-one" match with the console view. At the same time, the mobile client enhanced the text interface with platform-specific features that were unavailable in standard Telnet:
1. **Audio notifications:** Playing distinct audio alerts for successful barcode scans versus validation errors.
2. **Color coding:** Highlighting critical errors or warnings with bright colors to catch the worker's attention.
3. **HTML page rendering:** Frequently used to display product information pages.

### 1. Screen Lifecycle and the Role of Reflection in the Legacy System

The old WMS application did not utilize IoC containers (Dependency Injection) or object pooling. The screen management logic was built around a dynamic main menu tied directly to the database.

#### How the Main Menu Screen Was Structured

On the mobile data terminal (MDT) screen, the main menu was displayed as a simple numbered list of warehouse operations available to the user. Visually, it looked something like this on the terminal:
```
+------------------------------------+
|             MAIN MENU              |
|                                    |
| 1. RECEIVING                       |
| 2. PUTAWAY                         |
| 3. PICKING                         |
| 4. INVENTORY                       |
|                                    |
|                                    |
| 0. EXIT                            |
|                                    |
| SELECTED OPTION: ..                |
|                                    |
|                                    |
| CHOOSE OPTION     ESC - BACK       |
+------------------------------------+
```

#### Menu Mechanics and Reflection Isolation

* **Database-Driven Management:** The item list for this screen was generated dynamically. The system queried the database, checked the employee's permissions, and displayed only the rows they were authorized to see.
* **Use of Reflection:** In the database table, each menu item was mapped to a text string representing the C# class name of the corresponding screen. When the warehouse worker selected an item (e.g., pressed `1`), the system read the class name from the database and instantiated the starting screen object using reflection. This allowed system analysts to quickly enable or disable workflows directly at the DBMS level without rebuilding the application.
* **Transitions via the `new` Operator:** Within the warehouse workflows themselves (once the starting screen was opened), reflection was rarely used for navigation. When typing data into text fields (e.g., widgets like `TextEntryWidget`), new screens were not spawned by default — the input was handled by a standard validation delegate within the context of the current screen. A new window was created only when a process step needed to be switched (e.g., opening a product scanning screen after a cell selection screen). In the vast majority of cases, this was done directly via the `new NextStepScreen()` operator.

### 2. State Retention Mechanics and Network Event Loop in the Legacy System

The architecture of the old framework relied on a persistent (Stateful) TCP connection and character-by-character input processing, which imposed rigid constraints on scalability.

#### Anatomy of the Network Event Loop
The session lifecycle and UI rendering at the framework level were structured as follows:
1. **Connection Authorization:** When an MDT connected to the server, a system method named `AcceptSocket` was invoked. Inside this method, a session was initialized and the root screen object was created. The reference to the screen and its components was kept directly within the method servicing that specific socket.
2. **Character-by-Character Listening:** The framework called the blocking `Socket.Listen()` method, which reacted to **every single character entered by the user**. 
3. **Processing Cycle (Event Loop):** As soon as a character arrived from the client, an internal loop started:
   * The framework traversed the component tree of the current screen;
   * Corresponding event handling delegates for the widgets were executed;
   * The active screen was rendered into a flat character array;
   * The character array was sent back to the MDT over the open TCP channel.
4. **Focus Management:** The widget currently in focus (e.g., `TextEntryWidget`) was stored as a reference in a screen field (`FocusedWidget`). Focus was synchronized with the client (Telnet or the Xamarin app), after which the server went back to sleep on the `Socket.Listen()` method, awaiting the next character.

The legacy system's character-by-character mode was used to achieve an "in-place" feature: characters were printed directly within the coordinates of the `TextEntryWidget`, allowing the server to control widget boundaries on the fly (clipping) and interrupt input when the length limit was exceeded. Parsing each character also allowed for data interception on the fly and, if desired, autocompletion. However, the price for this was restarting the entire Event Loop and sending a frame over the network for each character pressed.

#### How the Call Stack and Object Graph Were Retained in Memory
Since all navigation and business logic were built on synchronous delegates (e.g., input validation opening a new screen inside itself), references to child screens were captured within the chains of these delegates. 

While the user was deep inside the menu hierarchy, the `AcceptSocket` method continued to hold a reference to the root screen, which held references to its widgets, which held references to the delegates, and those delegates pointed to new child screens. This entire tree-like structure "lived" in the server's memory for as long as the TCP connection remained active.

#### Scalability Implications (The Price of the Architecture)
Retaining full UI object graphs for every single user led to inefficient and skyrocketing RAM consumption. This created a critical bottleneck:
* **Strict Rate Limiting:** To prevent the server from crashing due to resource exhaustion, a hard limit was configured at the infrastructure level — **no more than 50 active connections** per application instance. By modern Highload standards, this is a microscopic figure.
* **Complex Scaling:** Horizontal scaling of the system to meet the needs of a large warehouse was achieved not through efficient load balancing, but through the extensive deployment of a huge number of isolated application instances, each wasting gigabytes of RAM just to maintain 50 sessions.

## The First (Stateful) Version of PixelTerminalUI

*Note: `PixelTerminalUI` is a completely independent, open-source project developed in my spare time. It was built from scratch, is not a replica of any commercial systems, and contains no code or confidential information belonging to my previous employer.*

The project was originally conceived as an attempt to replicate the existing legacy application with all its nuances, aiming to experience the architectural pain points from the inside out and find an effective solution. The first version of the framework mirrored many of the old problems, but with one major difference — the transport layer was switched to a **REST API** instead of a persistent TCP/Telnet connection.

The result was the same old SmartUI design, where event handling and navigation relied on synchronous delegates tightly bound to the screen's lifecycle on the server.

However, the interaction was split into self-contained "data packets." To prevent losing screen states between HTTP requests, I had to implement server-side session management using a standard `ConcurrentDictionary`, with the session UID serving as the key. 
* **The Memory Reclamation Problem:** Since a persistent socket was no longer used, the server could not detect when a client closed the application. I had to manually write TTL (Time-To-Live) logic and session invalidation mechanisms within the dictionary to prevent `OutOfMemory` exceptions.
* **Batch input processing instead of character-by-character echo mode:** Migrating the system to HTTP/REST required abandoning the server's constant retention of the input stream. In the demo CLI client, processing was switched to batch mode: data input was moved off-screen to a separate line under the form, prefixed with `>`. This allowed the backend core to be isolated from low-level client UI tasks (tracking the focus caret, local rendering of characters within fields), but it reduced the overall interactivity of the interface within this text-based prototype.
* **The Outcome:** This architectural approach "failed to take off." An attempt to apply it in a real-world scenario revealed that integration would require either completely rewriting the highly complex legacy core of the system or replacing the core with a modern one while manually rewriting hundreds of existing widgets and screens. In terms of labor costs, both options were equally time-consuming and economically impractical.

*Author's disclaimer:* Certainly, for an industrial server-driven UI (BDUI) system, text input under a form is a limitation. In a commercial implementation, the server must transmit the active field's metadata (absolute `CursorX`/`Y` and `MaxLength` coordinates), and the "dumb" client must perform local echo input of characters strictly within the widget's boundaries. However, this step was intentionally left out of the first iteration: priority was given to the backend architecture rather than writing a complex UI engine for the system console.

### How It Worked (The SmartUI Concept)

In the first version of the framework, all layout logic, focus management, and event handling remained on the server side. The client application (a thin client) served strictly as a rendering terminal, displaying the finalized character matrix received from the server.

A developer described the screen declaratively and bound the input handling logic to the synchronous delegates of the widgets (such as `EnterValidation`, inherited from the `TerminalScreen` base class).

An example of the starting screen implementation in the first version of `PixelTerminalUI`:
```csharp
public class StartScreen : TerminalScreen
{
    private TextEntryWidget txtUserInput;

    protected override void InitializeComponent()
    {
        txtUserInput = new TextEntryWidget();
        txtUserInput.Name = nameof(txtUserInput);
        txtUserInput.Top = 14;
        txtUserInput.Left = 0;
        txtUserInput.EntireLine = true;
        txtUserInput.Hint = "PRESS ENTER TO CONTINUE";
        txtUserInput.EnterValidation = txtUserInput_EnterValidation;
        Widgets.Add(txtUserInput);
    }

    // Input was processed through a synchronous delegate method
    private bool txtUserInput_EnterValidation()
    {
        switch (txtUserInput.Value)
        {
            case "-n":
                // The server thread was stopped here waiting for a new screen,
                // holding the entire Call Stack and screen context in memory.
                ShowScreen(new frmLogin());
                break;
            case "-q":
                ShowInformation("Are you sure to exit?");
                break;
        }
        return true;
    }
}
```

#### Structure of the Base Screen in the SmartUI Architecture

To clearly see the problem of mixed responsibilities and memory resource retention, one only needs to look at the implementation of the `TerminalScreen` class from the first (Stateful) version of the framework. This class combined the data model, rendering logic, manual memory management, and navigation context:

```csharp
public abstract class TerminalScreen
{
    public string Name { get; set; } = "";
    public int Height { get; set; }
    public int Width { get; set; }
    public SessionInfo? SessionInfo { get; set; }
    
    // Direct reference to the parent screen (retains the object graph)
    public TerminalScreen? ParentScreen { get; set; } 
    
    public List<TextWidget> Widgets { get; set; } = [];
    public TextEntryWidget? FocusedEntryWidget { get; set; }
    
    // Synchronous delegates for business logic and navigation
    public Func<bool>? ShowValidation { get; set; }
    public Func<bool>? ScreenValidation { get; set; }
    public Action? ShowMainMenu { get; set; }
    public Action? ShowSettings { get; set; }
    public ScreenParameters? ScreenParameters { get; set; }

    public virtual void Show()
    {
        int count = Widgets.Count;

        // Attempt to manually optimize allocations due to constant recreation of screens
        TextWidget[] poolArray = ArrayPool<TextWidget>.Shared.Rent(count);
        try
        {
            if (!OnShowValidation()) return;

            SessionInfo!.CurrentScreen = this;
            SessionInfo.AssignEmptyDisplayedInfo();

            Widgets.CopyTo(poolArray, 0);
            Array.Sort(poolArray, 0, count, Comparer<TextWidget>.Create((x, y) =>
            {
                int res = x.Top.CompareTo(y.Top);
                return res != 0 ? res : x.Left.CompareTo(y.Left);
            }));
            
            ReadOnlySpan<TextWidget> sortedSpan = poolArray.AsSpan(0, count);
            FocusedEntryWidget ??= ConfigureWidgets(sortedSpan);

            ShowTextWidgets(sortedSpan);
            // ... buffer rendering logic
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { ArrayPool<TextWidget>.Shared.Return(poolArray); }
    }

    // The method that creates an infinite nested Call Stack
    public void ShowScreen(TerminalScreen screen)
    {
        try
        {
            SessionInfo.CurrentScreen = screen;
            screen.ScreenParameters = ScreenParameters ?? new ScreenParameters();
            screen.SessionInfo = SessionInfo;
            screen.ParentScreen = this; // Link references (creates a two-way connection / closure)
            screen.Init();
            screen.Show(); // Thread enters the nested method and blocks here
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }
    // ...
}
```

Similarly, widgets also had state, rendering logic, and sometimes references to other widgets.

### Why Did This Architecture Prove Unsuitable for Distributed Systems?

An attempt to port the old architectural model "as-is" onto a REST API foundation revealed three fundamental problems that make the project unviable in modern environments.

**1. Infrastructure and Technical Constraints**:

* **Synchronous Handlers and I/O Blocking:** Rendering methods and event handlers (such as `EnterValidation`) remained completely synchronous. Whenever a network request, database query, or disk operation was required, the current server thread was fully blocked. This eliminated the web service's throughput capacity under high load.
* **Inability to Scale Horizontally:** All session data and the graph of open screens were stored strictly within the RAM of a specific application instance (`ConcurrentDictionary`). Deploying such a service in a cluster behind a load balancer broke the system: *Server 1* would render the screen, but the subsequent input request from the same user would be routed to *Server 2*. Since *Server 2* knew nothing about this session, the navigation flow was interrupted, causing the session to crash.
* **RAM Degradation and Garbage Collector Overhead:** Retaining heavy object graphs and chains of invoked screens in RAM between HTTP requests led to skyrocketing memory consumption as the user base grew. Long-lived objects were forced into older generations of the heap (Gen 1 / Gen 2), increasing the load on the Garbage Collector and inducing long, blocking pauses ("Stop-the-world").

**2. Architectural Dead End: Logical Chaos and Spaghetti Code**

The most critical factor in the collapse of the SmartUI approach was not performance bottlenecks, but the **unmanageable complexity of the codebase**:

* **Event Loop Breakdown on Screen Return:** The most frustrating and hard-to-reproduce bugs occurred when a child screen was closed. When widget returned to the parent screen, the framework had to restore focus to the original widget. However, because the shared state was kept in memory, the internal Event Loop executed incorrectly: field values were not reset, old validation logic re-triggered, and focus jumped to random elements.
* **The "Black Box" Effect:** As a result of the stateful approach, the code turned into monolithic spaghetti, where rendering logic, focus management, network exchange, and business validation were tightly coupled. It became completely impossible to predict how a screen would behave when adding a new step.

*Note: A similar codebase state was observed in the original engine of the legacy WMS application. However, stability there was achieved through years of granular adjustments and continuous testing in a live warehouse environment. The code worked, but it turned into a sacred cow that everyone was afraid to touch. Replicating this path in a new open-source project made no sense.*

## Transition to Stateless: Breaking the Call Stack and Architecture

To make the system scalable and eliminate spaghetti code, I completely abandoned state retention in the server's memory and shifted to designing a Stateless architecture for the engine. It was founded on three principles: external state storage (MongoDB/Redis), separation of data and logic, and the use of isolated renderers. No more long-lived screen objects, nested delegates, or blocked threads.

Now, every HTTP or gRPC request from a client is an isolated, short-lived transaction.

### 1. Declarative Interfaces: Data Records
The interface is built on C# records (`record`), which are used as passive data structures serialized into JSON. Examples of these structures:
```csharp
public readonly record struct Pixel(
    char Symbol = ' ',
    bool IsInverted = false,
    ConsoleColor Foreground = ConsoleColor.White,
    ConsoleColor Background = ConsoleColor.Black);

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
    public bool Visible { get; set; } = true;
    public int Height { get; set; }
    public int Width { get; set; }
    public Guid? ParentScreenId { get; set; }
    public IEnumerable<TextWidget> Widgets { get; set; } = [];
    public Guid? FocusedEntryWidgetId { get; set; }
    public bool EnableDoubleBuffering { get; init; } = false;
}

public record SimpleMessageScreen : TerminalScreen;
```

#### Example: Adding a Custom Field Without Pain

Let's imagine a business requires adding a new field to the login form—an access level indicator (for example, to display an employee's role). All that's required is to extend the standard TextWidget through regular inheritance and declare a new property:
```csharp
public record ClassifiedTextWidget : TextWidget
{
    // New application field required for security business logic
    public SecurityAccessLevel AccessLevel { get; set; } = SecurityAccessLevel.Standard;
}
```

After this, the new widget is immediately ready for use within any screen, for example, our starting WelcomeScreen, without changing the engine core:
```csharp
public sealed record WelcomeScreen : TerminalScreen
{
    public WelcomeScreen()
    {
        Name = "WelcomeScreen";
        Width = 40;
        Height = 10;
        
        Guid inputId = Guid.NewGuid();
        Widgets = new List<TextWidget>
        {
            new TextWidget { Left = 2, Top = 2, Value = "WELCOME TO THE GRID" },
            
            // Easily reuse our new widget with a custom business property
            new ClassifiedTextWidget 
            { 
                Left = 2, Top = 3, 
                Value = "RESTRICTED AREA", 
                AccessLevel = SecurityAccessLevel.Critical,
                Foreground = ConsoleColor.Red 
            },
            
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

### 2. Processing Logic: Commands (`ICommand`)

Business logic is extracted into atomic `ICommand` components that support asynchronous operations and a serializable execution step. The binding of commands to UI elements is implemented to allow validation handling without the need to store state in the service's memory.

Here is how it is implemented in the framework's core:
```csharp
public interface ICommand
{
    Guid Id { get; }
    Guid WidgetId { get; set; }
    int RawState { get; set; }
    ValueTask<bool> ExecuteAsync(ICommandContext context);
}

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

The command implementation concept in `PixelTerminalUI` is directly inspired by how the C# compiler generates asynchronous methods using `IAsyncStateMachine`. During compilation, every `async` method is transformed into a state machine:
*   Local variables of the original method become fields of the generated structure or class.
*   An integer field (`_state`) is added to track the current execution step.
*   The method body is split into blocks inside a `switch/case` statement at suspension points (`await`).

In the new framework architecture, this exact pattern is applied to guarantee complete fault tolerance and distribution of business logic. The command itself and its current step (`RawState`) represent passive, serializable data. They are saved to external session storage alongside the current screen. 

This makes it possible to interrupt process execution on one cluster node at any moment, save the intermediate command "breakpoint" to the database, and, upon receiving the next network packet from the client, restore the command and resume execution from the exact same step on a completely different server.

To model the execution sequence steps, a strongly-typed enumeration (`enum`) is used to define the roadmap for the command:
```csharp
public enum MultiStepState
{
    Initial = 0,
    Processing = 1,
    Finalizing = 2
}
```

Below is an example of the `SequenceExecutionCommand` multi-step command implementation:
```csharp
public sealed class SequenceExecutionCommand : Command<MultiStepState>
{
    public override MultiStepState State { get; set; } = MultiStepState.Initial;
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }

    public override ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        if (context == null)
        {
            return ValueTask.FromResult(false);
        }

        switch (State)
        {
            case MultiStepState.Initial:
                // --- STEP 1: First phase of input validation and processing ---
                // Simulate runtime business validation check on the initial processing step
                if (string.Equals(context.InputValue, "ABORT", StringComparison.OrdinalIgnoreCase))
                {
                    // Transmit a custom context-driven business failure message back up to the pipeline orchestration layer
                    context.ErrorMessage = "Operational workflow sequence interrupted by user initial phase abort signal.";
                    return ValueTask.FromResult(false);
                }

                // Transition the internal state machine tracking register onto the next operational milestone phase
                State = MultiStepState.Processing;

                // Modify the current form layout to accept input for the next step
                // [Logic to update controls inside context.Form goes here]
                
                return ValueTask.FromResult(true);

            case MultiStepState.Processing:
                // --- STEP 2: Second phase of data processing ---
                // Simulate a secondary deep verification check within the transactional processing loop
                if (context.InputValue.Length < 3)
                {
                    context.ErrorMessage = "The submitted processing execution verification key sequence token is too short.";
                    return ValueTask.FromResult(false);
                }

                // Transition the state machine into the final phase
                State = MultiStepState.Finalizing;

                // Execute the primary transactional action
                // [Business logic execution/external persistence goes here]
                
                return ValueTask.FromResult(true);

            case MultiStepState.Finalizing:
                // --- STEP 3: Completion and navigation stack cleanup ---
                // [Logic to return to the parent form or redirect to the main menu goes here]

                return ValueTask.FromResult(true);

            default:
                return ValueTask.FromResult(false);
        }
    }
}
```

### 3. Error Interception Pattern (Short-Circuit Interception)

*Note: The central orchestrator of the entire system is the `RequestPipelineHandler`. This component is responsible for the end-to-end lifecycle of processing each user action: it accepts raw input, coordinates infrastructure validation, executes business commands bound to widgets, and finally routes the mutated screen to stateless rendering to assemble a network response.*

Stateless architecture imposes strict requirements on exception handling. If a business command or infrastructure validator fails, we can't simply throw an exception—this will lead to a network failure and session loss. The engine must be able to intercept the failure on the fly, replace the current screen with a unified notification window, and return a valid pixel matrix to the client.

#### Exceptions vs. Controlled Failures

An astute reader might ask: *"Why couldn't you just wrap the entire `RequestPipelineHandler` in a global try-catch block, catch any system exceptions, and automatically turn them into error screens?"*

This is a deliberate architectural limitation of the framework, separating responsibilities:

1. **Separation of Error Types:** Infrastructure failures (invalid input) and business failures (out-of-stock) are *normal, predictable scenarios*. They shouldn't generate severe system exceptions. Passing status through a `bool` and context is a more performant approach that doesn't overload the CPU with stakeholder traces.
2. **Data Integrity Protection:** An unhandled exception (`Exception`) in the developer's application code is an emergency (application bug, DBMS connection failure, OOM). At this point, the memory and session state become non-deterministic. If the engine begins to suppress such exceptions internally and redraw the interface, it will mask a critical defect. The emergency request must fail honorably so that the standard host logging mechanisms (Middleware/Logging) are triggered and transactions in the DBMS are rolled back.
3. **Application Layer Responsibility:** Wrapping a potentially dangerous piece of code (e.g., an integration request to an external API) in a try-catch, handling it, and deliberately returning false with a user-friendly message is the direct responsibility of the application developer writing the specific command. The framework provides all the tools for this (e.g., the ErrorMessage property in the CommandContext context).

#### Interception at the Infrastructure Validation Layer

The first line of defense is validating incoming data before it enters the business logic. If a framework validator (such as a maximum string length or character validation check) reports a failure, the engine immediately interrupts the standard pipeline. It calls the error factory and sends the batch to rendering:
```csharp
IEnumerable<ValidationDelegate> screenValidators = _validationProvider.GetValidatorsForScreen(screen.Name);
foreach (ValidationDelegate validate in screenValidators)
{
    ValidationResult validationResult = validate(screen, request.UserInput);
    if (!validationResult.IsValid)
    {
        // We intercept the validation error and assemble the form via the factory
        string errorMsg = validationResult.ErrorMessage ?? "Validation Fault!";
        SimpleMessageScreen errorScreen = _errorScreenFactory.BuildErrorScreen(sessionId, screen, errorMsg);
        
        await _sessionRepository.SaveActiveScreenAsync(sessionId, errorScreen);
        return await RenderAndBuildResponseAsync(errorScreen);
    }
}
```

#### Failure Handling in Business Commands (Error Transport Pattern)

If validation is successful, control is transferred to the application command. Business logic may fail (e.g., "Product not found in warehouse"). To pass a textual description of the error upward, use the ErrorMessage transport property in the local command context:
```csharp
if (entryWidget.Command != null)
{
    CommandContext commandContext = new(
        sessionId: sessionId,
        screen: screen,
        focusedEntryWidget: entryWidget,
        inputValue: entryWidget.Value,
        sessionRepository: _sessionRepository
    );

    bool isExecutionSuccessful = await entryWidget.Command.ExecuteAsync(commandContext);
    if (!isExecutionSuccessful)
    {
        // Business logic returned false. Resetting invalid input.
        entryWidget.Value = string.Empty;
        await _sessionRepository.SaveActiveScreenAsync(sessionId, screen);

        // Extract the custom failure reason written by the command into the context
        string businessError = commandContext.ErrorMessage ?? "Command Rejected Execution!";
        SimpleMessageScreen businessErrorScreen = _errorScreenFactory.BuildErrorScreen(sessionId, screen, businessError);
        
        await _sessionRepository.SaveActiveScreenAsync(sessionId, businessErrorScreen);
        return await RenderAndBuildResponseAsync(businessErrorScreen);
    }
}
```

This separation isolates the interface generation logic from the processing pipeline. `RequestPipelineHandler` doesn't know what error widgets look like or where they are located on the screen. Its job is to promptly catch the error signal (via `ValidationResult` or `commandContext.ErrorMessage`), switch the active state to the notification form using the factory, and ensure that the client receives a correctly formed delta or full packet.

### 4. System Assembly and Declarative Extension (DI/IoC)

All the infrastructure complexity (renderer management, bit packing, adaptive delta calculation) is hidden under the hood of the framework. To integrate the engine into a host application (for example, into a standard Web API), the application developer uses the declarative Fluent API.

For this purpose, three key extension methods are provided for the built-in DI container `Microsoft.Extensions.DependencyInjection`:

#### Engine Core Initialization (`AddPixelTerminalUI`)
This basic method deploys singleton rendering services in memory and registers built-in components out of the box (text fields, input fields, password masks). At this stage, you can also flexibly manage traffic optimization:

```csharp
// Register with default settings (double buffering enabled)
builder.Services.AddPixelTerminalUI();

// Or by deliberately disabling the frame cache to save server CPU
builder.Services.AddPixelTerminalUI(options =>
{
    options.EnableDoubleBuffering = false;
});
```

#### Registering the Startup Entry Point (`AddPixelTerminalStartup`)
Specifies a template or specific screen type that the engine will automatically generate during a cold start (when the user has just opened the terminal and the session has not yet been created).

```csharp
builder.Services.AddPixelTerminalStartup<WelcomeScreen>();
```
*Architectural detail:* Under the hood, the framework doesn't use reflection or heavy context factories (`IServiceScopeFactory`). Instead, the container compiles a lightweight activation delegate, `Func<Type, TerminalScreen>`, which instantiates new Transient screen objects with the speed of the native `new` operator, completely isolating user sessions from each other.

#### Connecting third-party plugins and widgets (`AddCustomTerminalRenderer`)
The engine is designed according to the Open-Closed principle (open for extension, closed for modification). If the team lacks standard elements, the developer creates their own widget by inheriting from `TextWidget`, writes an isolated renderer for it, and seamlessly integrates it into the general pipeline with a single line:

```csharp
builder.Services.AddCustomTerminalRenderer<CustomWidget, CustomWidgetRenderer>();
```

When the application starts, the engine automatically collects all registered renderers into a thread-safe registry. When the StatelessRenderer encounters your custom widget in the element tree, it instantly finds the required rendering strategy, packs the data into a bitmap, and passes it to the adaptive response builder. The developer extends the system without ever touching the library's core source code.

#### Registering the Startup Entry Point (`AddPixelTerminalStartup`)

Thanks to the Fluent API, initializing the entire UI engine, connecting the distributed state store, and explicitly registering forms and commands on the backend is concise and declarative. Basic configuration of the framework core and frame processing pipeline is performed identically for any data provider:

```csharp
// Initializing the PixelTerminalUI core, processing pipeline, and startup screen
builder.Services.AddPixelTerminalUI(options =>
{
    options.EnableDoubleBuffering = true;
});

builder.Services.AddPixelTerminalStartup<WelcomeScreen>();
builder.Services.AddModuleEndpoints();
```

##### Option 1. Connecting a Redis-based target storage (Recommended)

To minimize latency and automatically manage memory, hot session state is connected via the Redis Hash infrastructure plugin:

```csharp
// Connecting distributed session and frame buffer storage to Redis
builder.Services
    .AddTerminalRedisRepository(redisConnectionString)
    .WithSessionTimeout(TimeSpan.FromMinutes(30))
    .RegisterCustomScreens(custom => custom
        // Registering custom polymorphic screens, commands, and widgets for your app
        .RegisterScreen<WelcomeScreen>()
        .RegisterScreen<GamePlayScreen>()
        .RegisterCommand<StartGameCommand>());
```

> **Note on data management and session disposal:**
>
> Since the `PixelTerminalUI` architecture is completely stateless, the framework core is completely isolated from the database implementation details and operates exclusively through the `ITerminalSessionRepository` contract abstraction. Infrastructure plugins handle atomic entity management under the protection of end-to-end optimistic locking (`Version`).
>
> Initially, when integrating with MongoDB, the strategy for cleaning up stale sessions was delegated to the built-in **TTL (Time-To-Live)** index for the `updatedAt` field. However, due to the specifics of document storage, polymorphic trees of `TerminalScreen` widgets had to be stored in an isolated `active_screens` collection, which meant that automatic TTL deletion of the root session left child screens "orphaned." For PoC integration, this required running periodic background cleanup scripts (Cron/Worker) on the database side.
>
> Switching to a target architecture based on Redis Hash completely solved this problem natively. Since all related data for a single session (active screen, navigation history, frame buffer) resides within a single hash object under the shared key `session:{sessionId}`, the standard TTL mechanism in Redis automatically and atomically recycles the entire structure in a single step, guaranteeing a complete absence of memory leaks in the storage without writing background workers.

### 5. Rendering: StatelessRenderer

The global singleton `StatelessRenderer` is responsible for transforming the form's UI component tree into a flat representation. All rendering logic is built on the concept of a **one-dimensional linear pixel buffer**, similar to low-level video memory patterns (Framebuffers).

The renderer is a completely *Stateless* service: it does not store screen state internally, is unaware of network sessions, and performs a pure mathematical projection of the element graph onto a flat segment of contiguous memory in a single pass, accepting the target buffer from outside the renderer.

The frame generation process is structured as follows:
* **Buffer Initialization:** The calling layer allocates a flat pixel array `Pixel[]` exactly equal to the total grid volume (`Width × Height`), and the renderer fills it with default space characters, recalculating the line offset using the formula `y * Width + x`.
* **Character-Based Blitting (Transfer):** The framework traverses the current form's widget tree. For each visible element, a specialized rendering processor (`IWidgetRenderer`) is retrieved from the IoC container, which calculates absolute coordinates and copies characters with color attributes directly into the shared flat array.
* **Bounds Safety (Clipping):** All projection operations are protected by internal bounds checking loops, preventing the engine from crashing with an `IndexOutOfRangeException` if the coordinates or width of a widget exceed the physical dimensions of the TSD screen.
* **Contextual Hints:** If a form has a widget with focus, the renderer automatically calculates the index of the start of the last line of the screen, centers the hint text (`Hint`) and projects it onto the status bar.

### 6. Evolution of Rendering Optimization (Performance Roadmap)

#### Bit packing and transport primitive (`uint`)

Transmitting a two-dimensional character screen matrix as an array of JSON objects for each individual pixel imposes critical bandwidth constraints on a distributed system. For a standard terminal resolution of 80x24, the frame buffer contains 1920 logical pixels. In JSON text format, a single pixel structure with all its color attributes and inversion flags takes up about 85 bytes (`1920 pixels × 85 bytes ≈ 163 KB / frame`):

```json
{
    "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "screenBuffer": [
        {
            "symbol": " ",
            "isInverted": false,
            "foreground": 15,
            "background": 0
        },
        ...
    ],
    "width": 40,
    "height": 12
}
```

With intensive data input by the user (character-by-character network exchange) or frequent screen updates on the MDT, network bandwidth instantly gets clogged with megabytes of redundant data, most of which consists of empty space characters with the default background color.

To minimize network overhead while maintaining full visualization flexibility (individual color and flags for each pixel), the `PixelTerminalUI` architecture is migrated to a bit-packing pattern.

Instead of serializing high-level objects, data is packed into a flat binary array of `uint[]` primitives. In .NET, the `char` type occupies 16 bits. Using fast bitwise shifts (`Bitwise operations`), the remaining 16 bits of the 4-byte number are allocated for color metadata and system flags.

The bit allocation scheme for the `UInt32` structure (32 bits):
```
UInt32 Structure (32 bits):
[ 16 bits: Character (char) ] [ 7 bits: Foreground ] [ 7 bits: Background ] [ 2 bits: Flags ]
```
With this packing mechanism, the entire 80x24 frame is projected onto a fixed, flat array of primitives, whose raw net size over the network is: `1920 pixels × 4 bytes = 7.5 KB`. This provides a **reduction in transmitted data volume of nearly 22 times** even before applying HTTP traffic compression algorithms (GZip/Brotli).

The static class `PixelBitPacker` is responsible for packing and unpacking data, with forced inlining of methods by the compiler (`AggressiveInlining`), which eliminates the overhead of method calls at runtime:

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

As a result, the serialized JSON takes on the most compact form, representing a simple array of numeric primitives:

```json
{
    "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "screenBuffer": [
        2104832,
        2104832,
        2104832,
        ...
    ],
    "width": 40,
    "height": 12
}
```

##### Performance Microbenchmark Results (`BenchmarkDotNet`)

The comparison was between serialization of high-level pixel models (Legacy) and serialization of a packed flat `uint[]` array (Optimized):

| Method                 | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ProcessLegacyBuffer    | 1.957 μs | 0.0119 μs | 0.0100 μs |  1.00 | 2.7618 |   5.65 KB |        1.00 |
| ProcessOptimizedBuffer | 1.807 μs | 0.0036 μs | 0.0030 μs |  0.92 | 0.9289 |    1.9 KB |        0.34 |

##### Analysis of Results:
* **Memory Allocations (Allocated):** Replacing an array of objects with an array of primitives reduced memory allocations on the Managed Heap by **3x** (from 5.65 KB to 1.9 KB). This significantly reduced the frequency of garbage collector (GC) runs in high-load scenarios.
* **Computational Speed ​​(Mean):** Buffer processing and preparation speed increased by **8%** due to the elimination of allocation overhead and the execution of packing operations directly in processor registers. The main performance gain at this stage is achieved by reducing the load on the serializer and minimizing the network packet size.

#### Memory Allocation Optimization and Buffer Pooling

The theoretical pixel bit-packing pattern requires careful memory management at the C# runtime level. The initial conceptual implementation of the engine (based on two-dimensional `Pixel[,]` arrays) created a potentially high allocation overhead. With this approach, for each tick of the network Event Loop, the renderer allocated a new array in the Managed Heap, which was then mapped to a flat `uint[]`. Under hypothetical loads from hundreds or thousands of concurrent terminals, the constant allocation of short-lived objects would lead to excessive pressure on the Garbage Collector (GC) and blocking micro-pauses at runtime.

To reduce allocation overhead at the frame composition stage, the rendering pipeline was redesigned:

* **Removing multidimensional arrays:** In .NET, `Pixel[,]` arrays incur a built-in CLR overhead for calculating index offsets on each access, and are also not supported by standard memory pools. The engine has been completely converted to working with a flat one-dimensional `Pixel[]` array.
* **Symmetric pooling via `ArrayPool<T>`:** The lifecycle of the intermediate canvas is now isolated within the calling method according to the "rented, returned" rule. The renderer (`StatelessRenderer`) no longer allocates anything on the heap, but performs character-by-character transfers (blitting) directly to the externally provided array. After packing the data into a `uint[]`, the leased `Pixel[]` array is immediately returned to `ArrayPool<Pixel>.Shared.Return()`, minimizing its memory retention.

##### Previous implementation (allocation of `Pixel[,]` within the renderer):
```csharp
private TerminalResponse RenderAndBuildResponse(TerminalScreen screen)
{
    Pixel[,] buffer = renderer.Draw(screen);

    int width = buffer.GetLength(0);
    int height = buffer.GetLength(1);

    uint[] flatBuffer = new uint[width * height];
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            Pixel currentPixel = buffer[x, y];
            byte inversionFlag = (byte)(currentPixel.IsInverted ? 1 : 0);

            // Directly pack the pixels into a continuous primitive array.
            flatBuffer[y * width + x] = PixelBitPacker.Pack(
                currentPixel.Symbol,
                (byte)currentPixel.Foreground,
                (byte)currentPixel.Background,
                inversionFlag);
        }
    }
    return new TerminalResponse(screen.SessionId, flatBuffer, width, height);
}
```

##### Optimized implementation (using a buffer and ArrayPool):
```csharp
private TerminalResponse RenderAndBuildResponse(TerminalScreen screen)
{
    int width = screen.Width;
    int height = screen.Height;
    int totalCellsCount = width * height;

    Pixel[] pooledBuffer = ArrayPool<Pixel>.Shared.Rent(totalCellsCount);
    uint[] flatBuffer = new uint[totalCellsCount];
    try
    {
        renderer.Draw(screen, pooledBuffer);

        for (int index = 0; index < totalCellsCount; index++)
        {
            Pixel currentPixel = pooledBuffer[index];
            byte inversionFlag = (byte)(currentPixel.IsInverted ? 1 : 0);

            // Compressing into the transport target array
            flatBuffer[index] = PixelBitPacker.Pack(
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

    return new TerminalResponse(screen.SessionId, flatBuffer, width, height);
}
```

##### Performance benchmark results (`BenchmarkDotNet`)

Solution measurements for standard mobile device screen resolutions (`40x12`, `40x25`, `80x12`, and `80x25`) in the `Release` configuration:

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
* **Memory Allocations (Allocated):** On the maximum 80x25 grid, heap allocations decreased from 31.36 KB to 7.88 KB (exactly 4x). The rendering middleware now performs 0 bytes of allocations. The remaining 7.88 KB is the inevitable allocation of the resulting uint[] transport array (80 * 25 * 4 bytes ≈ 8 KB), which is required by the serializer and physically cannot be returned to the pool before sending over the network.
* **Mean Computational Speed:** The new approach is consistently 3.6x faster (Ratio 0.27 - 0.28). The processor speedup is achieved by eliminating allocation cycles and using the *Bounds Checking Elimination* optimization in the JIT compiler. When linearly traversing a one-dimensional `pooledBuffer[index]`, the runtime completely disables hidden array bounds checks.

#### Transport Layer Isolation

An important architectural feature of the updated engine is the complete separation of the rendering core and command processing from the network (transport) layer. At the framework level, the input point is the abstract `TerminalRequest`, and the output is the polymorphic `TerminalResponse`.

Thanks to this isolation, virtually any data transfer protocol can be deployed on top of the existing system:
* **HTTP (REST/RPC):** Classic stateless communication, ideal for simple request-response scenarios.
* **WebSockets / SignalR:** Enables a persistent full-duplex connection for instant frame delivery.
* **gRPC (Streaming):** The optimal choice for high-load industrial environments, reducing network header overhead.

The engine itself remains a pure function, and the choice of transport becomes solely a matter of host configuration (infrastructure).

#### Hybrid Adaptive Rendering Mechanism (Double Buffering)

To minimize network traffic, the engine architecture incorporates a double buffering mechanism. The framework stores a snapshot of the previously sent frame of the session in persistent storage. When processing user input, the server calculates the difference (`diff`) between the current and historical matrices. The idea is simple: instead of redrawing the entire screen, only the changed pixels and their coordinates (delta update) are transmitted to the client.

```json
{
  "$type": "delta",
  "mutations": [
    {
      "index": 162,
      "packedValue": 7675392
    },
    {
      "index": 163,
      "packedValue": 7544320
    },
    ...
  ],
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "width": 40,
  "height": 12
}
```

In practice, the JSON text format imposes strict limitations on point updates. To send a single changed pixel, we need to transmit not only its new value but also service metadata: field names, quotes, curly braces, and the cell index. This causes the text delta to rapidly bloat.

Let's visually calculate this math using the example of a small 40x12 screen (480 cells in total):

* **Option A: Send the entire screen (FullFrameResponse)**
  In JSON, this looks like a regular flat array of numbers: [12345,67890,11223,...]. A single value, including the comma separator, weighs on average 8 bytes.
  *Total frame size:* 480 cells × 8 bytes = 3.8 KB. This weight is fixed and doesn't depend on what's happening on the screen.

* **Option B: Send only point changes (`DeltaResponse`)**
  Each change is serialized as an array of objects with a rigid structure: `[{"Index":12,"PackedValue":12345},...]`. Due to the significant overhead of the JSON markup (keys, colons, parentheses), just one such record weighs an average of **36 bytes**.

Now let's compare the numbers: if we divide the total screen weight (3800 bytes) by the weight of one mutation (36 bytes), we get a critical point of **105 cells**. Thus, if only **22% of the pixels** on the screen change, a point JSON delta packet starts to weigh **more** than sending the entire matrix.

However, in reality, traffic is compressed at the network level (GZIP / Brotli). How does this change the picture?

- When we send a delta, the `"index"` and `"packedValue"` service keys are reduced to almost zero by compression algorithms by searching for duplicate strings. However, it's important to understand that the delta still contains unique values ​​of the indexes themselves (e.g., `162`, `163`), which compress poorly.
- At the same time, the entire terminal frame (`screenBuffer`) is often filled with huge chains of identical numbers—empty cells of spaces on a black background. Network archivers compress such continuous sequences of duplicates with tremendous efficiency.

Taking into account transport compression, the real economic benefit of a point delta is completely lost around **25% of changes**. This pragmatic threshold of 0.25 is fixed in the component responsible for rendering the response. If changes are small, a compact delta is sent; if the screen is redrawn extensively (opening a new form), the engine immediately drops optimization and sends a flat full frame, which, when compressed, weighs less than the delta bloated by unique indexes.

##### The Downside of Double Buffering: Computing Overhead

Double buffering provides enormous benefits for stability and network traffic density, but shifts overhead to the service itself. When rendering "as is" (without buffering), the engine operates linearly: it allocates one frame array, fills it, and immediately serializes it.

The implementation of Double Buffering dramatically complicates the server pipeline:
1. The previous frame's array is read from the database/cache.
2. The current frame's array is allocated and filled.
3. A full traversal of all matrix elements is performed to calculate the diff (computational complexity `O(N)`).
4. A separate array is allocated for mutations (`PixelMutation[]`).

Under severe CPU constraints on the server or when using ultra-fast local networks (where traffic isn't a bottleneck), this mechanism may prove disadvantageous.

To allow flexible management of this optimization, the Fluent IoC configurator was built into the engine, allowing double buffering to be completely enabled/disabled at the application infrastructure build level.
```csharp
// Initializing the PixelTerminalUI core with explicit double buffering control
builder.Services.AddPixelTerminalUI(options =>
{
    options.EnableDoubleBuffering = true; // Traffic optimization management (true/false)
});
```

### 7. Choosing a Storage Engine: Redis vs. MongoDB

#### The Problem of Polymorphism and Type Discriminators: From MongoDB to Redis

When migrating the session store from MongoDB to Redis, the key challenge was deserialization of abstract widgets and commands, which the Mongo driver handled out-of-the-box via the built-in `_t` (Type Discriminator) field.

To recreate this behavior in Redis and avoid polluting the isolated engine core with .NET infrastructure attributes, dynamic configuration was implemented via the contract modification mechanism (`DefaultJsonTypeInfoResolver`) built into `System.Text.Json`. At application startup, the plugin dynamically mixes the `$type` property with the name of a specific derived class directly into the JSON string, keeping the domain model clean and ensuring transparent polymorphic deserialization.

```csharp
private void ConfigurePolymorphism(JsonTypeInfo typeInfo)
{
    if (typeInfo.Type == typeof(TerminalScreen) && _screens.Count > 0)
    {
        typeInfo.PolymorphismOptions = CreateOptionsForTypes(_screens);
    }
    else if (typeInfo.Type == typeof(CommandBase) && _commands.Count > 0)
    {
        typeInfo.PolymorphismOptions = CreateOptionsForTypes(_commands);
    }
    else if (typeInfo.Type == typeof(TextWidget) && _widgets.Count > 0)
    {
        typeInfo.PolymorphismOptions = CreateOptionsForTypes(_widgets);
    }
}
```

#### Choosing a Data Structure in Redis (String vs. Hash)

Since the ITerminalSessionRepository interface operates on three entities (the active screen, a specific screen by ID, and the historical rendering buffer), we faced a classic data structure dilemma when designing a Redis storage solution.

##### Option 1. Flat JSON (Redis String)
The idea is to package the entire user session into a single monolithic JSON document and store it by the key `session:{sessionId}`.
* **Problem:** High network stack and CPU overhead. Deleting just one screen from the navigation history would require us to read a gigantic JSON over the network (including the heavy terminal framebuffer array `uint[]`), deserialize it in C# memory, delete the element, re-serialize it, and send it back. In a Stateless BDUI engine, this would create unnecessary overhead.

##### Option 2. Pivot Table (Redis Hash)
An alternative is to use the Redis Hash data type. In this design, a single session is represented by a single dictionary object with the `session:{sessionId}` key, and the internal data is distributed across isolated independent fields.

The hash field structure for a single session looks like this:

| Field Name (Field) | Value Type | Field Description |
| :--- | :--- | :--- |
| `version` | Text / Number | Counter for optimistic locking (OCC) |
| `active_screen_id` | Text (Guid) | Pointer to the ID of the currently active screen |
| `historical_buffer` | Binary JSON | Serialized terminal frame rendering buffer |
| `screen:{screenId}` | Binary JSON | Isolated document of a specific screen from the navigation history |

##### Final Choice
**Option 2 (Redis Hash)** was chosen for the repository implementation. This structure allowed for more precise operations. For example, the `RemoveScreenAsync` method now turns into an immediate call to the `HDEL session:{id} screen:{screenId}` command. We completely eliminated the need to read and rewrite the entire session by isolating work with screen history.

#### Benchmark Results and Performance Analysis

To validate the architectural transition from the MongoDB disk model to the lightweight Redis Hash in-memory model, measurements were taken using BenchmarkDotNet. We simulated the full session lifecycle of a terminal BDUI application: session creation, deep navigation through screen history, writing a heavy graphics framebuffer (uint[80 * 25]), selective reading of active components, and subsequent navigation stack cleanup.

Below are the final benchmark results (AMD Ryzen 7 5700U processor in .NET 8):

| Method                               | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0     | Allocated | Alloc Ratio |
|------------------------------------- |----------:|----------:|----------:|------:|--------:|-----:|---------:|----------:|------------:|
| RedisHash_FullSessionCycleSimulation |  8.102 ms | 0.1874 ms | 0.5526 ms |  0.34 |    0.02 |    1 |  31.2500 |  63.32 KB |        0.14 |
| Mongo_FullSessionCycleSimulation     | 23.760 ms | 0.3746 ms | 0.3321 ms |  1.00 |    0.02 |    2 | 218.7500 | 465.18 KB |        1.00 |

##### Why did Redis Hash demonstrate complete superiority?

- **Execution Speed ​​(Mean):** Our Redis Hash-based repository executed the entire chain of operations **3 times faster** (8.1 ms vs. 23.7 ms). Since Redis stores all data structures exclusively in RAM and processes commands through a fast, non-blocking Event Loop, the overhead of network transactions and disk subsystem waits (which are present in MongoDB's WiredTiger engine) are completely absent.
- **Memory Allocation (Allocated):** The heap memory allocation results are even more surprising—Redis Hash requires **7 times less memory** (63.32 KB vs. 465.18 KB). Under the hood, the MongoDB driver generates a huge number of intermediate `BsonDocument` objects, byte pools, and metadata for mapping. Moreover, when updating history arrays, Mongo is forced to traverse heavy structures entirely. In the Redis Hash implementation, we serialize JSON selectively and only for one specific screen. When calling removeScreenAsync, no memory allocations occur in the .NET heap—the HDEL command only sends a string key to the socket.
- Garbage Collector (Gen0) Pressure: Reducing allocations will help reduce the frequency of first-generation garbage collector (Gen0) invocations by a factor of 7. For a Stateless BDUI engine processing hundreds of user terminal sessions per second, this is a critical metric, preventing micro-freezes (stop-the-world) during interface rendering.

### Limitations, Tradeoffs, and Solution Costs (Trade-Offs)

#### Shifting Load to the Transport Layer and Storage
The transition to a Stateless architecture completely eliminated the RAM utilization issue within the application server itself (since we no longer store session state in the instance's RAM), but logically shifted this task to the storage layer. Now, for every user input (pressing Enter, initiating a scan), the engine performs a combination of read/write operations to the session repository.

This is precisely why using classic document-oriented MongoDB seems less optimal for a hot UI state. As the benchmarks above show, the driver overhead of BSON parsing and constant full document rewrites creates significantly more memory allocations and takes longer to complete.

Switching to an in-memory Redis Hash structure in this scenario appears to be a much more suitable and efficient solution. The numbers in the benchmark section clearly confirm that targeted hash field management can significantly reduce operation execution time and the load on the Garbage Collector.

#### Fun Math: When Will Allocations Gobble Up Gigabytes?
Nevertheless, even the optimized figure of **63.32 KB** for one full session cycle (creation, transitions, buffer write, and deletion) is a significant allocation on the .NET service side within a single transaction. For fun, we can estimate a hypothetical workload that would start consuming gigabytes of memory and evaluate whether the engine and Redis can handle it.

* **Service Memory Consumption (.NET Heap Allocations):**
Assuming the application processes a constant load of 1,000 RPS (requests per second), the total amount of memory allocated in the heap would be: 1,000 requests * 63.32 KB = 63.32 MB per second.
In one minute of continuous operation, the Garbage Collector would be forced to process and recycle approximately 3.6 GB of garbage. In reality, this is quite manageable for a modern CLR within the Gen 0 generation, but on weaker cloud virtual machines, this could lead to increased latency due to garbage collection pauses.

* **RAM consumption in Redis (In-Memory Storage):**
Redis itself stores data in compressed binary form, and the net hash size of one active session is only about 5–8 KB (since the `uint[]` buffer is converted to a compact string, and screens contain minimal data). With 10,000 concurrent users (concurrent sessions), Redis will occupy RAM: `10,000 sessions * 8 KB = 80,000 KB`.
A single-threaded Redis on a single CPU core will handle this load effortlessly, without even noticing the network traffic, since its performance limit for such operations is usually limited by the network card's bandwidth, not the CPU or RAM.

Therefore, the bottleneck as the abstract load increases will be the intensity of allocations in C# during JSON serialization, not the performance of Redis itself. The current engine performance provides a huge safety margin, but as a next step in optimization, it would be interesting to explore the use of ArrayPool<uint> for the frame buffer or custom compact binary serializers.

#### Historical context: why not WebSockets + HTML/JS
This project is an attempt to rethink the real-world background of large legacy WMS systems that emerged 15-20 years ago. Back then, there were no modern web stacks or powerful processors inside data collection terminals (DCTs). The hardware was weak, and warehouse Wi-Fi was poor.

Under these conditions, a custom-written server-driven UI, returning ready-made pixel matrices to the client, was the only way to make the system work. The client application on the mobile device remained "dumb" and as lightweight as possible; it didn't waste battery power parsing heavy HTML/JS and UI logic, while the server handled all the work (focus, validation, widget rendering). If the connection was lost, the "dumb" client instantly restored the image from the same point.

In modern development of new systems from scratch, using TUI matrices is foolish when Flutter, WebSockets, or gRPC are available. However, as a tool for evolution and rescuing existing legacy code, this approach is viable.

#### "Bicycle Building" (Bit Packing and a Custom Protocol)
Bitwise compression of color, symbols, and flags into a single `uint` primitive, as well as the development of a hybrid adaptive response builder, are attempts to squeeze maximum performance out of the inherently imperfect architecture of text-based JSON.

Instead of completely changing the data exchange format to binary (for example, Protocol Buffers), the system attempts to balance this within the text-based protocol. This is a classic tradeoff between readability (JSON is easy to debug in web interfaces) and traffic density at the network level.

#### Applicability in Real Business
Let's look at things pragmatically: implementing such an engine head-on in a large warehouse or production facility is practically impossible. Existing business processes and the codebase of legacy systems are so vast and intertwined that rewriting them for Stateless Rails would require colossal budgets and carry enormous risks of process shutdowns.

Therefore, `PixelTerminalUI` should be viewed not as a finished product for immediate production migration, but as a proof of concept (PoC). The project proves that even old, seemingly dead-end server-driven UI architectures can be significantly optimized for memory and bandwidth by applying modern .NET platform patterns (`ArrayPool`, `Span<T>`, bitmasks, and hybrid frame delivery).

## Afterword: Why reinvent the wheel?

Honestly, for me, `pixel-terminal-ui` is just another engineering sandbox. Before this, I had hundreds of other projects and thousands of commits, many of which now seem raw and naive to me. I fully understand that in two or three years, I'll look at this stateless engine and perhaps feel a little uneasy about some of the decisions I made. But that's precisely the essence of professional growth.

This project is a consolidation of my current understanding of distributed systems architecture and memory limitations. It's a tangible bridge from how I thought yesterday to how I will design complex systems tomorrow.
