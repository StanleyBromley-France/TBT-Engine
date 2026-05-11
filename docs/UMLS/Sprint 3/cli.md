# cli

```mermaid
classDiagram
    direction LR

    %% ============================================================
    %% CLI ENTRYPOINT & OPTIONS
    %% ============================================================

    class CliApp {
        +Run(CliOptions options)
    }

    class CliOptions {
        +string InputPath
        +string OutputPath
        +bool UseStdIn
        +bool UseStdOut
        +bool BatchMode
    }

    %% ============================================================
    %% SERIALIZERS
    %% ============================================================

    class IGameStateSerializer {
        <<interface>>
        +GameState Load(string source)
        +void Save(GameState state, string destination)
    }

    class IScenarioConfigSerializer {
        <<interface>>
        +ScenarioConfig Load(string source)
        +void Save(ScenarioConfig scenario, string destination)
    }

    %% ============================================================
    %% SETUP LAYER (USED BY CLI)
    %% ============================================================

    class GameSetup {
        +GameState BuildInitialGameState(ScenarioConfig scenario)
    }

    class ScenarioConfig

    %% ============================================================
    %% CLI ENGINE ADAPTER
    %% ============================================================

    class CliEngineAdapter {
        -IEngineFacade _engine

        +CliEngineAdapter(IEngineFacade engine)

        %% high-level operations for CLI
        +IReadOnlyGameState GetState()
        +void LoadState(GameState state)
        +ActionChoice SuggestActionForActive()
        +void Apply(ActionChoice action)
    }

    %% ============================================================
    %% EXTERNAL DOMAIN TYPES (DECLARED ELSEWHERE)
    %% ============================================================

    class GameState
    class IReadOnlyGameState
    class ActionChoice

    class IEngineFacade {
        <<interface>>
        +void LoadState(GameState state)
        +IReadOnlyGameState GetState()
        +ActionChoice SuggestActionForActive()
        +void ApplyAction(ActionChoice action)
        +MatchLog GetMatchLog()
        +void ResetMatchLog()
    }

    class ITurnPolicy {
        <<interface>>
        +ActionChoice ChooseAction(IReadOnlyGameState state)
    }

    class MatchLog

    %% ============================================================
    %% RELATIONSHIPS
    %% ============================================================

    %% CLI app wiring
    CliApp ..> CliOptions
    CliApp ..> IScenarioConfigSerializer
    CliApp ..> GameSetup
    CliApp ..> IGameStateSerializer
    CliApp ..> CliEngineAdapter

    %% Serializers use domain types
    IGameStateSerializer ..> GameState
    IScenarioConfigSerializer ..> ScenarioConfig

    %% Setup uses scenario & produces GameState
    GameSetup ..> ScenarioConfig
    GameSetup ..> GameState

    %% Adapter uses engine and domain types
    CliEngineAdapter ..> IEngineFacade
    CliEngineAdapter ..> IReadOnlyGameState
    CliEngineAdapter ..> GameState
    CliEngineAdapter ..> ActionChoice

    %% Engine facade depends on turn policy
    IEngineFacade ..> ITurnPolicy
    IEngineFacade ..> MatchLog

```
