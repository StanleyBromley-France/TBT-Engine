namespace Core.Map.Pathfinding;

using Domain.Types;
using Map.Grid;

public interface IPathfinder
{
    IReadOnlyDictionary<HexCoord, int> GetReachable(IReadOnlyMap map, HexCoord start, int maxMoves);
    bool IsMoveValid(IReadOnlyMap map, HexCoord start, HexCoord destination, int maxMoves);
}
