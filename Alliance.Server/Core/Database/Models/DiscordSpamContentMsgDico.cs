using System;
using System.Collections.Generic;

namespace Alliance.Server.Core.Database.Models
{
	public partial class DiscordSpamContentMsgDico
	{
		public DiscordSpamContentMsgDico()
		{
			DiscordSpamMsgs = new HashSet<DiscordSpamMsg>();
		}

		public string SpamMsgId { get; set; }
		public DateTime? DeletedAt { get; set; }
		public char? WasSafe { get; set; }

		public virtual ICollection<DiscordSpamMsg> DiscordSpamMsgs { get; set; }
	}
}
