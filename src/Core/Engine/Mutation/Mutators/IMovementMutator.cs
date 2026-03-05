namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Types;

/// <summary>
/// Mutation-layer API for updating unit positional state.
/// </summary>
/// <remarks>
/// Responsible for relocating a unit to a new <see cref="HexCoord"/> by
/// mutating the corresponding entry in <see cref="Core.Game.GameState.UnitInstances"/>.
/// <para></para>
/// All movement operations must pass through this mutator to ensure
/// centralized state control and future undo support.
/// </remarks>
public interface IMovementMutator
{
    void MoveUnit(UnitInstanceId unitId, HexCoord newPos);
}