using System;
using System.Collections.Generic;
using System.Reflection;
using Lockstep.Game;
using Lockstep.GameCore;
using UnityEngine;

namespace Lockstep.Game
{
    public class GameResourceService : BaseGameService, IGameResourceService
    {
        public string pathPrefix = "Prefabs/";

        private Dictionary<int, GameObject> _id2Prefab = new Dictionary<int, GameObject>();

        public object LoadPrefab(int id, uint type)
        {


            var finalId = id + (int)type * 100000;

            return _LoadPrefab(finalId, id, (EntityType)type);
        }

        GameObject _LoadPrefab(int finalId, int id, EntityType type)
        {
            if (_id2Prefab.TryGetValue(finalId, out var val))
            {
                return val;
            }

            var config = _gameConfigService.GetEntityConfig(type, id);
            if (string.IsNullOrEmpty(config.prefabPath)) return null;
            var prefab = (GameObject)Resources.Load(pathPrefix + config.prefabPath);
            _id2Prefab[finalId] = prefab;
            switch (type)
            {
                case EntityType.Player:
                    PhysicSystem.Instance.RigisterPrefab(id, (int)EColliderLayer.Hero);
                    break;
                case EntityType.Enemy:
                    PhysicSystem.Instance.RigisterPrefab(id, (int)EColliderLayer.Enemy);
                    break;
            }
            return prefab;
        }
    }
}