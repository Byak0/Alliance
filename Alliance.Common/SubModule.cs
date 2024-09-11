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
			Log("Alliance.Common initialized", LogLevel.Debug);
		}

		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
		}

		public override void OnGameInitializationFinished(Game game)
		{
		}
	}
}