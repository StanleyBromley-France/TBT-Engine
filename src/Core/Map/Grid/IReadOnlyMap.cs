using Core.Domain.Types;

namespace Core.Map.Grid;


public interface IReadOnlyMap
{
    int Width { get; }
    int Height { get; }

    IReadOnlyTile? GetTile(HexCoord coord);
    bool TryGetTile(HexCoord coord, out IReadOnlyTile tile);
}