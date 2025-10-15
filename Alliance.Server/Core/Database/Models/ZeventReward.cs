using System;

namespace Alliance.Server.Core.Database.Models
{
	public partial class ZeventReward
	{
		public int Id { get; set; }
		public DateTime InsertDate { get; set; }
		public DateTime LastUpdateDate { get; set; }
		public DateTime? DeletedAt { get; set; }
		public int Tier { get; set; }
		public int Variant { get; set; }
		public int RewardTag { get; set; }
		public string Username { get; set; }

		public virtual ZeventDonator UsernameNavigation { get; set; }
	}
}
