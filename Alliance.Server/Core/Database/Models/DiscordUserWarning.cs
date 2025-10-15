using System;

namespace Alliance.Server.Core.Database.Models
{
	public partial class DiscordUserWarning
	{
		public int Id { get; set; }
		public string Username { get; set; }
		public DateTime CreatedAt { get; set; }
		public string WarningLevel { get; set; }
		public string Reason { get; set; }
		public string TargetDiscordId { get; set; }
		public string SenderDiscordId { get; set; }
	}
}
