using System;
using System.Collections.Generic;

namespace Alliance.Server.Core.Database.Models
{
	public partial class ZeventDonator
	{
		public ZeventDonator()
		{
			ZeventDonations = new HashSet<ZeventDonation>();
			ZeventRewards = new HashSet<ZeventReward>();
		}

		public int Id { get; set; }
		public DateTime InsertDate { get; set; }
		public DateTime LastUpdateDate { get; set; }
		public DateTime? DeletedAt { get; set; }
		public string Username { get; set; }

		public virtual ICollection<ZeventDonation> ZeventDonations { get; set; }
		public virtual ICollection<ZeventReward> ZeventRewards { get; set; }
	}
}
