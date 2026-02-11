namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Types;
using Core.Engine.Mutation;

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
public sealed class MovementMutator
{
    private readonly IGameMutationAccess _ctx;

    public MovementMutator(GameMutationContext ctx)
        => _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));

    public void MoveUnit(UnitInstanceId unitId, HexCoord newPos)
    {
        var state = _ctx.GetState();
        var unit = state.UnitInstances[unitId];

        var before = unit.Position;
        unit.Position = newPos;

        // TODO: Record undo step in UndoRecord
    }
}
