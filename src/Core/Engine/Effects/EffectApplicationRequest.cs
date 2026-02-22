namespace Core.Engine.Effects;

using Core.Domain.Types;

public sealed class EffectApplicationRequest
{
    public EffectTemplateId TemplateId { get; }
    public UnitInstanceId SourceUnitId { get; }
    public UnitInstanceId[] TargetUnitIds { get; }

    public EffectApplicationRequest(
        EffectTemplateId templateId,
        UnitInstanceId sourceUnitId,
        UnitInstanceId[] targetUnitIds)
    {
        TemplateId = templateId;
        SourceUnitId = sourceUnitId;
        TargetUnitIds = targetUnitIds ?? throw new ArgumentNullException(nameof(targetUnitIds));
        if (TargetUnitIds.Length == 0)
            throw new ArgumentException("Must include at least one target.", nameof(targetUnitIds));
    }
}
