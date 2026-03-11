namespace Agents.Mcts.Simulation;

using Core.Engine;

public sealed class EngineSandboxFactory : ISandboxFactory
{
    public ISimulationFacade CreateFrom(EngineFacade engine)
    {
        if (engine is null)
            throw new ArgumentNullException(nameof(engine));

        var sandboxEngine = engine.CreateSandbox();
        return new SimulationFacade(sandboxEngine);
    }
}
