using System;

namespace Alliance.Server.Core.Database.Models
{
	public partial class ZeventGoldPile
	{
		public int Id { get; set; }
		public DateTime InsertDate { get; set; }
		public DateTime LastUpdateDate { get; set; }
		public DateTime? DeletedAt { get; set; }
		public int GoldAmount { get; set; }
	}
}
