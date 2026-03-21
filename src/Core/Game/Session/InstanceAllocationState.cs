namespace Core.Game.Session;

public sealed class InstanceAllocationState
{
    private int _nextUnitInstanceIdSeed = 1;
    private int _nextEffectInstanceIdSeed = 1;
    private int _nextEffectComponentInstanceIdSeed = 1;

    public int GetNextUnitInstanceIdSeed()
        => _nextUnitInstanceIdSeed++;

    public int GetNextEffectInstanceIdSeed()
        => _nextEffectInstanceIdSeed++;

    public int GetNextEffectComponentInstanceIdSeed()
        => _nextEffectComponentInstanceIdSeed++;

    public InstanceAllocationState DeepCloneForSimulation()
    {
        return new InstanceAllocationState
        {
            _nextUnitInstanceIdSeed = _nextUnitInstanceIdSeed,
            _nextEffectInstanceIdSeed = _nextEffectInstanceIdSeed,
            _nextEffectComponentInstanceIdSeed = _nextEffectComponentInstanceIdSeed,
        };
    }
}
