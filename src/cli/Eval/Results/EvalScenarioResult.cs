namespace Cli.Eval.Results;

using GameRunner.Results;

internal sealed record EvalScenarioResult(string GameStateId, EvalRunResult Result);
