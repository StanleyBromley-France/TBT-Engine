
namespace Core.Domain.Effects.Components.Templates;

public interface IDamageComponent
{
    int DamageAmount { get; }
    DamageType DamageType { get; }
}
