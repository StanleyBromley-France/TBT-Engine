namespace Core.Engine.Mutation;

using Core.Engine.Random;
using Core.Engine.Undo;
using Core.Game;

/// <summary>
/// Internal mutation-layer access contract that exposes controlled
/// access to mutable game state and core mutation services.
/// </summary>
/// <remarks>
/// <para>
/// Implemented by <see cref="GameMutationContext"/> and used exclusively
/// by mutators to retrieve the current <see cref="GameState"/> and
/// shared services such as <see cref="DeterministicRng"/>.
/// </para>
/// 
/// This interface prevents external systems from directly mutating
/// game state while allowing structured, centralized state changes
/// within the mutation layer.
/// </remarks>
internal interface IGameMutationAccess
{
    GameState GetState();
    UndoRecord GetUndo();
    DeterministicRng GetRngService();
}
