using System;
using System.Collections.Generic;

namespace Alliance.Server.Core.Database.Models
{
    public partial class BnlMsg
    {
        public int Id { get; set; }
        public int BnlGameId { get; set; }
        public DateTime LstUpdTmstmp { get; set; }
        public string PlayerName { get; set; }
        public string Msg { get; set; }
        public int BnlPlayerId { get; set; }

        public virtual BnlGame BnlGame { get; set; }
        public virtual BnlPlayer BnlPlayer { get; set; }
    }
}
