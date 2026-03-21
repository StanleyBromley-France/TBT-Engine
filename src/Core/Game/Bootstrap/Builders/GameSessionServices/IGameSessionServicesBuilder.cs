namespace Core.Game.Bootstrap.Builders.GameSessionServices;

using Core.Domain.Repositories;
using Core.Game.Session;

internal interface IGameSessionServicesBuilder
{
    GameSessionServices Build(TemplateRegistry templateRegistry);
}
