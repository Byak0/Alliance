using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TaleWorlds.PlayerServices;

namespace Alliance.Common.Core.Security.Models
{
	[Serializable]
	public class PlayerDataFile
	{
		[XmlArray] public List<AL_PlayerData> Players { get; set; } = new();
	}

	[Serializable]
	public class AL_PlayerData
	{
		[XmlElement] public string Name { get; set; }

		[XmlElement]
		public string StringId
		{
			get => _stringId;
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

		private string _stringId = "0.0.0.0";

		[XmlIgnore] public PlayerId Id { get; set; }

		// Permissions & moderation
		[XmlElement] public bool Sudo { get; set; } = false;
		[XmlElement] public bool Admin { get; set; } = false;
		[XmlElement] public int WarningCount { get; set; } = 0;
		[XmlElement] public string LastWarning { get; set; } = "";
		[XmlElement] public int KickCount { get; set; } = 0;
		[XmlElement] public int BanCount { get; set; } = 0;
		[XmlElement] public bool IsMuted { get; set; } = false;
		[XmlElement] public bool IsBanned { get; set; } = false;
		[XmlElement] public string LastBanReason { get; set; } = "";
		[XmlElement] public DateTime SanctionEnd { get; set; } = DateTime.MinValue;

		// Other data
		[XmlElement] public bool VIP { get; set; } = false;
		//...

		[XmlIgnore] public bool IsSudo => Sudo;
		[XmlIgnore] public bool IsAdmin => Admin || IsSudo;

		public AL_PlayerData() { }
		public AL_PlayerData(string name, PlayerId id)
		{
			Name = name;
			Id = id;
			_stringId = id.ToString();
		}

		public override bool Equals(object obj) => obj is AL_PlayerData player && Id == player.Id;

		public override int GetHashCode() => Id.GetHashCode();
	}
}
