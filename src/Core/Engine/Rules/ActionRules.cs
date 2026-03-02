namespace Core.Engine.Rules;

public sealed class ActionRules : IActionRules
{
    public IActionValidator Validator { get; }
    public IActionGenerator Generator { get; }

    public ActionRules(IActionValidator validator, IActionGenerator generator)
    {
        Validator = validator ?? throw new ArgumentNullException(nameof(validator));
        Generator = generator ?? throw new ArgumentNullException(nameof(generator));
    }
}