using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.ExtendedXML.Models
{
	/// <summary>
	/// Extends the native Character XML with additional infos. Unused for now.
	/// </summary>
	public class ExtendedCharacter : MBObjectBase
	{
		BasicCharacterObject _basicCharacterObject;

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
			_basicCharacterObject = objectManager.ReadObjectReferenceFromXml<BasicCharacterObject>("id", node);
		}
	}
}
