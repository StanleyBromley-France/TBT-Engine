# GameSession

```mermaid
classDiagram
direction LR

  %% ============================================================
  %% GAME SESSION
  %%
  %% Match-scoped data container
  %%
  %% Owned by EngineFacade
  %% Created at game setup
  %%
  %% Used by EngineFacade for orchestration and state access
  %% Used by GameMutationContext to mutate GameState during operations
  %%
  %% Owns static content (TemplateRegistry),
  %% current mutable state (GameState),
  %% and undo history (UndoHistory)
  %%
  %% Does not own orchestration logic or services
  %% ============================================================

  namespace Core.Game{
  class GameSession {
    +TemplateRegistry Templates
    +GameState State
    +UndoHistory UHistory
  }
  }

  %% ------------------------------------------------------------
  %% Referenced Stubs
  %% ------------------------------------------------------------
  namespace Core.Domain.Repositories {
    class TemplateRegistry
  }

  namespace Core.Game {
    class GameState
  }

  namespace Core.Game.Match {
    class TeamPair
  }

  namespace Core.Engine.Undo {
    class UndoHistory
  }

  namespace Core.Engine.Victory {
    class GameOutcome
  }

  namespace Core.Engine {
    class EngineFacade
  }

  %% ------------------------------------------------------------
  %% Relationships
  %% ------------------------------------------------------------
  GameSession *-- TemplateRegistry
  GameSession *-- GameState
  GameSession *-- TeamPair
  GameSession *-- UndoHistory
  GameSession --> GameOutcome

  EngineFacade o-- GameSession
```
