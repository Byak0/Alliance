using System;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.ExtendedXML.Models
{
	/// <summary>
	/// Extends the native Character XML with additional infos.
	/// </summary>
	public class ExtendedCharacter : MBObjectBase
	{
		private int _troopLeft = 1000;
		private int _troopLimit = 1000;
		private int _player_select_limit = 100;
		private bool _hard_limit = false;
		BasicCharacterObject _basicCharacterObject;

		public int TroopLeft
		{
			get
			{
				return _troopLeft;
			}
			set
			{
				_troopLeft = Math.Max(0, value);
			}
		}

		public int TroopLimit
		{
			get
			{
				return _troopLimit;
			}
			set
			{
				_troopLimit = value;
			}
		}

		public int PlayerSelectLimit
		{
			get
			{
				return _player_select_limit;
			}
			set
			{
				_player_select_limit = value;
			}
		}

		public bool HardLimit
		{
			get
			{
				return _hard_limit;
			}
			set
			{
				_hard_limit = value;
			}
		}

		public BasicCharacterObject BasicCharacterObject
		{
			get
			{
				return _basicCharacterObject;
			}
			private set
			{
				_basicCharacterObject = value;
			}
		}

		public ExtendedCharacter()
		{
		}

		public ExtendedCharacter(BasicCharacterObject basicCharacterObject)
		{
			_basicCharacterObject = basicCharacterObject;
		}

		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			XmlHelper.ReadInt(ref _troopLimit, node, "troop_limit");
			XmlHelper.ReadInt(ref _player_select_limit, node, "player_select_limit");
			_hard_limit = XmlHelper.ReadBool(node, "hard_limit");
			_basicCharacterObject = objectManager.ReadObjectReferenceFromXml<BasicCharacterObject>("id", node);
			TroopLeft = TroopLimit;
		}
	}
}
