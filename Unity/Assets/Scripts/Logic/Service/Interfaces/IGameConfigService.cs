using System.Collections.Generic;
using Lockstep.ECS;
using Lockstep.Math;
using Lockstep.GameCore;
using NetMsg.Common;

namespace Lockstep.Game
{
    public interface IGameConfigService : IService
    {
        EntityConfig GetEntityConfig(EntityType type, int id);
        AnimatorConfig GetAnimatorConfig(int id);
        SkillBoxConfig GetSkillConfig(int id);

        CollisionConfig CollisionConfig { get; }
        string RecorderFilePath { get; }
        string DumpStrPath { get; }
        Msg_G2C_GameStartInfo ClientModeInfo { get; }
    }
}