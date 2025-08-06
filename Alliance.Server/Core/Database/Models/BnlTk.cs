using System;
using System.Collections.Generic;

namespace Alliance.Server.Core.Database.Models
{
    public partial class BnlTk
    {
        public int Id { get; set; }
        public int DamageTaken { get; set; }
        public string VictimName { get; set; }
        public string AttackerName { get; set; }
        public DateTime LstUpdTmstmp { get; set; }
        public int? BnlGameId { get; set; }

        public virtual BnlGame BnlGame { get; set; }
    }
}
