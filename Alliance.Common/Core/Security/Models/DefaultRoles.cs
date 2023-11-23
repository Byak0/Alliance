using System;
using System.Collections.Generic;

namespace Alliance.Common.Core.Security.Models
{
    /// <summary>
    /// WIP - Roles are mostly placeholders for now.
    /// Serializable to allow direct update from the XML file on the server side.
    /// </summary>
    [Serializable]
    public class DefaultRoles
    {
        public List<Player> Commanders;

        public List<Player> Officers;

        public List<Player> Admins;

        public List<Player> Devs;

        public List<Player> Moderators;

        public List<Player> Banned;

        public DefaultRoles()
        {
            Commanders = new List<Player>();
            Officers = new List<Player>();
            Admins = new List<Player>();
            Devs = new List<Player>();
            Moderators = new List<Player>();
            Banned = new List<Player>();
        }

        public void Init()
        {
            Commanders = new List<Player>
            {
                new Player("[COMB] Byako", "2.0.0.76561198029123129"),
                new Player("Jeandelaville", "2.0.0.76561197971698668")
            };
            Officers = new List<Player>
            {
                new Player("[COMB] Byako", "2.0.0.76561198029123129"),
                new Player("Jeandelaville", "2.0.0.76561197971698668")
            };
            Admins = new List<Player>()
            {
                new Player("Captain-Fracas", "2.0.0.76561198060703792"),
                new Player("Coolstan", "2.0.0.76561198057978000"),
                new Player("[COMB] Byako", "2.0.0.76561198029123129"),
                new Player("Daeimara", "2.0.0.76561198106249258"),
                new Player("Smooth", "2.0.0.76561198026238164"),
                new Player("Voda", "2.0.0.76561198026723199"),
                new Player("Redfield", "2.0.0.76561198004394006"),
                new Player("Thasom", "2.0.0.76561198239584448"),
                new Player("Jeandelaville", "2.0.0.76561197971698668")
            };
            Devs = new List<Player>()
            {
                new Player("[COMB] Byako", "2.0.0.76561198029123129"),
                new Player("Redfield", "2.0.0.76561198004394006"),
                new Player("Jeandelaville", "2.0.0.76561197971698668")
            };
        }
    }
}