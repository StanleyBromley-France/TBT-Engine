namespace Core.Engine.Turn;

using Core.Engine;

public sealed class TurnPolicyContext
{
    public TurnPolicyContext(EngineFacade engine)
    {
        Engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public EngineFacade Engine { get; }
}
