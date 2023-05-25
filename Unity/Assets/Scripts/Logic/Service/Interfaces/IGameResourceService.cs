using Lockstep.Game;
using Lockstep.GameCore;
namespace Lockstep.Game
{
    public interface IGameResourceService : IService
    {
        object LoadPrefab(int id, uint type);
    }
}