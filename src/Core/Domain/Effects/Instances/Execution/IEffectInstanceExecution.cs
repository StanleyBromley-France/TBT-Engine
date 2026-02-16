namespace Core.Domain.Effects.Instances.Execution;

using Core.Engine.Mutation;

public interface IEffectInstanceExecution
{
    void OnApply(GameMutationContext cxt);
    void OnTick(GameMutationContext cxt);
    void OnExpire(GameMutationContext cxt);
}
