using System.Collections.Generic;
using Lockstep.Math;
using Lockstep.GameCore;

namespace Lockstep.Game
{
    public interface IGameStateService : IService
    {
        //changed in the game
        LFloat RemainTime { get; set; }
        LFloat DeltaTime { get; set; }
        int MaxEnemyCount { get; set; }
        int CurEnemyCount { get; set; }
        int CurEnemyId { get; set; }

        Enemy[] GetEnemies();
        Player[] GetPlayers();
        Spawner[] GetSpawners();

        Ball[] GetBalls();

        object GetEntity(int id);
        T CreateEntity<T>(EntityType type, int prefabId, LVector3 position)
            where T : BaseEntity, new();
        void DestroyEntity(BaseEntity entity);
    }
}
