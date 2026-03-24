namespace Cli.Eval.Results;

internal sealed record EvalBatchResult(IReadOnlyList<EvalScenarioResult> Scenarios);
