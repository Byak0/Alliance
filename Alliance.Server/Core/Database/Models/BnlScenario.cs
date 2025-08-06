using System;
using System.Collections.Generic;

namespace Alliance.Server.Core.Database.Models
{
    public partial class BnlScenario
    {
        public BnlScenario()
        {
            BnlGames = new HashSet<BnlGame>();
        }

        public int Id { get; set; }
        public DateTime LstUpdTmstmp { get; set; }
        public string GameMode { get; set; }
        public string Name { get; set; }

        public virtual ICollection<BnlGame> BnlGames { get; set; }
    }
}
