namespace Agents.Mcts.Hashing;

using Core.Game.State.ReadOnly;

public interface IGameStateHasher
{
    GameStateKey Compute(IReadOnlyGameState state);
}
