namespace Core.Engine.Telemetry;

using Core.Domain.Types;

public interface ICombatTelemetrySink
{
    void RecordDamage(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, int amount);

    void RecordHealing(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, int amount);
}
