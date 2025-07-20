using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.PlayerSpawn.Models
{
	/// <summary>
	/// Represents an officer candidate in the player spawn menu.
	/// </summary>
	public class CandidateInfo
	{
		public NetworkCommunicator Candidate { get; }
		public string Pitch { get; set; }
		public int Votes { get; set; }

		public CandidateInfo(NetworkCommunicator candidate, string pitch)
		{
			Candidate = candidate;
			Pitch = pitch;
			Votes = 0;
		}
	}
}
