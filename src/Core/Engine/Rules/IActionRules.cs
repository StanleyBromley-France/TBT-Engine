namespace Core.Engine.Rules;

public interface IActionRules
{
    IActionValidator Validator { get; }
    IActionGenerator Generator { get; }
}