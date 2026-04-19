using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using Alliance.Common.GameModes.Lobby.Behaviors;
using Alliance.Server.Extensions.PlayerSpawn.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Lobby.Behaviors
{
	public class LobbyBehavior : MissionMultiplayerGameModeBase
	{
		private PlayerSpawnBehavior _playerSpawnBehavior;

		public override bool IsGameModeHidingAllAgentVisuals
		{
			get
			{
				return true;
			}
		}

		public override bool IsGameModeUsingOpposingTeams
		{
			get
			{
				return false;
			}
		}

		public override MultiplayerGameType GetMissionType()
		{
			return MultiplayerGameType.Skirmish;
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();

			BasicCultureObject cultureDef = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
			Banner bannerDef = new Banner(cultureDef.Banner, cultureDef.BackgroundColor1, cultureDef.ForegroundColor1);
			Team teamDef = Mission.Teams.Add(BattleSideEnum.Defender, cultureDef.BackgroundColor1, cultureDef.ForegroundColor1, bannerDef, isPlayerGeneral: false, isPlayerSergeant: true, true);
			teamDef.SetIsEnemyOf(teamDef, true);

			BasicCultureObject cultureAttack = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
			Banner bannerAttack = new Banner(cultureAttack.Banner, cultureAttack.BackgroundColor1, cultureAttack.ForegroundColor1);
			Team teamAttack = Mission.Teams.Add(BattleSideEnum.Attacker, cultureAttack.BackgroundColor1, cultureAttack.ForegroundColor1, bannerAttack, isPlayerGeneral: false, isPlayerSergeant: true, true);
			teamAttack.SetIsEnemyOf(teamAttack, false);

			// Generate default spawn menu if none exists
			if (PlayerSpawnMenu.Instance.Teams.IsEmpty())
			{
				if (PlayerSpawnMenu.TryLoadFromFile(SubModule.PlayerSpawnMenuFilePath, out PlayerSpawnMenu newMenu))
				{
					PlayerSpawnMenu.Instance = newMenu;
					Log($"Alliance - Loaded PlayerSpawnMenu succesfully with {PlayerSpawnMenu.Instance.Teams.Count} teams.", LogLevel.Information);
				}
				else
				{
					PlayerSpawnMenu.Instance = new PlayerSpawnMenu();
					BasicCultureObject culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
					PlayerSpawnMenu.Instance.GenerateDefaultMenu(new List<KeyValuePair<BattleSideEnum, BasicCultureObject>>
					{
						new(BattleSideEnum.Defender, culture)
					});
					Log($"Alliance - Failed to load PlayerSpawnMenu from {SubModule.PlayerSpawnMenuFilePath}. Using default menu with culture {culture}.", LogLevel.Warning);
				}

				// Broadcast the updated player spawn menu to all players
				PlayerSpawnMenuMsg.SendPlayerSpawnMenuToAll();
			}

			_playerSpawnBehavior = Mission.Current.GetMissionBehavior<PlayerSpawnBehavior>();
			_playerSpawnBehavior.StartSpawnSession(MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue());
		}

		protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
		{
			networkPeer.AddComponent<LobbyRepresentative>();
		}

		protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			component.Team = Mission.DefenderTeam;
		}

		public LobbyBehavior()
		{
		}
	}
}
