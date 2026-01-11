using Core.Abilities;
using Core.Abilities.Targeting;
using Core.Effects.Templates;
using Core.Tests.Effects;
using Core.Types;

namespace Core.Tests.Abilities;

public class AbilityTests
{
    [Fact]
    public void Effects_Are_Defensively_Copied_From_Source_Collection()
    {
        var cost = new AbilityCost(mana: 1);

        var targeting = new TargetingRules(
            range: 1,
            requiresLineOfSight: false,
            allowedTargets: new[] { TargetType.Enemy },
            areaPattern: null,
            includeSelf: false);

        var effectTemplate = new TestEffectTemplate(
            id: new EffectTemplateId("e1"),
            name: "E1",
            isHarmful: true,
            totalTicks: 1,
            maxStacks: 1,
            components: new[] { new NoOpEffectComponent(new EffectComponentTemplateId("c1")) });

        var sourceList = new List<EffectTemplate> { effectTemplate };

        var ability = new Ability(
            id: new AbilityId("a1"),
            name: "Ability 1",
            category: AbilityCategory.MeleeAttack,
            cost: cost,
            targeting: targeting,
            effects: sourceList);

        Assert.Single(ability.Effects);
        Assert.Same(effectTemplate, ability.Effects[0]);
    }
}
