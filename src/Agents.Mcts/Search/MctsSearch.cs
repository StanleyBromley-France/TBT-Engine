namespace Agents.Mcts.Search;

using Agents.Mcts.Config;
using Agents.Mcts.Simulation;
using Core.Engine.Actions.Choice;

public sealed class MctsSearch : IMctsSearch
{
    public ActionChoice FindBestAction(ISimulationFacade simulation, MctsSearchConfig config)
    {
        throw new NotImplementedException();
    }
}
