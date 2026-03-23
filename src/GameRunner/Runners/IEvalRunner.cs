namespace GameRunner.Runners;

using Core.Domain.Types;
using Core.Engine;
using GameRunner.Controllers;
using GameRunner.Results;

public interface IEvalRunner
{
    ValueTask<EvalRunResult> RunAsync(
        EngineFacade engine,
        IReadOnlyDictionary<TeamId, IPlayerController> controllers,
        CancellationToken cancellationToken = default);
}
