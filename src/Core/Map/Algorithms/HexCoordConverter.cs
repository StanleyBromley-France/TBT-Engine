namespace Core.Map.Algorithms;

using Core.Domain.Types;
using Grid;

/// <summary>
/// Converts between axial (q, r) and offset (col, row) hex coordinates
/// /// </summary>
public static class HexCoordConverter
{
    /// <summary>
    /// Converts an axial coordinate to an offset (col, row) coordinate.
    /// </summary>
    public static (int col, int row) ToOffset(HexCoord axial)
    {
        int col = axial.Q;
        int row = axial.R + (axial.Q - (axial.Q & 1)) / 2;
        return (col, row);
    }

    /// <summary>
    /// Converts an offset (col, row) coordinate to an axial coordinate.
    /// </summary>
    public static HexCoord FromOffset(int col, int row)
    {
        int q = col;
        int r = row - (col - (col & 1)) / 2;
        return new HexCoord(q, r);
    }
}

