using System;

namespace Alliance.Server.Core.Database.Models
{
	public partial class DiscordSpamMsg
	{
		public Guid TechId { get; set; }
		public string MsgId { get; set; }
		public string OwnerId { get; set; }
		public string OwnerName { get; set; }
		public DateTime? RecieveDate { get; set; }
		public string ChannelId { get; set; }
		public string SpamMsgDicoId { get; set; }
		public string ContentLink { get; set; }
		public DateTime? DeletedAt { get; set; }
		public string Content { get; set; }

		public virtual DiscordSpamContentMsgDico SpamMsgDico { get; set; }
	}
}
