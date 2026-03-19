using Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders;

namespace Setup.Build.TemplateRegistry.Builders.EffectComponents;

public interface IEffectComponentBuilderResolver
{
    bool TryResolve(string? componentType, out IEffectComponentBuilder strategy);
}
