namespace Core.Map.Grid;

/// <summary>
/// Axial hex coordinate (q, r) used to locate tiles on the hex grid.
/// </summary>
public readonly struct HexCoord : IEquatable<HexCoord>
{
    public int Q { get; }
    public int R { get; }
    public HexCoord(int q, int r)
    {
        Q = q;
        R = r;
    }

    public override string ToString() => $"({Q},{R})";

    /// <summary>
    /// Produces a hash code suitable for dictionary and set usage.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(Q, R);

    /// <summary>
    /// Checks value equality between two coordinates.
    /// </summary>
    public bool Equals(HexCoord other) => Q == other.Q && R == other.R;

    public override bool Equals(object? obj) => obj is HexCoord other && Equals(other);

    // Allows for comparing of two coordinates for value equality.
    public static bool operator ==(HexCoord left, HexCoord right) => left.Equals(right);

    // Allows for comparing of two coordinates for value inequality.
    public static bool operator !=(HexCoord left, HexCoord right) => !left.Equals(right);

    // Allows for Addition of one axial coordinate from another.
    public static HexCoord operator +(HexCoord a, HexCoord b) => new(a.Q + b.Q, a.R + b.R);

    // Allows for subtraction of one axial coordinate from another.
    public static HexCoord operator -(HexCoord a, HexCoord b) => new(a.Q - b.Q, a.R - b.R);
}

