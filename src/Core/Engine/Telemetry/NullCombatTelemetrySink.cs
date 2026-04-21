namespace Core.Engine.Telemetry;

using Core.Domain.Types;

public sealed class NullCombatTelemetrySink : ICombatTelemetrySink
{
    public static NullCombatTelemetrySink Instance { get; } = new();

    private NullCombatTelemetrySink()
    {
    }

    public void RecordDamage(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, int amount)
    {
    }

    public void RecordHealing(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, int amount)
    {
    }
}
