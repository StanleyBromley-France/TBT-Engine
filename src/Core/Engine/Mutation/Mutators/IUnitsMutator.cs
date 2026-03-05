namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;

/// <summary>
/// Mutation-layer API for modifying unit resource values>.
/// </summary>
/// <remarks>
/// Provides controlled mutation of unit resource fields such as HP, Mana,
/// and Action Points by updating entries in <see cref="Game.GameState.UnitInstances"/>.
/// </remarks>
public interface IUnitsMutator
{
    void ChangeHp(UnitInstanceId unitId, int delta);

    void ChangeMana(UnitInstanceId unitId, int delta);

    void ChangeActionPoints(UnitInstanceId unitId, int delta);

    void ChangeMovePoints(UnitInstanceId unitId, int delta);

    void ResetActionPoints(UnitInstanceId unitId);

    void ResetMovePoints(UnitInstanceId unitId);

    void SetDerivedStats(UnitInstanceId unitId, UnitDerivedStats newStats);
}