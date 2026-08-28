# Scenario System

This document explains how Alliance scenarios are authored, loaded and played, and how the internal runtime structure is organized.

The Scenario game mode lets creators build multi-act scripted missions with custom objectives, spawn rules, events, victory logic and transitions. Scenarios are stored as XML files and can be shipped by Alliance or by any enabled multiplayer module.

## Source-of-truth files

| Area | Main files |
|---|---|
| Data model | `Alliance.Common/GameModes/Story/Models/Scenario.cs`, `Act.cs`, `SpawnLogic.cs`, `VictoryLogic.cs`, `ConditionalActionStruct.cs` |
| Serialization | `Alliance.Common/GameModes/Story/Utilities/ScenarioSerializer.cs` |
| Runtime manager | `Alliance.Common/GameModes/Story/ScenarioManager.cs`, `Alliance.Server/GameModes/Story/ScenarioManagerServer.cs`, `Alliance.Client/GameModes/Story/ScenarioPlayer.cs` |
| Game mode behaviors | `Alliance.Server/GameModes/Story/ScenarioGameMode.cs`, `Alliance.Client/GameModes/Story/ScenarioGameMode.cs` |
| Server state machine | `Alliance.Server/GameModes/Story/Behaviors/ScenarioBehavior.cs` |
| Spawn system | `Alliance.Common/GameModes/Story/Models/SpawnLogic.cs`, `Alliance.Server/GameModes/Story/Behaviors/ScenarioSpawningBehavior.cs`, `Alliance.Server/GameModes/Story/Behaviors/SpawningStrategy/SpawningStrategyBase.cs`, `Alliance.Server/GameModes/Story/Behaviors/ScenarioDefaultSpawnFrameBehavior.cs` |
| Objectives, actions, conditions | `Alliance.Common/GameModes/Story/Objectives/`, `Alliance.Common/GameModes/Story/Actions/`, `Alliance.Common/GameModes/Story/Conditions/` |
| Client synchronization | `Alliance.Client/GameModes/Story/Handlers/StoryHandler.cs`, `Alliance.Common/GameModes/Story/NetworkMessages/` |
| Editor | `Alliance.Editor/GameModes/Story/`, `Alliance.Editor/SubModule.cs` |
| Bundled scenarios | `Alliance.Common/_Module/Scenarios/` |

## Scenario file discovery

At startup, client and server initialize their scenario managers:

- server: `ScenarioManagerServer.Initialize()`;
- client: `ScenarioPlayer.Initialize()`;
- editor: `ScenarioManager.Instance = new ScenarioManager()`.

`ScenarioManager.RefreshAvailableScenarios()` scans every selected Bannerlord multiplayer module and loads all XML files found in a `Scenarios/` folder:

```text
<EnabledModule>/Scenarios/*.xml
```

For Alliance-shipped scenarios, the source folder is:

```text
Alliance.Common/_Module/Scenarios/
```

Because both client and server resolve a scenario by its XML `Id`, custom scenarios must be available on both sides through an enabled module. The filename is not authoritative, but the editor saves files as `<ScenarioName>_<ScenarioId>.xml` to keep them unique and readable.

## Runtime flow

### 1. Start request

A scenario can be started in several ways:

- from the game mode menu / admin flow, through `ScenarioPollRequestMessage` handled by `Alliance.Server/Extensions/GameModeMenu/Handlers/PollHandler.cs`;
- from scenario logic itself, through `StartScenarioAction`;
- from the native intermission vote flow, which can restart a selected scenario act;
- from code by calling `ScenarioManager.Instance.StartScenario(...)`.

The server selects the `Scenario` and `Act` by `Scenario.Id` and act index, then calls `ScenarioManagerServer.StartScenario(...)`.

### 2. Mission selection and map loading

For the selected act, `ScenarioManagerServer` either starts a new mission or reuses the current one:

- a new mission is started if there is no current mission, if `Act.LoadMap` is enabled, or if the current map differs from `Act.MapID`;
- otherwise, the act settings are applied to the current mission and `ScenarioBehavior.ResetState()` restarts the act state machine.

The act map is stored in `Act.MapID` and copied into the native `Map` option before starting the mission.

### 3. Server state machine

`ScenarioBehavior` drives each act through the following `ActState` values:

| State | Purpose |
|---|---|
| `AwaitingPlayerJoin` | Wait until enough synchronized players have loaded. |
| `SpawningParticipants` | Start the spawn session and wait for initial spawning to finish. |
| `InProgress` | Run objectives and scripted events. |
| `DisplayingResults` | Display victory results and execute `ActionsOnDisplayResults`. |
| `Completed` | Execute delayed completion logic through `ActionsOnActCompleted`, then stop automatic state changes. |

State transitions are synchronized to clients with `UpdateScenarioMessage`. The current scenario and act are synchronized with `InitScenarioMessage`.

### 4. Client synchronization

`Alliance.Client/GameModes/Story/Handlers/StoryHandler.cs` registers mission-scoped handlers for scenario messages:

- `InitScenarioMessage` selects the same local `Scenario` and `Act` on the client;
- `UpdateScenarioMessage` updates the client act state and timer;
- `ObjectivesProgressMessage` synchronizes objective counters and mission timer data;
- `SyncScenarioLivesMessage` updates remaining lives in the client scenario state.

## Data model

### `Scenario`

A scenario is the top-level XML root.

| Field | Purpose |
|---|---|
| `Id` | Stable unique identifier used by client/server synchronization. |
| `Version`, `LastEditedAt`, `LastEditedBy` | Editor metadata updated on save. |
| `Name`, `Description` | Localized text displayed to players and tools. |
| `Acts` | Ordered list of playable scenario chapters. |

### `Act`

Each act defines one playable chapter.

| Field | Purpose |
|---|---|
| `Name`, `Description` | Localized act presentation text. |
| `LoadMap`, `MapID` | Controls whether the act loads a map and which map is used. |
| `ActSettings` | Native and Alliance settings applied for this act. |
| `SpawnLogic` | Player/bot spawn, lives, respawn, officer and spawn-location rules. |
| `Objectives` | Conditions that determine act victory. |
| `ConditionalActions` | Scripted events checked while the act is running. |
| `VictoryLogic` | Actions triggered on result display and after act completion. |

### `ActSettings`

Scenario acts use `ScenarioGameModeSettings`, which derives from `GameModeSettings` and sets the game type to `Scenario`.

The settings contain:

- `TWOptions`: TaleWorlds multiplayer options such as map, cultures, bot counts, friendly fire and round timers;
- `ModOptions`: Alliance configuration values allowed for scenarios, such as custom body, formation settings, bot difficulty, UI flags and combat multipliers.

### `SpawnLogic`

`SpawnLogic` defines how players and AI enter the act.

| Field group | Purpose |
|---|---|
| `PlayerSpawnMenu` | Optional team/formation/character selection menu. If configured, it is broadcast to clients at spawn session start. |
| `DefaultCharacterAttacker`, `DefaultCharacterDefender` | Fallback characters when no player spawn menu assignment is available. |
| `OfficerSelectionStrategy` | `NoOfficer`, `RandomOfficer` or `PlayerVote`. |
| `TimeBeforeSpawn`, `TimeBeforeRespawn` | Initial spawn preparation delay and respawn delay. |
| `DefaultSpawnTagAttacker`, `DefaultSpawnTagDefender` | Scene tags used to select spawn points for each side. Empty tags fall back to `attacker` and `defender`. |
| `LocationStrategyAttacker`, `LocationStrategyDefender` | Spawn location strategy: `OnlyTags`, `OnlyFlags`, `TagsThenFlags` or `PlayerChoice`. |
| `RespawnStrategyAttacker`, `RespawnStrategyDefender` | Respawn policy: `NoRespawn`, `MaxLivesPerTeam` or `MaxLivesPerPlayer`. |
| `MaxLivesAttacker`, `MaxLivesDefender` | Lives/tickets used by the selected respawn strategy. |
| `KeepLivesFromPreviousAct` | Adds the new act lives to remaining lives instead of resetting them. |

Spawn points are read from scene entities tagged `spawnpoint`. Additional tags select a side or custom spawn group. Parent zones tagged `starting` are preferred for initial spawn. Spawn points tagged `exclude_mounted` are penalized for mounted agents.

## Objectives and victory

All objectives derive from `ObjectiveBase` and share these fields:

| Field | Purpose |
|---|---|
| `Side` | Side that owns and must complete the objective. |
| `Name`, `Description` | Localized objective text. |
| `Optional` | Optional objectives do not count toward the required objective set. Do not make every objective optional, or the owning side can win immediately. |
| `InstantActWin` | Completing this objective immediately wins the act for its side. |
| `Active` | Disabled when the objective has been completed. |

Available objective types:

| Objective | Purpose |
|---|---|
| `KillAllObjective` | Wins when all enemy agents for the target side have been killed. |
| `KillCountObjective` | Wins when a configured kill count is reached. |
| `CaptureObjective` | Wins when a `CS_CapturableZone` with a matching ID is owned by the objective side. |
| `TimerObjective` | Wins after a configured duration. |

`ScenarioManager.CheckObjectives()` evaluates active objectives by side. A side wins if an instant-win objective completes, or if all non-optional objectives for that side are completed.

## Scripted events

Scripted events are represented by `ConditionalActionStruct`:

| Field | Purpose |
|---|---|
| `Name` | Editor-facing name. |
| `Conditions` | All conditions must evaluate to true. |
| `Actions` | Actions executed when conditions are met. |
| `Enabled` | Enables or disables the event. |
| `OneTimeOnly` | Disables the event after the first successful execution. |
| `RefreshDelay` | Delay between condition checks; higher values are better for performance. |

Act-level `ConditionalActions` are registered through `Act.RegisterObjectives()` together with objectives, then ticked while the act state is after `SpawningParticipants`.

Map makers can also place `AL_TriggerAction` on a scene entity. This script stores a compressed serialized `ConditionalActionStruct` in editor-safe chunks and executes it independently of act-level events. It is useful for local map interactions such as traps, toggles, sounds or object-triggered logic.

## Conditions

Available condition types:

| Condition | Purpose |
|---|---|
| `AgentDeathCondition` | Checks deaths of a configured character, with optional repeat behavior. |
| `AgentEnteredZoneCondition` | Checks whether matching agents entered a `SerializableZone`; can filter by side, target type and count. |
| `ObjectUsedCondition` | Listens to `CS_UsableObject` usage by object ID, optionally restricted to the parent entity hierarchy. |
| `TimerCondition` | Becomes true after a delay, optionally repeating at an interval. |
| `VictoryCondition` | Checks the current act winner; useful in victory/completion action branches. |

Shared condition enums include:

- `TargetType`: `All`, `Bots`, `Players`, `Officers`;
- `SideType`: `All`, `Defender`, `Attacker`;
- `MoveOrderType`: `Charge`, `Move`, `Retreat`, `Stop`, `Advance`, `FallBack`.

## Actions

Actions derive from `ActionBase`. Some actions have common behavior, while others are replaced with client-side or server-side implementations during deserialization through `ActionFactory`.

| Action | Purpose |
|---|---|
| `ConditionalAction` | Executes one list of actions when conditions are true, otherwise another list; supports delay for true actions. |
| `ShowMessageAction` | Displays localized information to the client. |
| `ShowResultScreenAction` | Displays win/loss result text; implemented on the client by `Client_ShowResultScreenAction`. |
| `StartGameAction` | Starts another game mode/map from the server. |
| `StartScenarioAction` | Starts a scenario act by scenario ID and zero-based act index; an empty scenario ID reuses the current scenario. |
| `EndScenarioAction` | Ends the scenario flow and starts the configured post-match transition. |
| `SpawnAgentAction` | Spawns one or more agents of a character at a zone. |
| `SpawnFormationAction` | Spawns a configured formation with movement and arrangement orders. |
| `DamageAgentInZoneAction` | Damages matching agents in a zone. |
| `MortalityStateZoneAction` | Changes mortality state for matching agents in a zone. |
| `ShowOrHideEntitiesAction` | Shows, hides or toggles entities by tag, optionally restricted to a parent entity. |
| `TeleportAgentAction` | Teleports matching agents from one zone to another. |
| `VOIPRangeInZoneAction` | Changes VOIP range for matching agents in a zone. |
| `PlaySoundAction` | Plays a sound or music event by name/category. |

When adding a new action type in code, add it to `ActionFactory` and to the client/server factories when it needs side-specific behavior.

## Creating a custom playable scenario

### 1. Prepare the map

In the Bannerlord editor, ensure the scene contains the entities required by the scenario:

- `spawnpoint` entities for spawning;
- side tags such as `attacker` and `defender`, or custom tags matching the act `SpawnLogic`;
- optional parent spawn zones tagged `starting` for initial spawn preference;
- optional `exclude_mounted` tag for spawn points that should be avoided by mounted agents;
- `CS_CapturableZone` entities when using `CaptureObjective`;
- `CS_UsableObject` entities when using `ObjectUsedCondition`;
- optional `AL_TriggerAction` scripts for map-local conditional actions.

### 2. Open the scenario editor

With `Alliance.Editor` loaded, press `LeftCtrl + P` to open the scenario editor. The editor creates a default scenario with one act and can load/save scenario XML files.

The editor updates these metadata fields on save:

- `Version`;
- `LastEditedAt`;
- `LastEditedBy`.

### 3. Configure scenario metadata

Set the scenario `Name` and `Description`, then keep the generated `Id` stable once the scenario is shared. Client/server synchronization depends on this ID.

### 4. Configure each act

For each act:

1. Set `Name` and `Description`.
2. Set `LoadMap` and `MapID` if the act should load a map.
3. Configure `ActSettings`, especially `Map`, `CultureTeam1`, `CultureTeam2`, bot counts and timers.
4. Configure `SpawnLogic` for player spawn menu, default characters, lives, respawn and spawn location strategies.
5. Add objectives for each side.
6. Add `ConditionalActions` for scripted events during the act.
7. Configure `VictoryLogic`:
   - `ActionsOnDisplayResults` usually contains `ShowResultScreenAction`;
   - `ActionsOnActCompleted` should transition to the next act, another game mode, lobby, or `EndScenarioAction`.

### 5. Save and deploy the XML

Save the XML into a module-level `Scenarios/` folder:

```text
<YourModule>/Scenarios/My Scenario_abcd1234.xml
```

For scenarios bundled with Alliance, place source XML files under:

```text
Alliance.Common/_Module/Scenarios/
```

After build/deployment, ensure both the dedicated server and clients have the module containing the scenario enabled.

### 6. Start and test

Start the scenario from the Alliance game mode menu as an admin, or chain it from another act with `StartScenarioAction`.

Test at least:

- server and client both find the scenario ID;
- all referenced maps exist on server and client;
- spawn tags resolve for attacker and defender;
- objectives can be completed by the expected side;
- victory actions transition correctly;
- clients joining late receive `InitScenarioMessage`, objective progress and remaining lives correctly.

## Minimal XML shape

Scenario XML is generated by `ScenarioSerializer`, but the root structure is:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Scenario xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Id>abcd1234</Id>
  <Version>1</Version>
  <Name>
    <Entry language="English">My Scenario</Entry>
  </Name>
  <Description>
    <Entry language="English">A short player-facing description.</Entry>
  </Description>
  <Acts>
    <Act>
      <Name>
        <Entry language="English">Act 1</Entry>
      </Name>
      <LoadMap>true</LoadMap>
      <MapID>my_map_id</MapID>
      <ActSettings>...</ActSettings>
      <SpawnLogic>...</SpawnLogic>
      <Objectives>...</Objectives>
      <ConditionalActions>...</ConditionalActions>
      <VictoryLogic>
        <ActionsOnDisplayResults>
          <ActionBase xsi:type="ShowResultScreenAction">...</ActionBase>
        </ActionsOnDisplayResults>
        <ActionsOnActCompleted>
          <ActionBase xsi:type="EndScenarioAction" />
        </ActionsOnActCompleted>
      </VictoryLogic>
    </Act>
  </Acts>
</Scenario>
```

Prefer using the editor or generated examples as templates instead of hand-writing large XML files.

## Best practices and pitfalls

- Keep `Scenario.Id` stable after release; changing it breaks start requests and client synchronization for existing references.
- Keep scenario XML present on both server and clients through enabled modules.
- Use `EndScenarioAction` when a scenario should respect the configured post-match transition, including native intermission voting.
- Use `StartScenarioAction` for act chaining; remember that `ActIndex` is zero-based.
- Avoid making every objective optional for a side.
- Prefer higher `RefreshDelay` values on expensive conditional actions.
- Validate all scene entity IDs/tags referenced by objectives, conditions and actions.
- Use `LoadMap = false` only when the next act is meant to continue on the current scene.

## Extending the scenario system in code

When adding new scenario building blocks:

1. Add common serializable data to `Alliance.Common/GameModes/Story/`.
2. Decorate editor-facing fields with `[ConfigProperty]` where appropriate.
3. For actions, implement common state in `Alliance.Common/GameModes/Story/Actions/`.
4. If the action needs runtime behavior only on server or client, add the concrete implementation under the matching project and register it in `Server_ActionFactory` or `Client_ActionFactory`.
5. Keep networked behavior server-authoritative; synchronize clients with explicit messages if UI or state must update.
6. Test XML serialization/deserialization with existing scenario files.



