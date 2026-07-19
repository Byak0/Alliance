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
2. **Client**: Add `Alliance.Client/GameModes/<Name>/` with `<Name>GameMode.cs`, views, and a `Handlers/` folder for `IHandlerRegister` implementations.
3. **Server**: Add `Alliance.Server/GameModes/<Name>/` with the same structure plus `Behaviors/` server logic.
4. **Registration**: Register the game mode in both `Alliance.Client/SubModule.cs` and `Alliance.Server/SubModule.cs`:
   ```csharp
   Module.CurrentModule.AddMultiplayerGameMode(new MyGameMode("MyModeName"));
   ```
   The string name must match on both sides.
5. **Default behaviors**: Use `DefaultClientBehaviors.GetDefaultBehaviors(scoreboardData)` / `DefaultServerBehaviors.GetDefaultBehaviors(scoreboardData)` as base lists, then `.AppendList(...)` custom behaviors.

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
Implement `IHandlerRegister` (defined in `Alliance.Common/Extensions/IHandlerRegister.cs`) in a dedicated handler class — **not** directly on a `MissionBehavior`. `ClientAutoHandler` discovers all implementations via reflection at startup.

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

## Configuration System
- All mod settings are public fields in `Alliance.Common/Core/Configuration/Models/DefaultConfig.cs` decorated with `[ConfigProperty]`.
- Access settings via the singleton `Config.Instance.<FieldName>` (available on both client and server).
- `ConfigManager.Instance` handles validation, serialization, and network sync (`SyncConfigField`, `SendConfigToAllPeers`).

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
- `Alliance.Common/Core/Configuration/Models/DefaultConfig.cs` – all configurable settings
- `Alliance.Common/Extensions/IHandlerRegister.cs` – network handler registration contract
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
