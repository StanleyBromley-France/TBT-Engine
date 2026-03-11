namespace Core.Engine.Victory;

using Core.Game.Match;
using Core.Game.Session;

public sealed class TurnLimitDefenderWinsEvaluator : IGameOverEvaluator
{
    public int MaxTurns { get; }

    public TurnLimitDefenderWinsEvaluator(int maxTurns)
    {
        if (maxTurns <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTurns));

        MaxTurns = maxTurns;
    }

    public GameOutcome Evaluate(GameSession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        var state = session.Runtime.State;

        if (state.Turn.AttackerTurnsTaken > MaxTurns)
        {
            return GameOutcome.Victory(session.Context.Teams.Defender);
        }

        return GameOutcome.Ongoing();
    }
}
