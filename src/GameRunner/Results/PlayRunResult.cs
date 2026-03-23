namespace GameRunner.Results;

using Core.Game.Match;

public sealed record PlayRunResult(GameOutcome Outcome, int AppliedActionCount);
