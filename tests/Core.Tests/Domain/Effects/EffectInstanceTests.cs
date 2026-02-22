namespace Core.Tests.Domain.Effects;
using Core.Domain.Types;
public class EffectInstanceTests
{
    [Fact]
    public void CreateInstance_Initializes_From_Template()
    {
        var template = new TestEffectTemplate(
            id: new EffectTemplateId("test-effect"),
            name: "Test Effect",
            isHarmful: true,
            totalTicks: 3,
            maxStacks: 5,
            components: new[] { new NoOpEffectComponent(new EffectComponentTemplateId("comp-1")) });

        var instance = template.CreateInstance("source-1", "target-1");

        Assert.Equal(template, instance.Template);
        Assert.Equal("source-1", instance.SourceUnitId);
        Assert.Equal("target-1", instance.TargetUnitId);
        Assert.Equal(3, instance.RemainingTicks);
        Assert.Same(template.ComponentTemplateIds, instance.Components);
    }

    //[Fact]
    //public void Tick_Decrements_RemainingTicks_Until_Zero()
    //{
    //    var template = new TestEffectTemplate(
    //        id: "effect-2",
    //        name: "Tick Effect",
    //        isHarmful: true,
    //        totalTicks: 2,
    //        maxStacks: 1,
    //        components: new[] { new NoOpEffectComponent("comp-1") });

    //    var instance = template.CreateInstance("source", "target");
    //    var state = new GameState();

    //    instance.Tick(state);
    //    Assert.Equal(1, instance.RemainingTicks);

    //    instance.Tick(state);
    //    Assert.Equal(0, instance.RemainingTicks);

    //    instance.Tick(state);
    //    Assert.Equal(0, instance.RemainingTicks);
    //}

    [Fact]
    public void IncrementStack_Respects_MaxStacks()
    {
        var template = new TestEffectTemplate(
            id: new EffectTemplateId("effect-3"),
            name: "Stack Effect",
            isHarmful: false,
            totalTicks: 1,
            maxStacks: 2,
            components: new[] { new NoOpEffectComponent(new EffectComponentTemplateId("comp-1")) });

        var instance = template.CreateInstance("source", "target");

        Assert.Equal(1, instance.CurrentStacks);

        instance.IncrementStack();
        Assert.Equal(2, instance.CurrentStacks);

        instance.IncrementStack();
        Assert.Equal(2, instance.CurrentStacks);
    }
}
