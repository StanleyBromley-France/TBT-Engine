namespace Agents.Mcts.Policies;

using Core.Engine.Actions.Choice;
using Core.Engine.Turn;
using Core.Game.State.ReadOnly;
using Agents.Mcts.Search;

public sealed class MctsTurnPolicy : ITurnPolicy
{
    private readonly IMctsSearch _search;

    public MctsTurnPolicy(IMctsSearch search)
    {
        _search = search ?? throw new ArgumentNullException(nameof(search));
    }

    public ActionChoice ChooseAction(IReadOnlyGameState state, IReadOnlyList<ActionChoice> legalActions)
    {
        // TODO: Wire simulation source / sandbox creation path so search can evaluate rollouts.
        throw new NotImplementedException();
    }
}
