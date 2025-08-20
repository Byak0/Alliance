using System;

namespace Alliance.Server.Core.Database.Models
{
	public partial class DiscordEvent
	{
		public int Id { get; set; }
		public DateTime IsrtTmstmp { get; set; }
		public string Status { get; set; }
		public string Name { get; set; }
		public string Desc { get; set; }
		public DateTime? EventDate { get; set; }
		public string RequesterDiscId { get; set; }
		public string EventChannelId { get; set; }
		public string BannerUrl { get; set; }
		public DateTime LstUpdTmstmp { get; set; }
		public string ValidatorDiscId { get; set; }
		public DateTime? ValidationTmstmp { get; set; }
		public string CreatorDiscId { get; set; }
		public DateTime? CreationTmstmp { get; set; }
		public string GameType { get; set; }
		public string RequestMsgId { get; set; }
		public string DiscordEventId { get; set; }
		public bool? IsHabitue { get; set; }
	}
}
