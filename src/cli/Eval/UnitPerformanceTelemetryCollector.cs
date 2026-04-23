namespace Cli.Eval;

using Core.Domain.Types;
using Core.Engine.Telemetry;
using Core.Game.State.ReadOnly;
using Core.Map.Search;
using GameRunner.Runners;

internal sealed class UnitPerformanceTelemetryCollector : ICombatTelemetrySink, IEvalRunTelemetryCollector
{
    private readonly Dictionary<int, UnitPerformanceTotals> _byUnitId = new();

    public void RecordDamage(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, int amount, bool wasFatal)
    {
        if (amount <= 0)
            return;

        GetOrCreate(sourceUnitId.Value).DamageDealt += amount;
        GetOrCreate(targetUnitId.Value).DamageTaken += amount;

        if (wasFatal)
        {
            GetOrCreate(sourceUnitId.Value).Kills += 1;
            GetOrCreate(targetUnitId.Value).Deaths += 1;
        }
    }

    public void RecordHealing(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, int amount)
    {
        if (amount <= 0)
            return;

        GetOrCreate(sourceUnitId.Value).HealingDone += amount;
    }

    public void RecordEffectApplied(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, EffectTelemetryKind kind, int grantedTicks)
    {
        var totals = GetOrCreate(sourceUnitId.Value);

        if (kind == EffectTelemetryKind.Debuff)
        {
            totals.DebuffEffectsApplied += 1;
            totals.DebuffUptimeTicksGranted += grantedTicks;
            return;
        }

        if (kind == EffectTelemetryKind.Buff)
        {
            totals.BuffEffectsApplied += 1;
            totals.BuffUptimeTicksGranted += grantedTicks;
        }
    }

    public void OnTeamTurnStarted(IReadOnlyGameState state)
    {
        foreach (var unit in state.UnitInstances.Values)
        {
            if (!unit.IsAlive || unit.Team != state.Turn.TeamToAct)
                continue;

            GetOrCreate(unit.Id.Value).TurnsSurvived += 1;
        }
    }

    public void OnActionChosen(IReadOnlyGameState state, Core.Engine.Actions.Choice.ActionChoice action)
    {
        var totals = GetOrCreate(action.UnitId.Value);
        totals.ActionsTaken += 1;

        switch (action)
        {
            case Core.Engine.Actions.Choice.UseAbilityAction:
                totals.AbilityCasts += 1;
                break;
            case Core.Engine.Actions.Choice.MoveAction move:
                totals.MoveActions += 1;
                var start = state.UnitInstances[action.UnitId].Position;
                totals.TilesMovedTotal += MapSearch.GetDistance(start, move.TargetHex);
                break;
        }
    }

    public UnitPerformanceTotals GetTotals(int unitInstanceId)
    {
        return _byUnitId.TryGetValue(unitInstanceId, out var totals)
            ? totals
            : new UnitPerformanceTotals();
    }

    private UnitPerformanceTotals GetOrCreate(int unitInstanceId)
    {
        if (_byUnitId.TryGetValue(unitInstanceId, out var totals))
            return totals;

        totals = new UnitPerformanceTotals();
        _byUnitId[unitInstanceId] = totals;
        return totals;
    }

    internal sealed class UnitPerformanceTotals
    {
        public int DamageDealt { get; set; }

        public int DamageTaken { get; set; }

        public int HealingDone { get; set; }

        public int Kills { get; set; }

        public int Deaths { get; set; }

        public int BuffEffectsApplied { get; set; }

        public int DebuffEffectsApplied { get; set; }

        public int BuffUptimeTicksGranted { get; set; }

        public int DebuffUptimeTicksGranted { get; set; }

        public int ActionsTaken { get; set; }

        public int AbilityCasts { get; set; }

        public int MoveActions { get; set; }

        public int TilesMovedTotal { get; set; }

        public int TurnsSurvived { get; set; }
    }
}
