namespace Agents.Mcts.Evaluation;

using Core.Domain.Types;
using Core.Game.State.ReadOnly;

public sealed class MaterialStateEvaluator : IStateEvaluator
{
    private const double WinScore = 1_000_000d;

    public double Evaluate(IReadOnlyGameState state, TeamId perspective)
    {
        if (state is null)
            throw new ArgumentNullException(nameof(state));

        var allyAliveCount = 0;
        var enemyAliveCount = 0;
        var allyHp = 0d;
        var enemyHp = 0d;

        foreach (var unit in state.UnitInstances.Values)
        {
            if (!unit.IsAlive)
                continue;

            if (unit.Team == perspective)
            {
                allyAliveCount++;
                allyHp += unit.Resources.HP;
            }
            else
            {
                enemyAliveCount++;
                enemyHp += unit.Resources.HP;
            }
        }

        if (allyAliveCount == 0 && enemyAliveCount == 0)
            return 0d;

        if (enemyAliveCount == 0)
            return WinScore + allyHp;

        if (allyAliveCount == 0)
            return -WinScore - enemyHp;

        return (allyHp - enemyHp) + (allyAliveCount - enemyAliveCount) * 100d;
    }
}
