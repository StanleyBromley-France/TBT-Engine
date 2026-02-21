using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Templates;
public sealed class InstantDamageComponentTemplate : EffectComponentTemplate, IDamageComponent, ICrittableComponentTemplate
{
    public int DamageAmount { get; }
    public DamageType DamageType { get; }

    public int CritChance { get; }
    public float CritMultiplier { get; }

    public InstantDamageComponentTemplate(
        EffectComponentTemplateId id,
        int damage,
        DamageType damageType,
        int critChance,
        float critMultiplier)
        : base(id)
    {
        DamageAmount = damage;
        DamageType = damageType;
        CritChance = critChance;
        CritMultiplier = critMultiplier;
    }
}
