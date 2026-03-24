namespace Cli.Eval.Observer;

using Core.Engine.Actions.Choice;
using Core.Game.Bootstrap.Contracts;
using GameRunner.Results;
using GameRunner.Runners.Observers;

internal sealed class ConsoleEvalRunObserver : IEvalRunObserver
{
    private readonly Dictionary<string, (int AttackerTeamId, int DefenderTeamId)> _scenarioTeams = new(StringComparer.Ordinal);

    public void RegisterScenario(string scenarioId, IGameStateSpec gameStateSpec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ArgumentNullException.ThrowIfNull(gameStateSpec);

        _scenarioTeams[scenarioId] = (gameStateSpec.AttackerTeamId.Value, gameStateSpec.DefenderTeamId.Value);
    }

    public void OnScenarioStarted(string scenarioId)
    {
        Console.WriteLine($"Starting scenario '{scenarioId}'...");
    }

    public void OnTurnStarted(string scenarioId, int attackerTurnsTaken, int teamToAct)
    {
        Console.WriteLine($"[{scenarioId}] Turn start: attackerTurnsTaken={attackerTurnsTaken}, teamToAct={ResolveTeamLabel(scenarioId, teamToAct)}");
    }

    public void OnActionChosen(string scenarioId, int actionIndex, ActionChoice action, TimeSpan selectionDuration)
    {
        Console.WriteLine(
            $"[{scenarioId}] Action {actionIndex}: {FormatAction(action)} selected in {selectionDuration.TotalMilliseconds:F0} ms");
    }

    public void OnScenarioCompleted(string scenarioId, EvalRunResult result, TimeSpan totalDuration)
    {
        var winner = result.WinningTeam.HasValue
            ? ResolveTeamLabel(scenarioId, result.WinningTeam.Value)
            : "Draw";
        Console.WriteLine(
            $"Finished scenario '{scenarioId}'. Outcome={result.OutcomeType}, Winner={winner}, Actions={result.AppliedActionCount}, Duration={totalDuration.TotalMilliseconds:F0} ms");
    }

    private static string FormatAction(ActionChoice action)
    {
        return action switch
        {
            MoveAction move => $"Move(unit={move.UnitId}, target={move.TargetHex})",
            UseAbilityAction useAbility => $"UseAbility(unit={useAbility.UnitId}, ability={useAbility.AbilityId}, target={useAbility.Target})",
            SkipActiveUnitAction skip => $"Skip(unit={skip.UnitId})",
            _ => $"{action.GetType().Name}(unit={action.UnitId})"
        };
    }

    private string ResolveTeamLabel(string scenarioId, int teamId)
    {
        if (_scenarioTeams.TryGetValue(scenarioId, out var teams))
        {
            if (teamId == teams.AttackerTeamId)
                return "Attacker";

            if (teamId == teams.DefenderTeamId)
                return "Defender";
        }

        return teamId.ToString();
    }
}
