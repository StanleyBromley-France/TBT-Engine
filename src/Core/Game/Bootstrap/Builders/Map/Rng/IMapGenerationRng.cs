namespace Core.Game.Bootstrap.Builders.Map.Rng;

public interface IMapGenerationRng
{
    (int Value, MapGenerationRngState NextState) Next(MapGenerationRngState state);
}
