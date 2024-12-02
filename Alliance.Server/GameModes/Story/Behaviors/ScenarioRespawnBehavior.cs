using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using NetworkMessages.FromServer;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static TaleWorlds.MountAndBlade.MPPerkObject;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Server.GameModes.Story.Behaviors
{
	/// <summary>
	/// MissionBehavior used to spawn/respawn players joining during a round.
	/// </summary>
	public class ScenarioRespawnBehavior : MissionNetwork, IMissionBehavior
	{
		//private Dictionary<MissionPeer, BasicCharacterObject> _playersPreviousCharacter;
		private ISpawnBehavior _spawnBehavior;
		private BasicCultureObject _cultureAttacker;
		private BasicCultureObject _cultureDefender;
		private MultiplayerClassDivisions.MPHeroClass _defaultMPHeroClassAttacker;
		private MultiplayerClassDivisions.MPHeroClass _defaultMPHeroClassDefender;
		private bool _allowRespawn;
		private float _lastSpawnCheck;
		private int _maxLivesAttacker;
		private int _maxLivesDefender;

		public ScenarioRespawnBehavior() : base()
		{
			//_playersPreviousCharacter = new Dictionary<MissionPeer, BasicCharacterObject>();
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();

			_spawnBehavior = (ISpawnBehavior)Mission.Current.GetMissionBehavior<SpawnComponent>().SpawningBehavior;

			if (ScenarioManagerServer.Instance.CurrentAct == null) return;

			string cultureTeam1 = ScenarioManagerServer.Instance.CurrentAct.ActSettings.TWOptions[OptionType.CultureTeam1].ToString();
			string cultureTeam2 = ScenarioManagerServer.Instance.CurrentAct.ActSettings.TWOptions[OptionType.CultureTeam2].ToString();
			_cultureAttacker = MBObjectManager.Instance.GetObject<BasicCultureObject>(cultureTeam1);
			_cultureDefender = MBObjectManager.Instance.GetObject<BasicCultureObject>(cultureTeam2);

			_defaultMPHeroClassAttacker = MultiplayerClassDivisions.GetMPHeroClasses(_cultureAttacker).FirstOrDefault(x => x.StringId == ScenarioManagerServer.Instance.CurrentAct.SpawnLogic.DefaultCharacters[(int)BattleSideEnum.Attacker]);
			if (_defaultMPHeroClassAttacker == null) _defaultMPHeroClassAttacker = MultiplayerClassDivisions.GetMPHeroClasses(_cultureAttacker).FirstOrDefault();
			_defaultMPHeroClassDefender = MultiplayerClassDivisions.GetMPHeroClasses(_cultureDefender).FirstOrDefault(x => x.StringId == ScenarioManagerServer.Instance.CurrentAct.SpawnLogic.DefaultCharacters[(int)BattleSideEnum.Defender]);
			if (_defaultMPHeroClassDefender == null) _defaultMPHeroClassDefender = MultiplayerClassDivisions.GetMPHeroClasses(_cultureDefender).FirstOrDefault();

			//_allowRespawn = ScenarioManagerServer.Instance.CurrentAct.SpawnLogic.AllowRespawn;
		}

		public override void OnRemoveBehavior()
		{
			base.OnRemoveBehavior();
		}

		public override void AfterStart()
		{
		}

		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);

			if (ScenarioManagerServer.Instance.CurrentAct != null && _spawnBehavior.AllowExternalSpawn() && _allowRespawn)
			{
				_lastSpawnCheck += dt;
				if (_lastSpawnCheck >= 10)
				{
					_lastSpawnCheck = 0;
					RespawnPlayersInRound();
				}
			}
		}

		/// <summary>
		/// Respawn players during a round, depending on the server configuration.
		/// AllowSpawnInRound : Allow players to join an ongoing round.
		/// FreeRespawnTimer : Grants free respawn for a limited time at the beginning of a round.
		/// </summary>
		private void RespawnPlayersInRound()
		{
			_maxLivesAttacker = ScenarioManagerServer.Instance.CurrentAct.SpawnLogic.MaxLives[0];
			_maxLivesDefender = ScenarioManagerServer.Instance.CurrentAct.SpawnLogic.MaxLives[1];
			for (int i = 0; i < GameNetwork.NetworkPeers.Count(); i++)
			{
				NetworkCommunicator peer = GameNetwork.NetworkPeers.ElementAt(i);
				MissionPeer missionPeer = peer.GetComponent<MissionPeer>();

				if (!CanPlayerBeRespawned(missionPeer))
				{
					continue;
				}
				if (peer.IsSynchronized && (missionPeer.Team == Mission.AttackerTeam || missionPeer.Team == Mission.DefenderTeam))
				{
					MPOnSpawnPerkHandler onSpawnPerkHandler = GetOnSpawnPerkHandler(missionPeer);
					BasicCharacterObject basicCharacterObject = GetCharacterForPeer(missionPeer.Team == Mission.AttackerTeam ? _defaultMPHeroClassAttacker : _defaultMPHeroClassDefender, peer);
					SpawnHelper.SpawnPlayer(peer, onSpawnPerkHandler, basicCharacterObject);
					int livesLeft = (missionPeer.Team == Mission.AttackerTeam ? _maxLivesAttacker : _maxLivesDefender) + 1 - missionPeer.SpawnCountThisRound;
					string report = "You have " + livesLeft + " reinforcements left.";
					GameNetwork.BeginModuleEventAsServer(peer);
					GameNetwork.WriteMessage(new ServerMessage(report, false));
					GameNetwork.EndModuleEventAsServer();
				}
			}
		}

		private BasicCharacterObject GetCharacterForPeer(MultiplayerClassDivisions.MPHeroClass mpHeroClass, NetworkCommunicator peer)
		{
			if (peer.IsOfficer() || peer.IsCommander())
			{
				return mpHeroClass.HeroCharacter;
			}
			else
			{
				return mpHeroClass.TroopCharacter;
			}
		}

		private bool CanPlayerBeRespawned(MissionPeer missionPeer)
		{
			if (missionPeer == null || missionPeer.ControlledAgent != null)
			{
				return false;
			}

			if (missionPeer.SpawnCountThisRound > (missionPeer.Team == Mission.AttackerTeam ? _maxLivesAttacker : _maxLivesDefender))
			{
				return false;
			}

			return true;
		}
	}
}