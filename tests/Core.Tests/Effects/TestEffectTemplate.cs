using Core.Effects.Templates;
using Core.Types;
namespace Core.Tests.Effects;

internal sealed class TestEffectTemplate : EffectTemplate
{
    public TestEffectTemplate(
        EffectTemplateId id,
        string name,
        bool isHarmful,
        int totalTicks,
        int maxStacks,
        IEnumerable<EffectComponentTemplate> components)
        : base(id, name, isHarmful, totalTicks, maxStacks, components)
    {
    }
}
