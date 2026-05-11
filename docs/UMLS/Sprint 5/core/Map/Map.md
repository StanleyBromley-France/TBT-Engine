# Map

```mermaid
classDiagram
direction LR

%% ============================================================
%% MAP GRID
%%
%% Array-backed storage for a hex map plus read-only interfaces.
%%
%% - GameState owns the Map instance (match-scoped state)
%% - Map owns its Tiles (internal storage)
%% - IReadOnlyMap / IReadOnlyTile provide safe query-only access for
%%   systems that must not mutate map data (search, pathfinding, UI)
%% ============================================================


%% ------------------------------------------------------------
%% Read-only Interfaces
%% ------------------------------------------------------------
namespace Map.Grid {

  class IReadOnlyTile {
    <<interface>>
    +TerrainType Terrain
    +bool IsWalkable
  }

  class IReadOnlyMap {
    <<interface>>
    +int Width
    +int Height
    +IReadOnlyTile? GetTile(HexCoord coord)
    +bool TryGetTile(HexCoord coord, out IReadOnlyTile tile)
  }
}

%% IReadOnlyMap returns IReadOnlyTile views
IReadOnlyMap ..> IReadOnlyTile


%% ------------------------------------------------------------
%% Concrete Map Storage
%% ------------------------------------------------------------
namespace Map.Grid {

  class Map {
    +int Width
    +int Height
    -Tile[,] Tiles
    -bool IsInside(int col, int row)
    +Tile? GetTile(HexCoord coord)
    +bool TryGetTile(HexCoord coord, out Tile tile)
  }

  class Tile {
    +TerrainType Terrain
    +bool IsWalkable
  }
}

%% Map owns Tiles
Map *-- Tile

%% Map/Tile implement read-only interfaces
Map ..|> IReadOnlyMap
Tile ..|> IReadOnlyTile


%% ------------------------------------------------------------
%% Terrain Definitions + Rules
%% ------------------------------------------------------------
namespace Map.Terrain {

  class TerrainType {
    <<enum>>
    Plain
    Forest
    Mountain
    Water
  }

  class TerrainRules {
    +bool IsWalkable(TerrainType terrain)
  }
}

%% Tile terrain wiring
Tile --> TerrainType
Tile ..> TerrainRules
TerrainRules --> TerrainType


%% ------------------------------------------------------------
%% Core References
%% ------------------------------------------------------------
namespace Core.Game {
  class GameState
}

namespace Core.Types {
  class HexCoord
}

%% GameState owns Map
GameState *-- Map

%% Coordinate dependency
Map ..> HexCoord
IReadOnlyMap ..> HexCoord
```
