# 🌐 The Lost Grid: Cyberpunk TUI RPG Engine

[English](README.md) | [Русский](README.ru.md)

Welcome to **The Lost Grid**, a hardcore text-based cyberpunk adventure running completely stateless over the PixelTerminalUI network wire protocol.

### 📝 The Lore & Story
You are an **Operator**—a rogue cyber-archaeologist who just woke up inside the frozen directory of a long-abandoned, military-grade corporate mainframe known as *The Grid*. Your physical body is safely plugged into a cyber-deck somewhere in the neon slums, but your consciousness is trapped inside these low-level character sectors. 

The mainframe is dying, leaking corrupted data packets and guarded by rogue sub-routines. Your only objective is to navigate through dark subnet sectors, hack offline terminals, bypass encrypted firewalls, and extract the missing encrypted Data Cores before the system executes a hard zero-wipe format.

### 🕹️ Core Gameplay Mechanics
* **Character Matrix Initialization:** Punch in your custom Operator Name and security cryptographic passkeys to securely commit your character sheet snapshot inside the MongoDB persistence layer.
* **Sector Subnet Exploration:** Use lightweight text directional controls (`-n` for forward loops, `-b` to retreat out into parent node rooms) to safely navigate through non-linear terminal nodes matrices.
* **Terminal Intrusion Sub-routines:** Trigger active terminal hacking sub-flows where fields enforce rigid `Required = true` security boundaries. 
* **State-Machine Countermeasures:** Mainframe firewalls track your decryption attempts. Every failed crack mutates your bitwise `RawState` inside the session document. Run out of attempts, and the pipeline will mutate the session, erase the current form, and forcibly teleport the player to the `SecurityQuarantineScreen` penalty screen (isolation). This is a deep network sector (a penal battalion), from which the player will have to escape through a series of complex, routine log clearing commands before returning to the main story.

### 🖥️ What the game looks like on the TUI screen (Visual Snapshots)

Snapshot 1: Main tactical screen (`SectorNavigationScreen`)
```
|--------------------------------------|
| SYSTEM: THE LOST GRID   [SEC: B-09]  | <- Simple TextWidget
| STATUS: OPERATOR CONSCIOUSNESS [94%] | <- Simple TextWidget
|--------------------------------------|
| DESCRIPTION:                         |
| You are inside a dark subnet node.   | <- Multiline TextWidget
| An offline databank glows nearby.    |
|                                      |
| CHOOSE NEXT ACTION CODE:             |
| [1] Move Forward  [2] Access Memory  | <- Simple TextWidget
|                                      |
| ACTION: [.....]                      | <- Active TextEntryWidget (Focus)
| PROTOCOL: [     ]                    | <- Inactive TextEntryWidget
|                                      |
| ENTER ACTION CODE FROM THE LIST      | <- Global UPPERCASE Hint
|--------------------------------------|
```

Screenshot 2: Hack screen (`TerminalHackScreen`)
```
|--------------------------------------|
| SECURITY INTERCEPT:COGNITIVE FIREWALL|
| ATTEMPTS REMAINING: ***              | <- Masked Life Bar (3 attempts)
|--------------------------------------|
| DECRYPTION MATRIX LOG:               |
| 0x4F2A: [CORRUPTED]                  |
| 0x9A1B: SYNC_ERROR                   |
|                                      |
| ENTER BYPASS ENCRYPTION KEYWORD:     |
| KEYWORD: [.....]                     | <- Active TextEntryWidget (Focus)
|                                      |
|                                      |
|                                      |
| REQUIRED: SCAN COMPATIBLE VECTOR KEY | <- Global UPPERCASE Hint
|--------------------------------------|
```

### Storyline, Gameplay, and Ending of "The Lost Grid"

The game revolves around tactical resource management and detective exploration of a digital labyrinth. The player doesn't simply "guess codes" (that would be boring, like brute-forcing). They collect clues scattered across different sectors of the subnet, and the Focus Manager and navigation pipeline assist them in this.

#### Characters

**1. Hacker**

A master of the virtual landscape, capable of piercing through the most brutal corporate firewalls and Intrusion Countermeasure Electronics (ICE). Cybernetically hardwired into the Net, the Hacker perceives data streams as a tangible digital reality. Gameplay-wise, the Hacker excels at bypassing mainframe defenses, decrypting classified databanks, and disabling security nodes through core matrix bruteforcing.

**2. Rigger**

A hardware specialist whose consciousness literally fuses with machinery, tactical drones, and smart-city infrastructure. Utilizing a dedicated Rigger Command Console (RCC), they project their sensory perception directly into vehicle circuits, operating them like extension of their own body. In the game, the Rigger coordinates field reconnaissance via recon probes, hijacks automated turrets, and scans physical sectors for vehicular and electronic signatures.

#### 🗺️ Approximate plot flow and twists:

Step 1: `CharacterCreationScreen` (Neurolink)
- Action: The player registers the Operator name and password.
- Plot start: The screen greets them with the words `OPERATOR CONSCIOUSNESS SYNCHRONIZED`. The mainframe believes them to be a legitimate Apex Corp employee who has come to clear the memory before shutting down the facility.

Step 2: `SectorNavigationScreen` (Subnet Hub)
- Action: The form contains three controls. The player switches focus between them. The first control is "Scan ports," the second is "View remaining power cells." They select scan and find two nodes: Archive-Node and Security-Gate.

Step 3: `ArchiveScreen` (Intelligence Gathering)
- Action: The player goes to the archive form. Using `ScrollMessageScreen`, they read old employee logs. Focus jumps to a hidden field. By pressing Enter, the player finds a text fragment: "...the password for the main terminal is linked to the AI ​​launch date..." This is the first clue! The player presses `-b`, and focus smoothly returns to the Hub.

Step 4: `TerminalHackScreen` (Hack Point and Plot Twist!)
- Action: The player goes to Security-Gate. The firewall prompts for a passphrase. The player matches the clues from the archive. He has 3 attempts (`RawState = 3`).
- Turn: If the player fails, the team's state machine decrements the counter. On the final attempt, the mainframe realizes the Operator is a spy. An alarm is triggered.

Step 5: `SecurityQuarantineScreen` (Quarantine/Penal Battalion)
- Action: If the hack fails, the pipeline immediately wipes the game screen and teleports the player to an isolated quarantine sector. The `PurgeLogsCommand` command forces the player to manually enter service directives (`FLUSH`), fending off incoming security programs. After escaping quarantine, the player returns to the Hub, but with 20% health (the `Operator Consciousness` parameter).

Step 6: `CoreDataScreen` (Final)
- Action: The player returns, enters the correct key, and hacks the terminal. The mainframe opens access to the Central Core. - Connect: The backend replaces the form with the final victory screen, where a running line displays the corporation's secret file, for which the hack was started, and the flag `TerminateSession = true` disables the terminal, successfully completing the quest.

#### ⏱️ How to implement a "Ticking Timer" without a jumping cursor?

To prevent the user's cursor from jumping and the screen from redrawing every second (which looks terrible in the terminal console), we move time control to the backend using the request sending time:
- The `DateTime? ActiveStepStartedAt` field is added to the session document in MongoDB.
- When the player enters the dangerous `TerminalHackScreen` form, the backend records the current time: `screen.ActiveStepStartedAt = DateTime.UtcNow;`.
- The player has, for example, 30 seconds to solve the riddle. The player thinks, enters the code word, and presses `Enter`.
- The `RequestPipelineHandler` pipeline wakes up, loads the form, and first checks the time difference:
```csharp
TimeSpan elapsed = DateTime.UtcNow - screen.ActiveStepStartedAt.Value;
if (elapsed.TotalSeconds > 30)
{
    // The player took too long to think! The defense systems were activated automatically.
    SimpleMessageForm alarmForm = BuildErrorNotificationForm(request, form, "TIMEOUT ERROR: SECURITY TRACE COMPLETED!");
    
    // We force teleport to Quarantine
    return NavigateToQuarantine(alarmForm); 
}
```

**Suspense Effect**: The player doesn't physically see the timer on the screen, but they know the system is tracking them. This creates a powerful psychological effect of hidden threat (like in horror games), where the price of long deliberation is the failure of the operation.

#### 🎲 Procedural Hint Generation and Nonlinearity

To prevent the game from being a one-time use game, passwords and hints should be generated randomly at the start of the session (Cold Start).
- We add the string fields `GeneratedPassword` and `ClueLocation` to the `UserSessionDocument` class (or command state).
- During Cold Start, the backend selects a random word from the dictionary (e.g. `MATRIX`, `REBOOT`, `CYPHER`) and a random room for the hint.
When a player enters the `ArchiveScreen`, the renderer or command dynamically populates the TextWidget.Value with the tooltip text generated specifically for this session. The screen template is the same, but its content is completely unique for each player.
