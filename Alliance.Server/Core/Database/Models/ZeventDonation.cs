using System;
using System.Collections.Generic;

namespace Alliance.Server.Core.Database.Models
{
    public partial class ZeventDonation
    {
        public int Id { get; set; }
        public DateTime InsertDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string Username { get; set; }
        public int DonationAmount { get; set; }
    }
}
