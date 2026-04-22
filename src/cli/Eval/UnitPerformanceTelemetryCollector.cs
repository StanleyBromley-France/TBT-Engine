namespace Cli.Eval;

using Core.Domain.Types;
using Core.Engine.Telemetry;

internal sealed class UnitPerformanceTelemetryCollector : ICombatTelemetrySink
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
    }
}
