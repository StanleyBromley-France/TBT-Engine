namespace GameRunner.Runners;

using Core.Domain.Types;
using Core.Engine;
using GameRunner.Controllers;
using GameRunner.Results;
using GameRunner.Runners.Observers;

public interface IEvalRunner
{
    ValueTask<EvalRunResult> RunAsync(
        string scenarioId,
        EngineFacade engine,
        IReadOnlyDictionary<TeamId, IPlayerController> controllers,
        IEvalRunObserver observer,
        IEvalRunTelemetryCollector? performanceCollector = null,
        CancellationToken cancellationToken = default);
}
