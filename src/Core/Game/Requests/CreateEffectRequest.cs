namespace Core.Game.Requests;

using Core.Domain.Types;

public sealed class CreateEffectRequest
{
    public EffectTemplateId TemplateId { get; }
    public UnitInstanceId SourceUnitId { get; }
    public UnitInstanceId TargetUnitId { get; }

    public CreateEffectRequest(
        EffectTemplateId templateId,
        UnitInstanceId sourceUnitId,
        UnitInstanceId targetUnitId)
    {
        TemplateId = templateId;
        SourceUnitId = sourceUnitId;
        TargetUnitId = targetUnitId;
    }
}
