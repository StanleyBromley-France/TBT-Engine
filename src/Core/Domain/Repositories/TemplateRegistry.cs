namespace Core.Domain.Repositories;

/// <summary>
/// Aggregates read-only repositories for all compiled game templates
/// required at runtime (units, abilities, effects, and effect components).
///
/// A <see cref="TemplateRegistry"/> represents static game content that is
/// shared for the lifetime of a match and must not be mutated at runtime.
/// It is constructed by the setup layer and owned by the game session.
/// </summary>
public sealed class TemplateRegistry
{
    public IUnitTemplateRepository Units { get; }
    public IAbilityRepository Abilities { get; }
    public IEffectTemplateRepository Effects { get; }
    public IEffectComponentTemplateRepository EffectComponents { get; }

    public TemplateRegistry(
        IUnitTemplateRepository units,
        IAbilityRepository abilities,
        IEffectTemplateRepository effects,
        IEffectComponentTemplateRepository effectComponents)
    {
        Units = units ?? throw new ArgumentNullException(nameof(units));
        Abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
        Effects = effects ?? throw new ArgumentNullException(nameof(effects));
        EffectComponents = effectComponents ?? throw new ArgumentNullException(nameof(effectComponents));
    }
}
