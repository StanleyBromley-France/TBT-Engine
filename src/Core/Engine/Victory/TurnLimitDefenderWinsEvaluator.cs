namespace Core.Engine.Victory;

using Core.Game;

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

        var state = session.State;

        if (state.Turn.AttackerTurnsTaken > MaxTurns)
        {
            var defendingTeam = session.Teams.GetOpposingTeam(state.Turn.TeamToAct);
            return GameOutcome.Victory(defendingTeam);
        }

        return GameOutcome.Ongoing();
    }
}