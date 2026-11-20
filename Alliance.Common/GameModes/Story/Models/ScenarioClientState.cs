using System;

namespace Alliance.Common.GameModes.Story.Models
{
	public class ScenarioClientState
	{
		private RespawnStrategy _respawnStrategy;
		private int _teamRemainingLives;
		private int _playerRemainingLives;

		public RespawnStrategy RespawnStrategy => _respawnStrategy;
		public int TeamRemainingLives => _teamRemainingLives;
		public int PlayerRemainingLives => _playerRemainingLives;

		public event Action OnLivesChanged;

		public void UpdateLives(RespawnStrategy respawnStrategy, int teamRemainingLives, int playerRemainingLives)
		{
			if (_respawnStrategy == respawnStrategy
				&& _teamRemainingLives == teamRemainingLives
				&& _playerRemainingLives == playerRemainingLives)
			{
				return;
			}

			_respawnStrategy = respawnStrategy;
			_teamRemainingLives = teamRemainingLives;
			_playerRemainingLives = playerRemainingLives;
			OnLivesChanged?.Invoke();
		}
	}
}
