namespace Core.Domain.Effects.Components.Templates;

public interface ICrittableComponentTemplate
{
    int CritChance { get; } // 0..100
    float CritMultiplier { get; } // e.g. 1.5f = 150%
}