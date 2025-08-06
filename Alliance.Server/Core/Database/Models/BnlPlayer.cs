using System;
using System.Collections.Generic;

namespace Alliance.Server.Core.Database.Models
{
    public partial class BnlPlayer
    {
        public BnlPlayer()
        {
            BnlMsgs = new HashSet<BnlMsg>();
        }

        public DateTime LstUpdTmstmp { get; set; }
        public DateTime IsrtTmstmp { get; set; }
        public string Username { get; set; }
        public int Id { get; set; }
        public int? DiscordId { get; set; }

        public virtual DiscordUser Discord { get; set; }
        public virtual ICollection<BnlMsg> BnlMsgs { get; set; }
    }
}
