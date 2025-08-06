using System;
using System.Collections.Generic;

namespace Alliance.Server.Core.Database.Models
{
    public partial class BnlGame
    {
        public BnlGame()
        {
            BnlMsgs = new HashSet<BnlMsg>();
            BnlTks = new HashSet<BnlTk>();
        }

        public int Id { get; set; }
        public DateTime LstUpdTmstmp { get; set; }
        public string MapName { get; set; }
        public int? BnlScenarioId { get; set; }

        public virtual BnlScenario BnlScenario { get; set; }
        public virtual ICollection<BnlMsg> BnlMsgs { get; set; }
        public virtual ICollection<BnlTk> BnlTks { get; set; }
    }
}
