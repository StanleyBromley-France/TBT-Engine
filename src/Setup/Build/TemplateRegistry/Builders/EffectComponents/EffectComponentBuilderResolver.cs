using Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders;

namespace Setup.Build.TemplateRegistry.Builders.EffectComponents;

public sealed class EffectComponentBuilderResolver : IEffectComponentBuilderResolver
{
    private readonly Dictionary<string, IEffectComponentBuilder> _strategies;

    public EffectComponentBuilderResolver(Dictionary<string, IEffectComponentBuilder> strategies)
    {
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
    }

    public bool TryResolve(string? componentType, out IEffectComponentBuilder strategy)
    {
        if (!string.IsNullOrWhiteSpace(componentType) &&
            _strategies.TryGetValue(componentType.Trim(), out var resolved))
        {
            strategy = resolved;
            return true;
        }

        strategy = null!;
        return false;
    }
}
