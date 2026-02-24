namespace Core.Map.Pathfinding;

using Core.Map.Search;
using Domain.Types;
using Map.Grid;

/// <summary>
/// Computes movement reachability on a hex grid using breadth-first search (BFS).
///
/// Performs a uniform-cost flood fill from a starting coordinate, expanding
/// outward up to a maximum movement budget. Each step costs 1 move and only
/// walkable tiles may be entered.
///
/// Produces a mapping of reachable coordinates and their minimum movement cost,
/// primarily used for movement validation and range calculation.
/// </summary>
public sealed class Pathfinder : IPathfinder
{
    /// <summary>
    /// Computes all coordinates reachable from a starting position within a given
    /// movement budget using breadth-first search.
    ///
    /// Only tiles that exist in the map and are walkable may be entered.
    /// Returns a mapping of coordinate → minimum movement cost.
    /// </summary>
    public IReadOnlyDictionary<HexCoord, int> GetReachable(IReadOnlyMap map, HexCoord start, int maxMoves)
    {
        ValidateInputs(map, maxMoves);

        // Cannot move if the starting tile is invalid or blocked
        if (!CanStartOn(map, start))
            return new Dictionary<HexCoord, int>();

        return RunBreadthFirstSearch(map, start, maxMoves);
    }

    /// <summary>
    /// Determines whether a destination can be reached from a starting position
    /// within the specified movement budget.
    /// </summary>
    public bool IsMoveValid(IReadOnlyMap map, HexCoord start, HexCoord destination, int maxMoves)
    {
        ValidateInputs(map, maxMoves);

        // Special case: staying in place
        if (start.Equals(destination))
            return CanStartOn(map, start);

        var reachable = GetReachable(map, start, maxMoves);
        return reachable.TryGetValue(destination, out var cost) && cost <= maxMoves;
    }

    // Runs a BFS flood-fill from the start coordinate.
    // Tracks minimum movement cost to each reachable coordinate.
    private static IReadOnlyDictionary<HexCoord, int> RunBreadthFirstSearch(
        IReadOnlyMap map,
        HexCoord start,
        int maxMoves)
    {
        // Stores best known cost to each coordinate
        var costs = new Dictionary<HexCoord, int>();

        // BFS queue of coordinates to explore
        var queue = new Queue<HexCoord>();

        // Start position always costs 0
        costs[start] = 0;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            // Take next coordinate to expand
            var current = queue.Dequeue();
            var currentCost = costs[current];

            // Stop expanding when movement budget exhausted
            if (currentCost >= maxMoves)
                continue;

            // Get adjacent map-bounded coordinates
            foreach (var next in MapSearch.GetNeighbourCoords(map, current))
            {
                // Skip blocked or non-walkable tiles
                if (!CanEnter(map, next))
                    continue;

                var nextCost = currentCost + 1;

                // Ignore if exceeding movement budget
                if (nextCost > maxMoves)
                    continue;

                // In uniform-cost BFS, first visit is always the cheapest path
                if (costs.ContainsKey(next))
                    continue;

                // Record cost and schedule for further expansion
                costs[next] = nextCost;
                queue.Enqueue(next);
            }
        }

        return costs;
    }

    // Checks whether movement can begin from a coordinate.
    // Requires tile to exist and be walkable.
    private static bool CanStartOn(IReadOnlyMap map, HexCoord coord)
    {
        var tile = map.GetTile(coord);
        return tile != null && tile.IsWalkable;
    }

    // Checks whether movement may enter a coordinate.
    // Currently requires tile existence and walkability.
    private static bool CanEnter(IReadOnlyMap map, HexCoord coord)
    {
        var tile = map.GetTile(coord);
        return tile != null && tile.IsWalkable;
    }

    // Validates common input parameters.
    private static void ValidateInputs(IReadOnlyMap map, int maxMoves)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        if (maxMoves < 0)
            throw new ArgumentOutOfRangeException(nameof(maxMoves));
    }
}