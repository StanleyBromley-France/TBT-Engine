namespace Core.Engine.Victory;

using Core.Game;

public sealed class LastTeamStandingEvaluator : IGameOverEvaluator
{
    public GameOutcome Evaluate(GameSession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        var state = session.State;

        var aliveTeams = state.UnitInstances.Values
            .Where(u => u.IsAlive)
            .Select(u => u.Team)
            .Distinct()
            .ToList();

        if (aliveTeams.Count == 0)
            return GameOutcome.Draw();

        if (aliveTeams.Count == 1)
            return GameOutcome.Victory(aliveTeams[0]);

        return GameOutcome.Ongoing();
    }
}