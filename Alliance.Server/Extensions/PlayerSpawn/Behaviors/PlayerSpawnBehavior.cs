using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.PlayerSpawn.Behaviors
{
	/// <summary>
	/// Server-side behavior to handle PlayerSpawnMenu initialization. Called by the different GameMode behaviors to get custom spawn behavior.
	/// </summary>
	public class PlayerSpawnBehavior : MissionNetwork
	{
		public bool Initialized { get; private set; } = false;
		public bool SpawnInProgress { get; private set; } = false;
		public float SpawnWaitTimeAfterSelection { get; private set; } = -1f;
		public float SpawnSessionLifeTime { get; private set; } = -1f;
		public float TimeSinceSpawnStart { get; private set; } = 0f;

		public float TimeLeftBeforeSpawnEnd => SpawnSessionLifeTime - TimeSinceSpawnStart;

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();

			MissionPeer.OnTeamChanged += OnPlayerChangeTeam;

			Initialized = true;
		}

		private void OnPlayerChangeTeam(NetworkCommunicator peer, Team previousTeam, Team newTeam)
		{
			if (PlayerSpawnMenu.Instance == null) return;

			// Clear previous player assignment if any
			PlayerAssignment previousAssignment = PlayerSpawnMenu.Instance.GetPlayerAssignment(peer);
			PlayerSpawnMenu.Instance.ClearCharacterSelection(peer);
		}

		protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
		{
			PlayerSpawnMenu.Instance.ClearDisconnectedPlayers();

			// When a new player connects, send the whole player spawn menu ("static" data)
			PlayerSpawnMenuMsg.SendPlayerSpawnMenuToPeer(networkPeer);
			PlayerSpawnMenuMsg.SendElectionStatusToPeer(PlayerSpawnMenu.Instance.ElectionInProgress, PlayerSpawnMenu.Instance.TimeBeforeOfficerElection, networkPeer);

			// Then send all the current character usages / officer candidates ("dynamic" data)
			foreach (PlayerTeam team in PlayerSpawnMenu.Instance.Teams)
			{
				foreach (PlayerFormation formation in team.Formations)
				{
					// Send the character usages
					foreach (NetworkCommunicator member in formation.Members)
					{
						PlayerAssignment assignment = PlayerSpawnMenu.Instance.GetPlayerAssignment(member);
						if (assignment.Character != null) PlayerSpawnMenuMsg.SendAddPlayerCharacterUsageToPeer(assignment.Player, team, formation, assignment.Character, networkPeer);
					}

					// Send the officer candidacies
					foreach (CandidateInfo candidateInfo in formation.Candidates)
					{
						PlayerAssignment assignment = PlayerSpawnMenu.Instance.GetPlayerAssignment(candidateInfo.Candidate);
						if (assignment.Character != null) PlayerSpawnMenuMsg.SendAddOfficerCandidacyToPeer(networkPeer, team, formation, assignment.Character, candidateInfo.Pitch, networkPeer);
					}

					// Send the formation officer if it exists
					if (formation.Officer != null)
					{
						PlayerSpawnMenuMsg.SendSetFormationOfficerToPeer(formation.Officer, team, formation, networkPeer);
					}
				}
			}

			// Send the spawn status
			PlayerSpawnMenuMsg.SendSpawnStatusToPeer(SpawnInProgress, SpawnWaitTimeAfterSelection, networkPeer);
		}

		public override void OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
		{
			PlayerSpawnMenu.Instance.ClearPlayer(networkPeer);
		}

		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);

			if (PlayerSpawnMenu.Instance == null) return;

			if (SpawnInProgress)
			{
				TimeSinceSpawnStart += dt;
				if (SpawnSessionLifeTime > 0f && TimeSinceSpawnStart >= SpawnSessionLifeTime)
				{
					StopSpawnSession();
				}
			}

			if (PlayerSpawnMenu.Instance.ElectionInProgress && PlayerSpawnMenu.Instance.TimeBeforeOfficerElection > 0f)
			{
				PlayerSpawnMenu.Instance.TimeBeforeOfficerElection -= dt;
				if (PlayerSpawnMenu.Instance.TimeBeforeOfficerElection <= 0f)
				{
					// Election timer expired, trigger the election end logic
					StopElection();
				}
			}

			foreach (PlayerAssignment assignment in PlayerSpawnMenu.Instance.PlayerAssignments.Values)
			{
				if (assignment.CanSpawn && assignment.TimeBeforeSpawn > 0f)
				{
					assignment.TimeBeforeSpawn -= dt;
				}
			}
		}

		public void StartSpawnSession(float spawnWaitTimeAfterSelection, float spawnSessionLifeTime = -1f)
		{
			if (PlayerSpawnMenu.Instance == null)
			{
				Log($"PlayerSpawnMenu is not initialized, can't start spawn session", LogLevel.Error);
				return;
			}
			SpawnInProgress = true;
			SpawnWaitTimeAfterSelection = spawnWaitTimeAfterSelection;
			SpawnSessionLifeTime = spawnSessionLifeTime;
			TimeSinceSpawnStart = 0f;
			foreach (PlayerAssignment assignment in PlayerSpawnMenu.Instance.PlayerAssignments.Values)
			{
				assignment.CanSpawn = true;
				assignment.TimeBeforeSpawn = spawnWaitTimeAfterSelection;
			}
			PlayerSpawnMenuMsg.SendSpawnStatusToAll(SpawnInProgress, SpawnWaitTimeAfterSelection);
			Log($"Alliance - PlayerSpawnMenu - Started spawn session, will last {spawnSessionLifeTime}s. Players will spawn {spawnWaitTimeAfterSelection}s after character choice.", LogLevel.Information);
		}

		public void StopSpawnSession()
		{
			SpawnInProgress = false;
			PlayerSpawnMenuMsg.SendSpawnStatusToAll(SpawnInProgress, -1f);
			Log($"Alliance - PlayerSpawnMenu - Stopped spawn session after {TimeSinceSpawnStart}s", LogLevel.Information);
		}

		public void StartElectionCountdown(float duration)
		{
			if (PlayerSpawnMenu.Instance == null)
			{
				Log($"PlayerSpawnMenu is not initialized, can't start election countdown", LogLevel.Error);
				return;
			}
			PlayerSpawnMenu.Instance.StartOfficerElection(duration);
			PlayerSpawnMenuMsg.SendElectionStatusToAll(true, duration);
			Log($"Alliance - PlayerSpawnMenu - Started election countdown of {duration}s. Officers will be elected afterward.", LogLevel.Information);
		}

		public void StopElection()
		{
			if (PlayerSpawnMenu.Instance == null)
			{
				Log($"PlayerSpawnMenu is not initialized, can't stop election", LogLevel.Error);
				return;
			}
			PlayerSpawnMenu.Instance.EndElection();
			PlayerSpawnMenuMsg.SendElectionStatusToAll(false);
			Log($"Alliance - PlayerSpawnMenu - Stopped election.", LogLevel.Information);
		}
	}
}
