using System.Collections.Generic;
using Lockstep.Game;
using Lockstep.GameCore;
using Lockstep.Math;
using NetMsg.Common;
using UnityEngine;
using Debug = Lockstep.Logging.Debug;

namespace Lockstep.Game
{
    public class GameConfigService : BaseGameService, IGameConfigService
    {
        private GameConfig _config;
        public string configPath = "GameConfig";

        public override void DoAwake(IServiceContainer container)
        {
            _config = Resources.Load<GameConfig>(configPath);
            _config.DoAwake();
        }

        //TODO: 需要改成EntityType的方式
        public EntityConfig GetEntityConfig(EntityType type, int id)
        {
            //var id = 0;
            switch (type)
            {
                case EntityType.Player:
                    return _config.GetPlayerConfig(id);
                case EntityType.Enemy:
                    id = id - 10;
                    return _config.GetEnemyConfig(id);
                case EntityType.Spawner:
                    id = id - 100;
                    return _config.GetSpawnerConfig(id);
                case EntityType.Ball:
                    LogMaster.L("Load ball Config " + id);
                    var cfg = _config.GetBallConfig(id);
                    return cfg;
                default:
                    LogMaster.E($"undefine type   {type} ");
                    return null;
            }
        }

        public AnimatorConfig GetAnimatorConfig(int id)
        {
            return _config.GetAnimatorConfig(id - 1);
        }

        public SkillBoxConfig GetSkillConfig(int id)
        {
            return _config.GetSkillConfig(id - 1);
        }

        public CollisionConfig CollisionConfig => _config.CollisionConfig;
        public string RecorderFilePath => _config.RecorderFilePath;
        public string DumpStrPath => _config.DumpStrPath;
        public Msg_G2C_GameStartInfo ClientModeInfo => _config.ClientModeInfo;
    }
}