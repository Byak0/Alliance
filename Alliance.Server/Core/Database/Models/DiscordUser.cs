using System;

namespace Alliance.Server.Core.Database.Models
{
	public partial class DiscordUser
	{
		public string DiscordId { get; set; }
		public DateTime? LstUpdTmstmp { get; set; }
		public string DiscordTagName { get; set; }
		public long? LastEventScoreReceived { get; set; }
		public int? ParticipationPoints { get; set; }
		public DateTime CreatedAt { get; set; }
		public string DisplayedName { get; set; }
		public int Id { get; set; }
	}
}
