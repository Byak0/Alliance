using System;

namespace Alliance.Server.Core.Database.Models
{
	public partial class ZeventDonation
	{
		public int Id { get; set; }
		public DateTime InsertDate { get; set; }
		public DateTime LastUpdateDate { get; set; }
		public DateTime? DeletedAt { get; set; }
		public string Username { get; set; }
		public string DonationComment { get; set; }
		public decimal DonationAmount { get; set; }

		public virtual ZeventDonator UsernameNavigation { get; set; }
	}
}
