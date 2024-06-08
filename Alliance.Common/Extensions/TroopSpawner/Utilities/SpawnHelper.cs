using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.TroopSpawner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MPPerkObject;

namespace Alliance.Common.Extensions.TroopSpawner.Utilities
{
	public static class SpawnHelper
	{
		public enum Difficulty
		{
			PlayerChoice = -1,
			Easy = 0,
			Normal = 1,
			Hard = 2,
			VeryHard = 3,
			Bannerlord = 4
		}

		public static int TotalBots = 0;
		public const int MaxBotsPerSpawn = 200;

		static SpawnComponent SpawnComponent => Mission.Current.GetMissionBehavior<SpawnComponent>();
		static MissionLobbyComponent MissionLobbyComponent => Mission.Current.GetMissionBehavior<MissionLobbyComponent>();

		public static bool SpawnBot(Team team, BasicCultureObject culture, BasicCharacterObject character, MatrixFrame? position = null, MPOnSpawnPerkHandler onSpawnPerkHandler = null, int selectedFormation = -1, float botDifficulty = 1f, Agent.MortalityState mortalityState = Agent.MortalityState.Mortal)
		{
			try
			{
				// Define formation
				Formation form;
				if (selectedFormation > -1)
				{
					form = team.GetFormation((FormationClass)selectedFormation);
					form ??= new Formation(team, selectedFormation);
				}
				else
				{
					form = team.GetFormation(character.GetFormationClass());
					form ??= new Formation(team, character.DefaultFormationGroup);
				}

				// Define spawn position
				MatrixFrame spawnFrame = (MatrixFrame)(position != null ? position : SpawnComponent.GetSpawnFrame(team, character.HasMount(), false));
				AgentBuildData agentBuildData = new AgentBuildData(character).Team(team).InitialPosition(spawnFrame.origin);
				Vec2 initialDirection = spawnFrame.rotation.f.AsVec2;
				initialDirection = initialDirection.Normalized();

				// Define perks                
				Equipment equipment = Equipment.GetRandomEquipmentElements(character, randomEquipmentModifier: false, isCivilianEquipment: false, MBRandom.RandomInt());
				IEnumerable<(EquipmentIndex, EquipmentElement)> perkAlternativeEquipment = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: false);
				if (perkAlternativeEquipment != null)
				{
					foreach ((EquipmentIndex, EquipmentElement) item in perkAlternativeEquipment)
					{
						equipment[item.Item1] = item.Item2;
					}
				}

				int randomSeed = Config.Instance.RandomizeAppearance ? MBRandom.RandomInt() : 0;
				AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(initialDirection).TroopOrigin(new BasicBattleAgentOrigin(character))
					.VisualsIndex(randomSeed)
					.EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(character, agentBuildData.AgentVisualsIndex))
					.ClothingColor1(team == Mission.Current.AttackerTeam ? culture.Color : culture.ClothAlternativeColor)
					.ClothingColor2(team == Mission.Current.AttackerTeam ? culture.Color2 : culture.ClothAlternativeColor2)
					.Banner(team == Mission.Current.AttackerTeam ? Mission.Current.AttackerTeam.Banner : Mission.Current.DefenderTeam.Banner)
					.Formation(form)
					.IsFemale(character.IsFemale);
				agentBuildData2.Equipment(equipment);
				agentBuildData2.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData2.AgentRace, agentBuildData2.AgentIsFemale, character.GetBodyPropertiesMin(false), character.GetBodyPropertiesMax(), (int)agentBuildData2.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, character.HairTags, character.BeardTags, character.TattooTags));
				agentBuildData2.Age((int)agentBuildData2.AgentBodyProperties.Age);

				// Whether the agent has a mount or not
				ItemObject horseSlot = agentBuildData2.AgentData.AgentOverridenEquipment[EquipmentIndex.ArmorItemEndSlot].Item;
				bool hasMount = horseSlot != null && horseSlot.HasHorseComponent && horseSlot.HorseComponent.IsRideable;

				// Find free slots to spawn the bot
				List<int> forcedIndex = new List<int>();
				forcedIndex = AgentsInfoModel.Instance.GetAvailableSlotIndex(hasMount ? 2 : 1);
				if (forcedIndex.IsEmpty())
				{
					Log("Alliance : Cannot spawn - no slots available.", LogLevel.Error);
					return false;
				}

				string slotsUsed = forcedIndex.Count > 1 ? $"slots {forcedIndex[0]}-{forcedIndex[1]}" : $"slot {forcedIndex[0]}";
				string mountInfo = hasMount ? " (mounted)" : "";
				string originInfo = position?.origin.ToString() ?? "Unknown";
				Log($"Alliance : Trying to spawn bot n.{TotalBots} on {slotsUsed} \n {team.Side} | Char={character.Name}{mountInfo} | Origin={originInfo} | Formation={selectedFormation} | Culture={culture}", LogLevel.Debug);

				agentBuildData2.Index(forcedIndex[0]);
				if (hasMount) agentBuildData2.MountIndex(forcedIndex[1]);
				Agent agent = Mission.Current.SpawnAgent(agentBuildData2, false);
				agent.AddComponent(new MPPerksAgentComponent(agent));
				agent.MountAgent?.UpdateAgentProperties();
				float bonusHealth = onSpawnPerkHandler?.GetHitpoints(false) ?? 0f;
				agent.HealthLimit += bonusHealth;
				agent.Health = agent.HealthLimit;

				if (mortalityState != Agent.MortalityState.Mortal)
				{
					agent.SetMortalityState(mortalityState);
					agent.MountAgent?.SetMortalityState(mortalityState);
				}

				agent.AIStateFlags |= Agent.AIStateFlag.Alarmed;
				AgentsInfoModel.Instance.AddAgentInfo(agent, botDifficulty, synchronize: true);
				if (hasMount) AgentsInfoModel.Instance.AddAgentInfo(agent.MountAgent, botDifficulty, synchronize: true);
				agent.UpdateAgentProperties();
				agent.WieldInitialWeapons();

				Log("Alliance : Spawned bot n." + TotalBots++, LogLevel.Debug);
				return true;
			}
			catch (Exception ex)
			{
				Log("Alliance : ERROR spawning bot n." + TotalBots++, LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
				return false;
			}
		}

		public static void SpawnPlayer(NetworkCommunicator networkPeer, MPOnSpawnPerkHandler onSpawnPerkHandler, BasicCharacterObject character, MatrixFrame? origin = null, int selectedFormation = -1, IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipment = null, Agent.MortalityState mortalityState = Agent.MortalityState.Mortal, BasicCultureObject customCulture = null)
		{
			try
			{
				MissionPeer component = networkPeer.GetComponent<MissionPeer>();
				SpawnComponent spawnComponent = Mission.Current.GetMissionBehavior<SpawnComponent>();
				MissionLobbyComponent missionLobbyComponent = Mission.Current.GetMissionBehavior<MissionLobbyComponent>();
				MissionMultiplayerGameModeBase gameMode = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBase>();

				Formation form;
				if (selectedFormation > -1)
				{
					form = component.Team.GetFormation((FormationClass)selectedFormation);
					if (form == null) form = new Formation(component.Team, selectedFormation);
				}
				else
				{
					form = component.Team.GetFormation(character.GetFormationClass());
					if (form == null) form = new Formation(component.Team, character.DefaultFormationGroup);
				}

				BasicCultureObject culture = customCulture != null ? customCulture : component.Culture;
				uint color = component.Team == Mission.Current.AttackerTeam ? culture.Color : culture.ClothAlternativeColor;
				uint color2 = component.Team == Mission.Current.AttackerTeam ? culture.Color2 : culture.ClothAlternativeColor2;
				uint color3 = component.Team == Mission.Current.AttackerTeam ? culture.BackgroundColor1 : culture.BackgroundColor2;
				uint color4 = component.Team == Mission.Current.AttackerTeam ? culture.ForegroundColor1 : culture.ForegroundColor2;

				Banner banner = component.Team == Mission.Current.AttackerTeam ? Mission.Current.AttackerTeam.Banner : Mission.Current.DefenderTeam.Banner;
				int randomSeed = Config.Instance.RandomizeAppearance ? MBRandom.RandomInt() : 0;
				Log("Formation = " + form.FormationIndex.GetName(), LogLevel.Debug);
				AgentBuildData agentBuildData = new AgentBuildData(character)
					.VisualsIndex(randomSeed)
					.Team(component.Team)
					.TroopOrigin(new BasicBattleAgentOrigin(character))
					.Formation(form)
					.ClothingColor1(component.Team == Mission.Current.AttackerTeam ? culture.Color : culture.ClothAlternativeColor)
					.ClothingColor2(component.Team == Mission.Current.AttackerTeam ? culture.Color2 : culture.ClothAlternativeColor2)
					.Banner(component.Team == Mission.Current.AttackerTeam ? Mission.Current.AttackerTeam.Banner : Mission.Current.DefenderTeam.Banner);
				agentBuildData.MissionPeer(component);
				bool randomEquipement = true;
				Equipment equipment = randomEquipement ? Equipment.GetRandomEquipmentElements(character, randomEquipmentModifier: false, isCivilianEquipment: false, MBRandom.RandomInt()) : character.Equipment.Clone();
				IEnumerable<(EquipmentIndex, EquipmentElement)> perkAlternativeEquipment = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: true);
				if (perkAlternativeEquipment != null)
				{
					foreach ((EquipmentIndex, EquipmentElement) item in perkAlternativeEquipment)
					{
						equipment[item.Item1] = item.Item2;
					}
				}

				if (alternativeEquipment != null)
				{
					foreach ((EquipmentIndex, EquipmentElement) item in alternativeEquipment)
					{
						if (item.Item1 == EquipmentIndex.Horse &&
							!agentBuildData.AgentMonster.Flags.HasFlag(AgentFlag.CanRide)) continue;
						equipment[item.Item1] = item.Item2;
					}
				}
				agentBuildData.Equipment(equipment);

				// Use player custom bodyproperties only if allowed
				if (Config.Instance.AllowCustomBody)
				{
					gameMode.AddCosmeticItemsToEquipment(equipment, gameMode.GetUsedCosmeticsFromPeer(component, character));
					agentBuildData.BodyProperties(GetBodyProperties(component, component.Culture).ClampForMultiplayer());
					agentBuildData.Age((int)agentBuildData.AgentBodyProperties.Age);
					agentBuildData.IsFemale(component.Peer.IsFemale);
				}
				else
				{
					agentBuildData.EquipmentSeed(missionLobbyComponent.GetRandomFaceSeedForCharacter(character, agentBuildData.AgentVisualsIndex));
					agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, character.GetBodyProperties(agentBuildData.AgentOverridenSpawnEquipment), character.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, character.HairTags, character.BeardTags, character.TattooTags));
					agentBuildData.Age((int)agentBuildData.AgentBodyProperties.Age);
					agentBuildData.IsFemale(character.IsFemale);
				}

				if (component.ControlledFormation != null && component.ControlledFormation.Banner == null)
				{
					component.ControlledFormation.Banner = banner;
				}

				MatrixFrame spawnFrame = (MatrixFrame)(origin != null ? origin : spawnComponent.GetSpawnFrame(component.Team, character.HasMount(), component.SpawnCountThisRound == 0));

				agentBuildData.InitialPosition(spawnFrame.origin);
				Vec2 vec = spawnFrame.rotation.f.AsVec2;
				vec = vec.Normalized();
				agentBuildData.InitialDirection(vec);
				Agent agent = Mission.Current.SpawnAgent(agentBuildData, spawnFromAgentVisuals: true);
				agent.AddComponent(new MPPerksAgentComponent(agent));
				agent.MountAgent?.UpdateAgentProperties();
				float bonusHealth = onSpawnPerkHandler?.GetHitpoints(true) ?? 0f;
				// Additional health for officers
				if (networkPeer.IsOfficer())
				{
					agent.HealthLimit *= Config.Instance.OfficerHPMultip;
				}
				agent.HealthLimit += bonusHealth;
				agent.Health = agent.HealthLimit;

				agent.WieldInitialWeapons();

				if (mortalityState != Agent.MortalityState.Mortal)
				{
					agent.SetMortalityState(mortalityState);
					agent.MountAgent?.SetMortalityState(mortalityState);
				}


				if (agent.Formation != null)
				{
					MissionPeer temp = agent.MissionPeer;
					agent.MissionPeer = null;
					agent.Formation.OnUndetachableNonPlayerUnitAdded(agent);
					agent.MissionPeer = temp;
				}

				component.SpawnCountThisRound++;

				Log("Alliance : Spawned player " + networkPeer.UserName, LogLevel.Information);
			}
			catch (Exception ex)
			{
				Log("Alliance : ERROR spawning player " + networkPeer.UserName, LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}

		public static void SpawnPlayerPreview(NetworkCommunicator player, BasicCultureObject culture)
		{
			try
			{
				SpawnComponent spawnComponent = Mission.Current.GetMissionBehavior<SpawnComponent>();
				MissionMultiplayerGameModeBase gameMode = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBase>();
				MissionPeer peer = player.GetComponent<MissionPeer>();
				if (peer != null && peer.ControlledAgent == null && !peer.HasSpawnedAgentVisuals && peer.Team != null && peer.Team != Mission.Current.SpectatorTeam && peer.TeamInitialPerkInfoReady && peer.SpawnTimer.Check(Mission.Current.CurrentTime))
				{
					int num = peer.SelectedTroopIndex;
					IEnumerable<MultiplayerClassDivisions.MPHeroClass> mpheroClasses = MultiplayerClassDivisions.GetMPHeroClasses(culture);
					MultiplayerClassDivisions.MPHeroClass mpheroClass = num < 0 ? null : mpheroClasses.ElementAt(num);
					if (mpheroClass == null && num < 0)
					{
						mpheroClass = mpheroClasses.First();
						num = 0;
					}

					BasicCharacterObject character = mpheroClass.TroopCharacter;

					// If player is officer, assign a HeroCharacter instead
					if (player.IsOfficer())
					{
						character = mpheroClass.HeroCharacter;
						Log("Alliance - " + peer.DisplayedName + " is officer.", LogLevel.Debug);
					}

					Log($"Alliance - {peer.DisplayedName} will spawn as {character.StringId}", LogLevel.Debug);

					Equipment equipment = character.Equipment.Clone(false);
					MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(peer);
					IEnumerable<ValueTuple<EquipmentIndex, EquipmentElement>> enumerable = onSpawnPerkHandler?.GetAlternativeEquipments(true);
					if (enumerable != null)
					{
						foreach (ValueTuple<EquipmentIndex, EquipmentElement> valueTuple in enumerable)
						{
							equipment[valueTuple.Item1] = valueTuple.Item2;
						}
					}
					MatrixFrame matrixFrame;
					matrixFrame = spawnComponent.GetSpawnFrame(peer.Team, character.Equipment.Horse.Item != null, false);
					AgentBuildData agentBuildData = new AgentBuildData(character).MissionPeer(peer).Equipment(equipment).Team(peer.Team)
						.TroopOrigin(new BasicBattleAgentOrigin(character))
						.InitialPosition(matrixFrame.origin);
					Vec2 vec = matrixFrame.rotation.f.AsVec2;
					vec = vec.Normalized();
					AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec);

					// If custom body is allowed, retrieve player's gender and body properties
					if (Config.Instance.AllowCustomBody) agentBuildData2.IsFemale(peer.Peer.IsFemale).BodyProperties(GetBodyProperties(peer, culture));

					agentBuildData2.VisualsIndex(0)
						.ClothingColor1(peer.Team == Mission.Current.AttackerTeam ? culture.Color : culture.ClothAlternativeColor)
						.ClothingColor2(peer.Team == Mission.Current.AttackerTeam ? culture.Color2 : culture.ClothAlternativeColor2);
					gameMode.HandleAgentVisualSpawning(player, agentBuildData2, 0, Config.Instance.AllowCustomBody);

					Log("Alliance : Spawned visuals for player " + peer.Name, LogLevel.Information);
				}
			}
			catch (Exception ex)
			{
				Log("Alliance : ERROR spawning visuals for player " + player.UserName, LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}

		public static BodyProperties GetBodyProperties(MissionPeer missionPeer, BasicCultureObject cultureLimit)
		{
			NetworkCommunicator networkPeer = missionPeer.GetNetworkPeer();
			MissionLobbyComponent missionLobbyComponent = Mission.Current.GetMissionBehavior<MissionLobbyComponent>();

			if (networkPeer != null)
			{
				return networkPeer.PlayerConnectionInfo.GetParameter<PlayerData>("PlayerData").BodyProperties;
			}

			SpawnComponent spawnComponent = Mission.Current.GetMissionBehavior<SpawnComponent>();
			Debug.FailedAssert("networkCommunicator != null", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\SpawnBehaviors\\SpawningBehaviors\\SpawningBehaviorBase.cs", "GetBodyProperties", 366);
			Team team = missionPeer.Team;
			BasicCharacterObject troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(cultureLimit).ToList().GetRandomElement()
				.TroopCharacter;
			MatrixFrame spawnFrame = spawnComponent.GetSpawnFrame(team, troopCharacter.HasMount(), isInitialSpawn: true);
			AgentBuildData agentBuildData = new AgentBuildData(troopCharacter).Team(team).InitialPosition(in spawnFrame.origin);
			Vec2 direction = spawnFrame.rotation.f.AsVec2.Normalized();
			AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(in direction).TroopOrigin(new BasicBattleAgentOrigin(troopCharacter)).EquipmentSeed(missionLobbyComponent.GetRandomFaceSeedForCharacter(troopCharacter))
				.ClothingColor1(team.Side == BattleSideEnum.Attacker ? cultureLimit.Color : cultureLimit.ClothAlternativeColor)
				.ClothingColor2(team.Side == BattleSideEnum.Attacker ? cultureLimit.Color2 : cultureLimit.ClothAlternativeColor2)
				.IsFemale(troopCharacter.IsFemale);
			agentBuildData2.Equipment(Equipment.GetRandomEquipmentElements(troopCharacter, true, isCivilianEquipment: false, agentBuildData2.AgentEquipmentSeed));
			agentBuildData2.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData2.AgentRace, agentBuildData2.AgentIsFemale, troopCharacter.GetBodyPropertiesMin(), troopCharacter.GetBodyPropertiesMax(), (int)agentBuildData2.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData2.AgentEquipmentSeed, troopCharacter.HairTags, troopCharacter.BeardTags, troopCharacter.TattooTags));
			return agentBuildData2.AgentBodyProperties;
		}

		/// <summary>
		/// Spawn a random army for the specified team based on given gold and difficulty (max 1000 agents).
		/// </summary>
		/// <returns>Number of agents spawned.</returns>
		public static int SpawnArmy(int goldToUse, float difficulty, Team team, BasicCultureObject culture1)
		{
			List<BasicCharacterObject> agentsToSpawn = new List<BasicCharacterObject>();
			int nbAgents = 0;

			while (goldToUse > 0 && nbAgents < 1000)
			{
				BasicCharacterObject troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(culture1).ToList().GetRandomElement().TroopCharacter;
				goldToUse -= GetTroopCost(troopCharacter, difficulty);
				agentsToSpawn.Add(troopCharacter);
				nbAgents++;
			}

			foreach (BasicCharacterObject character in agentsToSpawn)
			{
				SpawnBot(team, culture1, character, botDifficulty: difficulty);
			}

			return nbAgents;
		}

		// Return a linear troop cost from minCost to MaxCost, depending on TroopMultiplier
		public static int GetTroopCost(BasicCharacterObject character, float difficultyMultiplier = 1f)
		{
			float multiplier = 0.75f + (difficultyMultiplier - 0.5f) * (1.5f - 0.75f) / (2.5f - 0.5f);

			if (MultiplayerClassDivisions.GetMPHeroClassForCharacter(character)?.TroopMultiplier is null)
			{
				Log("ERROR in GetTroopCost of " + character?.Name + " : Could not find associated TroopMultiplier.", LogLevel.Error);
				return 1;
			}

			return (int)(multiplier * (Config.Instance.MinTroopCost + (Config.Instance.MaxTroopCost - Config.Instance.MinTroopCost) * (1 - MultiplayerClassDivisions.GetMPHeroClassForCharacter(character).TroopMultiplier)));
		}

		/// <summary>
		/// Get total troop cost from a character, troop count and difficulty
		/// </summary>
		public static int GetTotalTroopCost(BasicCharacterObject troopToSpawn, int troopCount = 1, float difficultyMultiplier = 1f)
		{
			return GetTroopCost(troopToSpawn, difficultyMultiplier) * troopCount;
		}

		/// <summary>
		/// Get corresponding perks from a character and a list of perks indices.
		/// </summary>
		public static List<IReadOnlyPerkObject> GetPerks(BasicCharacterObject troop, List<int> indices)
		{
			MultiplayerClassDivisions.MPHeroClass heroClass = MultiplayerClassDivisions.GetMPHeroClassForCharacter(troop);
			List<List<IReadOnlyPerkObject>> allPerks = MultiplayerClassDivisions.GetAllPerksForHeroClass(heroClass);
			List<IReadOnlyPerkObject> selectedPerks = new List<IReadOnlyPerkObject>();
			int i = 0;
			foreach (List<IReadOnlyPerkObject> perkList in allPerks)
			{
				IReadOnlyPerkObject selectedPerk = perkList.ElementAtOrValue(indices.ElementAtOrValue(i, 0), null);
				if (selectedPerk != null)
				{
					selectedPerks.Add(selectedPerk);
				}
				i++;
			}
			return selectedPerks;
		}

		public static float DifficultyMultiplierFromLevel(int difficultyLevel)
		{
			switch (difficultyLevel)
			{
				case 0: return 0.5f;
				case 1: return 1f;
				case 2: return 1.5f;
				case 3: return 2f;
				case 4: return 2.5f;
				default: return 1f;
			}
		}

		public static float DifficultyMultiplierFromLevel(Difficulty difficultyLevel)
		{
			return DifficultyMultiplierFromLevel((int)difficultyLevel);
		}

		public static float DifficultyMultiplierFromLevel(string difficultyLevel)
		{
			if (Enum.TryParse(difficultyLevel, out Difficulty difficulty))
			{
				return DifficultyMultiplierFromLevel(difficulty);
			}
			return DifficultyMultiplierFromLevel(Difficulty.Normal);
		}

		public static int DifficultyLevelFromString(string difficultyString)
		{
			if (Enum.TryParse(difficultyString, out Difficulty difficulty))
			{
				return (int)difficulty;
			}
			return (int)Difficulty.Normal;
		}

		public static Difficulty DifficultyFromMultiplier(float multiplier)
		{
			switch (multiplier)
			{
				case 0.5f: return Difficulty.Easy;
				case 1f: return Difficulty.Normal;
				case 1.5f: return Difficulty.Hard;
				case 2f: return Difficulty.VeryHard;
				case 2.5f: return Difficulty.Bannerlord;
				default: return Difficulty.Normal;
			}
		}
	}
}
