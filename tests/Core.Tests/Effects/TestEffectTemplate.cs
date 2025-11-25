using Core.Effects.Templates;

namespace Core.Tests.Effects;

internal sealed class TestEffectTemplate : EffectTemplate
{
    public TestEffectTemplate(
        string id,
        string name,
        bool isHarmful,
        int totalTicks,
        int maxStacks,
        IEnumerable<EffectComponentTemplate> components)
        : base(id, name, isHarmful, totalTicks, maxStacks, components)
    {
    }
}
