namespace Core.Engine.Victory;

using Core.Game.Match;
using Core.Game.Session;

public sealed class CompositeGameOverEvaluator : IGameOverEvaluator
{
    private readonly IReadOnlyList<IGameOverEvaluator> _evaluators;

    public CompositeGameOverEvaluator(IReadOnlyList<IGameOverEvaluator> evaluators)
    {
        _evaluators = evaluators ?? throw new ArgumentNullException(nameof(evaluators));

        if (_evaluators.Count == 0)
            throw new ArgumentException("At least one evaluator is required.", nameof(evaluators));
    }

    public GameOutcome Evaluate(GameSession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        foreach (var evaluator in _evaluators)
        {
            if (evaluator == null)
                continue;

            var result = evaluator.Evaluate(session);

            // First non-ongoing result wins
            if (result.Type != GameOutcomeType.Ongoing)
                return result;
        }

        return GameOutcome.Ongoing();
    }
}