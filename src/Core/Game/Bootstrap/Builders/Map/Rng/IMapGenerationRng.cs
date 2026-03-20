namespace Core.Game.Bootstrap.Creation.Map.Rng;

public interface IMapGenerationRng
{
    (int Value, MapGenerationRngState NextState) Next(MapGenerationRngState state);
}
