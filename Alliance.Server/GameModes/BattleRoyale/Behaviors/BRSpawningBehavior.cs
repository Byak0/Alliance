using Alliance.Client.Extensions.AdminMenu;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Server.GameModes.BattleRoyale.Behaviors
{
	/// <summary>
	/// Simple spawn behavior for the Battle Royale.    
	/// </summary>
	public class BRSpawningBehavior : SpawningBehaviorBase, ISpawnBehavior
	{
		public bool SpawnEnded;
		private float _lastSpawnCheck;
		private List<(EquipmentIndex, EquipmentElement)> _altEquipment;
		private static Random _random = new Random();
		private static List<float> _values = new List<float> { 0.5f, 1f, 1.5f, 2f, 2.5f };

		public BRSpawningBehavior()
		{
			// Alternate equipment for heroes
			EquipmentElement sword = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("mp_empire_gladius"), null, null, false);
			_altEquipment = new List<(EquipmentIndex, EquipmentElement)>
						{
							(EquipmentIndex.Weapon0, sword),
							(EquipmentIndex.Weapon1, EquipmentElement.Invalid),
							(EquipmentIndex.Weapon2, EquipmentElement.Invalid),
							(EquipmentIndex.Weapon3, EquipmentElement.Invalid)
						};
		}

		public override void RequestStartSpawnSession()
		{
			int spawnDuration = 20;

			string spawnBegins = $"Spawn begins. {spawnDuration}s remaining.";
			Log(spawnBegins, LogLevel.Debug);
			CommonAdminMsg.SendNotificationToAll(spawnBegins);

			base.RequestStartSpawnSession();

			Task.Run(() => StopSpawnAfterTimer(spawnDuration * 1000));
		}

		private async void StopSpawnAfterTimer(int waitTime)
		{
			await Task.Delay(waitTime);
			StopSpawn();
		}

		private void StopSpawn()
		{
			IsSpawningEnabled = false;


			string spawnOver = $"Spawn is over. Be the last alive to win.";
			Log(spawnOver, LogLevel.Debug);
			CommonAdminMsg.SendNotificationToAll(spawnOver);

			// Make everyone mortal once spawn is over
			foreach (Agent agent in Mission.Current?.AllAgents)
			{
				if (agent.CurrentMortalityState != MortalityState.Mortal)
				{
					agent.SetMortalityState(MortalityState.Mortal);
				}
			}
			Mission.AllowAiTicking = true;

			SpawnEnded = true;
		}

		public override void OnTick(float dt)
		{
			if (IsSpawningEnabled)
			{
				_lastSpawnCheck += dt;
				if (_lastSpawnCheck >= 1)
				{
					_lastSpawnCheck = 0;
					SpawnAgents();
				}
			}
		}

		public override void Initialize(SpawnComponent spawnComponent)
		{
			base.Initialize(spawnComponent);
		}

		protected override void SpawnAgents()
		{
			BasicCultureObject culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));

			// Spawn max 20 players at once
			int playersSpawn = 0;
			foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
			{
				MissionPeer missionPeer = peer.GetComponent<MissionPeer>();

				if (missionPeer == null || missionPeer.ControlledAgent != null)
				{
					continue;
				}
				if (peer.IsSynchronized && (missionPeer.Team == Mission.AttackerTeam || missionPeer.Team == Mission.DefenderTeam))
				{
					BasicCharacterObject basicCharacterObject;
					MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer;
					mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClasses(culture).First();
					MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(mPHeroClassForPeer.GetAllAvailablePerksForListIndex(0));

					bool useAltEquipement = true;
					basicCharacterObject = mPHeroClassForPeer.TroopCharacter;
					//if (PvCRoles.Instance.Commanders.Contains(peer.UserName) || PvCRoles.Instance.Officers.Contains(peer.UserName))
					//{
					//    mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClasses(culture).GetRandomElementInefficiently();
					//    basicCharacterObject = mPHeroClassForPeer.HeroCharacter;
					//    useAltEquipement = true;
					//}
					//else
					//{
					//    mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClasses(culture).GetRandomElementInefficiently();
					//    basicCharacterObject = mPHeroClassForPeer.TroopCharacter;
					//}
					SpawnHelper.SpawnPlayer(peer, onSpawnPerkHandler, basicCharacterObject, alternativeEquipment: useAltEquipement ? _altEquipment : null, customCulture: culture, mortalityState: MortalityState.Invulnerable);
					playersSpawn++;
				}
				if (playersSpawn >= 20) break;
			}

			// Spawn bots
			int nbBotsToSpawnAtt = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) + MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
			while (Mission.AttackerTeam.ActiveAgents.Count() < nbBotsToSpawnAtt)
			{
				BasicCharacterObject troopCharacter;
				//troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(culture).ToList().GetRandomElement().TroopCharacter;
				troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(culture).ToList().First().TroopCharacter;

				// Random difficulty between 0.5 and 2.5f
				float difficulty = _values[_random.Next(_values.Count)];
				SpawnHelper.SpawnBot(Mission.AttackerTeam, culture, troopCharacter, botDifficulty: difficulty, mortalityState: MortalityState.Invulnerable);
			}
		}

		protected override bool IsRoundInProgress()
		{
			return Mission.Current.CurrentState == Mission.State.Continuing;
		}

		public override bool AllowEarlyAgentVisualsDespawning(MissionPeer missionPeer)
		{
			return true;
		}

		public bool AllowExternalSpawn()
		{
			return IsRoundInProgress();
		}
	}
}
