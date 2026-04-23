namespace Cli.Eval.Observer;

using Core.Engine.Actions.Choice;
using Core.Game.Bootstrap.Contracts;
using GameRunner.Results;
using GameRunner.Runners.Observers;
using System.Collections.Concurrent;

internal sealed class ConsoleEvalRunObserver : IEvalRunObserver
{
    private readonly ConcurrentDictionary<string, (int AttackerTeamId, int DefenderTeamId)> _scenarioTeams = new(StringComparer.Ordinal);
    private readonly object _consoleLock = new();

    public void RegisterScenario(string scenarioId, IGameStateSpec gameStateSpec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ArgumentNullException.ThrowIfNull(gameStateSpec);

        _scenarioTeams[scenarioId] = (gameStateSpec.AttackerTeamId.Value, gameStateSpec.DefenderTeamId.Value);
    }

    public void OnScenarioStarted(string scenarioId)
    {
        WriteLine($"Starting scenario '{scenarioId}'...");
    }

    public void OnTurnStarted(string scenarioId, int attackerTurnsTaken, int teamToAct)
    {
        WriteLine($"[{scenarioId}] Turn start: attackerTurnsTaken={attackerTurnsTaken}, teamToAct={ResolveTeamLabel(scenarioId, teamToAct)}");
    }

    public void OnActionChosen(string scenarioId, int actionIndex, ActionChoice action, TimeSpan selectionDuration)
    {
        WriteLine(
            $"[{scenarioId}] Action {actionIndex}: {FormatAction(action)} selected in {selectionDuration.TotalMilliseconds:F0} ms");
    }

    public void OnScenarioCompleted(string scenarioId, EvalRunResult result, TimeSpan totalDuration)
    {
        WriteLine(
            $"Finished scenario '{scenarioId}'. Outcome={result.Match.Outcome}, TerminationReason={result.Match.TerminationReason}, Actions={result.Match.ActionCount}, Duration={totalDuration.TotalMilliseconds:F0} ms");
    }

    private void WriteLine(string message)
    {
        lock (_consoleLock)
        {
            Console.WriteLine(message);
        }
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
