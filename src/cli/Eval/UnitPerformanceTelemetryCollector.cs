namespace Cli.Eval;

using Core.Domain.Types;
using Core.Engine.Telemetry;

internal sealed class UnitPerformanceTelemetryCollector : ICombatTelemetrySink
{
    private readonly Dictionary<int, UnitPerformanceTotals> _byUnitId = new();

    public void RecordDamage(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, int amount)
    {
        if (amount <= 0)
            return;

        GetOrCreate(sourceUnitId.Value).DamageDealt += amount;
        GetOrCreate(targetUnitId.Value).DamageTaken += amount;
    }

    public void RecordHealing(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, int amount)
    {
        if (amount <= 0)
            return;

        GetOrCreate(sourceUnitId.Value).HealingDone += amount;
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
    }
}
