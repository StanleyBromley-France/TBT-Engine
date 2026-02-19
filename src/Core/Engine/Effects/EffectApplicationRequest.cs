namespace Core.Engine.Effects;

using Core.Domain.Effects.Templates;
using Core.Domain.Types;

public sealed class EffectApplicationRequest
{
    public EffectTemplate Template { get; }
    public UnitInstanceId SourceUnitId { get; }
    public UnitInstanceId[] TargetUnitIds { get; }

    public EffectApplicationRequest(
        EffectTemplate template,
        UnitInstanceId sourceUnitId,
        UnitInstanceId[] targetUnitIds)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
        SourceUnitId = sourceUnitId;
        TargetUnitIds = targetUnitIds ?? throw new ArgumentNullException(nameof(targetUnitIds));
        if (TargetUnitIds.Length == 0)
            throw new ArgumentException("Must include at least one target.", nameof(targetUnitIds));
    }
}
