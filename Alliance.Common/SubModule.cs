using Alliance.Common.GameModels;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common
{
    public class SubModule : MBSubModuleBase
    {
        public const string ModuleId = "Alliance.Common";

        protected override void OnSubModuleLoad()
        {
            Log("Alliance.Common initialized", LogLevel.Information);
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
        }

        // TODO => Find if GameModels can be moved to their own extension
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            gameStarter.AddModel(new ExtendedAgentStatCalculateModel());
            gameStarter.AddModel(new ExtendedAgentApplyDamageModel());
        }
    }
}