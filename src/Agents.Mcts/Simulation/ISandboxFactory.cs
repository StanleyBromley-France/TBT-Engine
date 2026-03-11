namespace Agents.Mcts.Simulation;

using Core.Engine;

public interface ISandboxFactory
{
    ISimulationFacade CreateFrom(EngineFacade engine);
}
