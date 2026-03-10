namespace Agents.Mcts.Search;

using Agents.Mcts.Config;
using Agents.Mcts.Simulation;
using Core.Engine.Actions.Choice;

public interface IMctsSearch
{
    ActionChoice FindBestAction(ISimulationFacade simulation, MctsSearchConfig config);
}
