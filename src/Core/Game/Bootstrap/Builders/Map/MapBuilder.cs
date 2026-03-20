namespace Core.Game.Bootstrap.Builders.Map;

using Core.Domain.Types;
using Core.Game.Bootstrap.Contracts;
using Core.Game.Bootstrap.Creation.Map.Results;
using Core.Game.Bootstrap.Creation.Map.Rng;
using Core.Map.Grid;
using Core.Map.Pathfinding;
using Core.Map.Search;
using Core.Map.Terrain;

public sealed class MapBuilder : IMapBuilder
{
    private readonly IMapGenerationRng _rng;

    public MapBuilder(IMapGenerationRng rng)
    {
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    public MapBuildResult Build(IMapSpec mapSpec, MapBuildOptions options)
    {
        _ = mapSpec ?? throw new ArgumentNullException(nameof(mapSpec));
        _ = options ?? throw new ArgumentNullException(nameof(options));

        var tileDistribution = mapSpec.TileDistribution ?? new Dictionary<TerrainType, double>();
        var weightedTerrains = new List<(TerrainType Terrain, double Weight)>(tileDistribution.Count);
        var totalWeight = 0d;

        foreach (var (terrain, weight) in tileDistribution)
        {
            weightedTerrains.Add((terrain, weight));
            totalWeight += weight;
        }

        if (mapSpec.Width <= 0 ||
            mapSpec.Height <= 0 ||
            weightedTerrains.Count == 0 ||
            !double.IsFinite(totalWeight) ||
            totalWeight <= 0d)
        {
            throw new InvalidOperationException("Map spec failed core invariants. Ensure setup validation runs before map creation.");
        }

        var maxAttempts = Math.Max(1, options.MaxAttempts);

        var requiredCoords = options.RequiredWalkableCoords ?? Array.Empty<HexCoord>();
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var tiles = BuildTiles(
                width: mapSpec.Width,
                height: mapSpec.Height,
                seed: options.Seed,
                attempt: attempt,
                weightedTerrains: weightedTerrains,
                totalWeight: totalWeight);

            SmoothSingleTileIslands(tiles);

            var map = new Map(tiles);
            if (SatisfiesConstraints(map, requiredCoords, options.RequireAllRequiredCoordsConnected))
            {
                return new MapBuildResult(
                    map: map,
                    attemptUsed: attempt);
            }
        }

        throw new InvalidOperationException(
            $"Failed to build a valid map in {maxAttempts} attempts. " +
            $"Constraints: required walkable coords={requiredCoords.Count}, requireConnected={options.RequireAllRequiredCoordsConnected}.");
    }

    private Tile[,] BuildTiles(
        int width,
        int height,
        int seed,
        int attempt,
        IReadOnlyList<(TerrainType Terrain, double Weight)> weightedTerrains,
        double totalWeight)
    {
        var tiles = new Tile[width, height];
        var rngState = new MapGenerationRngState(seed: seed, position: attempt * 7919);

        for (var col = 0; col < width; col++)
        {
            for (var row = 0; row < height; row++)
            {
                var (value, nextState) = _rng.Next(rngState);
                rngState = nextState;

                var roll = ToUnitInterval(value) * totalWeight;
                var terrain = PickTerrain(weightedTerrains, roll);
                tiles[col, row] = new Tile { Terrain = terrain };
            }
        }

        return tiles;
    }

    private static double ToUnitInterval(int value)
    {
        var unsigned = unchecked((uint)value);
        var mixed = Mix32(unsigned);
        return mixed / (double)uint.MaxValue;
    }

    private static uint Mix32(uint value)
    {
        value ^= value >> 16;
        value *= 0x85EBCA6Bu;
        value ^= value >> 13;
        value *= 0xC2B2AE35u;
        value ^= value >> 16;
        return value;
    }

    private static TerrainType PickTerrain(
        IReadOnlyList<(TerrainType Terrain, double Weight)> weightedTerrains,
        double roll)
    {
        var cumulative = 0d;
        for (var i = 0; i < weightedTerrains.Count; i++)
        {
            var item = weightedTerrains[i];
            cumulative += item.Weight;
            if (roll <= cumulative)
                return item.Terrain;
        }

        return weightedTerrains[weightedTerrains.Count - 1].Terrain;
    }

    private static bool SatisfiesConstraints(
        Map map,
        IReadOnlyList<HexCoord> requiredCoords,
        bool requireConnected)
    {
        if (requiredCoords.Count == 0)
            return true;

        var uniqueRequired = new HashSet<HexCoord>(requiredCoords);
        foreach (var coord in uniqueRequired)
        {
            if (!map.TryGetTile(coord, out var tile) || !tile.IsWalkable)
            {
                return false;
            }
        }

        if (!requireConnected || uniqueRequired.Count <= 1)
            return true;

        return AreAllRequiredCoordsConnected(map, uniqueRequired);
    }

    private static bool AreAllRequiredCoordsConnected(
        Map map,
        HashSet<HexCoord> requiredCoords)
    {
        var start = requiredCoords.First();
        var pathfinder = new Pathfinder();
        var maxMoves = map.Width * map.Height;
        var reachable = pathfinder.GetReachable(map, start, maxMoves);

        foreach (var coord in requiredCoords)
        {
            if (!reachable.ContainsKey(coord))
                return false;
        }

        return true;
    }

    internal static void SmoothSingleTileIslands(Tile[,] tiles)
    {
        var map = new Map(tiles);
        var width = tiles.GetLength(0);
        var height = tiles.GetLength(1);
        var maxPasses = width * height;

        for (var pass = 0; pass < maxPasses; pass++)
        {
            var changes = new List<(int Col, int Row, TerrainType Terrain)>();
            for (var col = 0; col < width; col++)
            {
                for (var row = 0; row < height; row++)
                {
                    var centerTile = tiles[col, row];
                    if (centerTile is null)
                        continue;

                    var centerCoord = HexCoordConverter.FromOffset(col, row);
                    var neighbours = MapSearch.GetNeighbourCoords(map, centerCoord).ToArray();
                    if (neighbours.Length != 6)
                        continue;

                    TerrainType? surroundTerrain = null;
                    var allSame = true;
                    for (var i = 0; i < neighbours.Length; i++)
                    {
                        var (nCol, nRow) = HexCoordConverter.ToOffset(neighbours[i]);
                        var neighbourTerrain = tiles[nCol, nRow].Terrain;

                        if (surroundTerrain is null)
                        {
                            surroundTerrain = neighbourTerrain;
                        }
                        else if (surroundTerrain.Value != neighbourTerrain)
                        {
                            allSame = false;
                            break;
                        }
                    }

                    if (!allSame || surroundTerrain is null || centerTile.Terrain == surroundTerrain.Value)
                        continue;

                    changes.Add((col, row, surroundTerrain.Value));
                }
            }

            if (changes.Count == 0)
                return;

            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                tiles[change.Col, change.Row].Terrain = change.Terrain;
            }
        }
    }
}
