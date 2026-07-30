# Alliance – AI Agent Guide

## Project Overview
Alliance is a multiplayer mod for **Mount & Blade II: Bannerlord** (TaleWorlds engine).  
It is a C# solution with five projects that map to distinct Bannerlord deployment targets.

| Project | Target | Role |
|---|---|---|
| `Alliance.Common` | `net472` + `net6.0` | Shared code: config, network messages, base behaviors, extensions |
| `Alliance.Client` | `net472` | Client-only: views, UI, client-side behaviors |
| `Alliance.Server` | `net6.0` | Dedicated server: server behaviors, admin, persistence |
| `Alliance.Editor` | `net472` | Bannerlord editor tools |
| `Alliance.SP` | `net472` | Singleplayer entry point |

## Build System
- **Prerequisite**: Set environment variable `BANNERLORD_GAME_DIR` to your Bannerlord installation path before building.
- Build any project via `dotnet build` or Visual Studio. The post-build event automatically copies DLLs and module assets to `%BANNERLORD_GAME_DIR%/Modules/Alliance/`.
- `Alliance.Common` compiles twice: `net472` (client) and `net6.0` (server). The constant `SERVER` is defined only for `net6.0`, enabling `#if SERVER` guards in shared code.
- Version is centralized in `Alliance.Common/CommonProps.props` (`AllianceVersion`). `SubModule.xml` version is updated automatically on build.
- To decompile TaleWorlds source for reference: run `Alliance.Common/TW_References/Decompile.bat` (requires ILSpy via `Install ILSpy.bat`). Output lands in `TW_References/BannerlordSource/`.

## Adding a New Game Mode
Every game mode is registered symmetrically on Client and Server. Follow this checklist:
1. **Common**: Add a folder under `Alliance.Common/GameModes/<Name>/` with `Behaviors/`, `NetworkMessages/FromServer/`, `NetworkMessages/FromClient/`, and `Models/`.
2. **Client**: Add `Alliance.Client/GameModes/<Name>/` with `<Name>GameMode.cs`, views, and a `Handlers/` folder for handler implementations (`IHandlerRegister` by default; `IGlobalHandlerRegister` only when the handler must outlive the mission).
3. **Server**: Add `Alliance.Server/GameModes/<Name>/` with the same structure plus `Behaviors/` server logic.
4. **Registration**: Register the game mode in both `Alliance.Client/SubModule.cs` and `Alliance.Server/SubModule.cs`:
   ```csharp
   Module.CurrentModule.AddMultiplayerGameMode(new MyGameMode("MyModeName"));
   ```
   The string name must match on both sides.
5. **Default behaviors**: Use `DefaultClientBehaviors.GetDefaultBehaviors(scoreboardData)` / `DefaultServerBehaviors.GetDefaultBehaviors(scoreboardData)` as base lists, then `.AppendList(...)` custom behaviors.

## Scenario System
The complete contributor documentation for creating custom playable scenarios and understanding the internal runtime flow is in `docs/scenario-system.md`.

Key implementation points:
- Scenario XML files are discovered from every enabled module's `Scenarios/` folder by `ScenarioManager.RefreshAvailableScenarios()`.
- `Scenario` and `Act` data live under `Alliance.Common/GameModes/Story/Models/`; XML serialization is handled by `ScenarioSerializer`.
- Runtime ownership is server-authoritative: `ScenarioManagerServer` selects the scenario/act, starts or reuses the mission, and `ScenarioBehavior` drives the `ActState` state machine.
- Clients mirror scenario state through `StoryHandler` and the scenario network messages (`InitScenarioMessage`, `UpdateScenarioMessage`, `ObjectivesProgressMessage`, `SyncScenarioLivesMessage`).
- Scenario authors use objectives, conditions, actions, `SpawnLogic`, `VictoryLogic`, and optional scene-level `AL_TriggerAction` scripts to build gameplay.
- If a scenario should end through the configured post-match flow, use `EndScenarioAction` rather than manually starting another mode.

## Network Messages
All custom messages live in `Common/.../NetworkMessages/FromServer/` or `FromClient/`.

```csharp
[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
public sealed class MyMessage : GameNetworkMessage
{
    public int Value { get; private set; }
    public MyMessage() { }
    public MyMessage(int value) { Value = value; }

    protected override void OnWrite() =>
        WriteIntToPacket(Value, new CompressionInfo.Integer(0, 100, true));

    protected override bool OnRead()
    {
        bool valid = true;
        Value = ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref valid);
        return valid;
    }
    protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
    protected override string OnGetLogFormat() => "My message";
}
```

**Sending** (server → all clients):
```csharp
GameNetwork.BeginBroadcastModuleEvent();
GameNetwork.WriteMessage(new MyMessage(42));
GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
```

## Registering Message Handlers
Handler contracts are defined in `Alliance.Common/Extensions/IHandlerRegister.cs`. Implement them in dedicated handler classes — **not** directly on a `MissionBehavior`, because auto-discovery instantiates handler classes through reflection.

### Mission-scoped handlers
Use `IHandlerRegister` for handlers that are only needed while a mission is active. `ClientAutoHandler` / `ServerAutoHandler` discover these implementations through reflection, register them when their `MissionNetwork` behavior initializes, and unregister them when the mission behavior is removed.

```csharp
public class MyHandler : IHandlerRegister
{
    public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
    {
        reg.Register<MyMessage>(HandleMyMessage);
    }
    private void HandleMyMessage(MyMessage msg) { ... }
}
```

### Global handlers
Use `IGlobalHandlerRegister` for handlers whose lifetime must exceed the mission lifecycle. `ClientGlobalAutoHandler` / `ServerGlobalAutoHandler` discover these implementations through reflection, call `Register(...)` once per game session, and never unregister them.

This is required for flows that keep exchanging messages after mission behaviors have been removed, such as native post-match intermission voting. Keep global handlers minimal and make them tolerant of lobby/intermission state where `Mission.Current` may be unavailable.

```csharp
public class MyGlobalHandler : IGlobalHandlerRegister
{
    public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
    {
        reg.Register<MyMessage>(HandleMyMessage);
    }
    private void HandleMyMessage(MyMessage msg) { ... }
}
```

Concrete example: `Alliance.Client/Extensions/NativeIntermissionVote/Handlers/NativeIntermissionVoteHandler.cs` implements `IGlobalHandlerRegister` so it can clear stale native intermission vote items after the previous mission has already closed.

## Configuration System
- All mod settings are public fields in `Alliance.Common/Core/Configuration/Models/DefaultConfig.cs` decorated with `[ConfigProperty]`.
- Access settings via the singleton `Config.Instance.<FieldName>` (available on both client and server).
- `ConfigManager.Instance` handles validation, serialization, and network sync (`SyncConfigField`, `SendConfigToAllPeers`).

## Post-Match Intermission Voting
- The advanced config field `LoopCurrentModeWithNativeVote` (`DefaultConfig.cs`) enables the optional post-match flow that keeps the current game mode and uses TaleWorlds native intermission voting.
- Match-end code should call `GameModeStarter.Instance.StartPostMatchTransition()` instead of directly starting the lobby when it must respect the configured post-match behavior. This currently backs the Battle Royale, CaptainX match-end, and scenario `EndScenarioAction` flows.
- When enabled, `NativeIntermissionVoteService` prepares vote candidates before mission end, waits until clients return to lobby/intermission state, broadcasts native intermission messages, then restarts the selected game mode. If preparation fails, the server falls back to the existing Lobby transition.
- For regular game modes, players vote for the next map and cultures. For `Scenario`, culture voting is disabled and map vote entries represent `Scenario + Act` candidates, because several scenarios can share the same physical `MapID`.
- Scenario authors who want the new configured post-match transition must add `EndScenarioAction` to the relevant `ActionsOnActCompleted` list; existing scenarios without this action remain action-driven.
- Client-side vote item reset uses `ClearNativeIntermissionVoteItems` plus `NativeIntermissionVoteHandler`, which is a global handler because native intermission voting runs after mission-scoped handlers are unregistered.

## Harmony Patches
Patches are organized as `Patch/<DirtyXxxPatcher.cs>` (orchestrators) and `Patch/HarmonyPatch/Patch_<ClassName>.cs` (individual patches).  
Each patch class exposes a static `Patch()` method returning `bool` and uses its own `Harmony` instance keyed by `SubModule.ModuleId + nameof(PatchClass)`.  
Patches are a last resort — leave a comment explaining why a clean override was not possible.

## Logging
Import `static Alliance.Common.Utilities.Logger` and call:
```csharp
Log("Message", LogLevel.Debug);   // suppressed in Release builds
Log("Warning", LogLevel.Warning);
```
`LogLevel.Debug` logs are stripped from Release builds. On the server, output goes to the console; on the client, it appears in-game chat.

## Key Files & Directories
- `docs/scenario-system.md` – scenario authoring and internal runtime documentation
- `Alliance.Common/Core/Configuration/Models/DefaultConfig.cs` – all configurable settings
- `Alliance.Common/Extensions/IHandlerRegister.cs` – mission-scoped and global network handler registration contracts
- `Alliance.Client/Core/ClientGlobalAutoHandler.cs` / `Alliance.Server/Core/ServerGlobalAutoHandler.cs` – global handler auto-registration
- `Alliance.Server/Extensions/NativeIntermissionVote/` – server-side native intermission vote flow
- `Alliance.Client/Extensions/NativeIntermissionVote/Handlers/NativeIntermissionVoteHandler.cs` – client-side global handler for intermission vote resets
- `Alliance.Client/GameModes/DefaultClientBehaviors.cs` – default client behavior list
- `Alliance.Server/GameModes/DefaultServerBehaviors.cs` – default server behavior list
- `Alliance.Common/Patch/DirtyCommonPatcher.cs` – native compression limit overrides
- `Alliance.Common/TW_References/` – scripts to decompile TaleWorlds source for IDE reference
- `Alliance.Common/TW_References/BannerlordSource/` – decompiled source code of TaleWorlds DLLs (generated by Decompile.bat)
- `Alliance.Common/_Module/` – shared assets (GUI, ModuleData, Prefabs, Scenarios…) copied to game at build time

## Custom Agents

### `tw-native-explorer` — TaleWorlds Native Code Explorer
**Definition file**: `.github/agents/tw-native-explorer.agent.md`

This agent specializes in **searching and reading decompiled native TaleWorlds** (Bannerlord) code.  
Use it whenever you need to:
- Understand the behavior of a native TaleWorlds class/method
- Find the inheritance hierarchy of an engine type
- Identify possible overrides before writing a Harmony patch
- Explore usage patterns in the native code

**Source of truth**: `Alliance.Common/TW_References/BannerlordSource/`  
Organized by namespace/assembly (e.g. `TaleWorlds/MountAndBlade/`, `TaleWorlds/Core/`, `Sandbox/`…)

**Authorized tools**: `run_in_terminal`, `read_file`, `list_dir`  
**Disabled tools**: `grep_search`, `file_search` — `BannerlordSource/` is excluded from the workspace index (`.gitignore`), so these tools return no results. The agent uses PowerShell commands via `run_in_terminal` instead:
```powershell
Get-ChildItem -Path "<workspace>\Alliance.Common\TW_References\BannerlordSource" -Recurse -Filter "*.cs" | Select-String -Pattern "SymbolName" -List
```

**If the source code is missing or incomplete**, the agent will propose in order:
1. Install ILSpy: `Alliance.Common/TW_References/Install ILSpy.bat`
2. Decompile the DLLs: `Alliance.Common/TW_References/Decompile.bat` (requires `BANNERLORD_GAME_DIR`)
3. Generate PDB files (debug only): `Alliance.Common/TW_References/GenPDB.bat`

> ⚠️ These operations are costly — the agent never runs them without explicit user confirmation.

### `doc-maintainer` — Documentation Maintenance Agent
**Definition file**: `.github/agents/doc-maintainer.agent.md`

This agent specializes in **maintaining Markdown documentation** across the Alliance project.  
Use it whenever you need to:
- Check whether documentation is still up-to-date with the current repository state
- Validate user-provided documentation hints against code, config, project files or recent commits
- Produce a documentation maintenance report
- Update Markdown documentation files with consistent structure and formatting
- Keep agent documentation, including its own definition, aligned with project conventions

**Write scope**: Markdown documentation only:
- `*.md`
- `.github/agents/*.agent.md`

The agent may inspect code, project files, module XML files and Git history in read-only mode
to validate documentation, but it must never modify non-documentation files. If it finds
something that should change outside docs, it reports a recommendation instead.

**Documentation inventory command**:
```powershell
Get-ChildItem -Path "." -Recurse -Filter "*.md" -File |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
    ForEach-Object { $_.FullName.Substring((Get-Location).Path.Length + 1) } |
    Sort-Object
```

**Recent changes checks**:
```powershell
git --no-pager status --short
git --no-pager log --oneline -n 20
git --no-pager show --name-status --stat <commit>
```

**Mandatory behavior**: report what was checked, what was stale, what was updated, and any
non-documentation suggestions that require human or maintainer action.

