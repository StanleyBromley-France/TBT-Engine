namespace Core.Engine.Telemetry;

using Core.Domain.Types;

public interface ICombatTelemetrySink
{
    void RecordDamage(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, int amount, bool wasFatal);

    void RecordHealing(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, int amount);

    void RecordEffectApplied(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, EffectTelemetryKind kind, int grantedTicks);
}
