using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Templates;
using Core.Domain.Types;

namespace Core.Tests.Domain.Effects;

internal sealed class TestEffectTemplate : Core.Domain.Effects.Templates.EffectTemplate
{
    public TestEffectTemplate(
        Core.Domain.Types.EffectTemplateId id,
        string name,
        bool isHarmful,
        int totalTicks,
        int maxStacks,
        IEnumerable<EffectComponentTemplate> components)
        : base(id, name, isHarmful, totalTicks, maxStacks, components)
    {
    }
}
