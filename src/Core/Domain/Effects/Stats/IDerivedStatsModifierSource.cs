namespace Core.Domain.Effects.Stats;

public interface IDerivedStatsModifierSource
{
    int GetGoodFlatOrZero(StatType stat);
    int GetBadFlatOrZero(StatType stat);

    float GetGoodPercentOrZero(StatType stat);
    float GetBadPercentOrZero(StatType stat);
}
