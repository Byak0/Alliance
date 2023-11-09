using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TaleWorlds.PlayerServices;

namespace Alliance.Common.Core.Security.Models
{
    [Serializable]
    public class Player
    {
        [XmlElement]
        public string Name { get; set; }
        [XmlElement]
        public string StringId
        {
            get
            {
                return _stringId;
            }
            set
            {
                _stringId = value;
                string[] parts = StringId.Split('.');
                byte providedType = Convert.ToByte(parts[0]);
                ulong part1 = Convert.ToUInt64(parts[1]);
                ulong part2 = Convert.ToUInt64(parts[2]);
                ulong part3 = Convert.ToUInt64(parts[3]);
                Id = new PlayerId(providedType, part1, part2, part3);
            }
        }

        private string _stringId;

        [XmlIgnore]
        public PlayerId Id;

        public Player(string name, string id)
        {
            StringId = id;
            Name = name;
        }

        public Player(string name, PlayerId id)
        {
            StringId = id.ToString();
            Name = name;
        }

        public Player() { }

        public override bool Equals(object obj)
        {
            return obj is Player player &&
                   Id == player.Id;
        }

        public override int GetHashCode()
        {
            return 2108858624 + EqualityComparer<PlayerId>.Default.GetHashCode(Id);
        }
    }
}
