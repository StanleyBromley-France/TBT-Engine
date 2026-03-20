namespace Core.Game.Factories.Units;

using Core.Domain.Types;

public sealed class UnitInstanceIdFactory : IUnitInstanceIdFactory
{
    private int _next = 1;

    public UnitInstanceId Create() => new UnitInstanceId(_next++);
}
