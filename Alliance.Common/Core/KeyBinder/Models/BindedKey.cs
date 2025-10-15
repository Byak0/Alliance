using TaleWorlds.InputSystem;

namespace Alliance.Common.Core.KeyBinder.Models
{
	public class BindedKey
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public InputKey DefaultInputKey { get; set; }
		public InputKey DefaultControllerKey { get; set; }
		internal int KeyId { get; set; }
	}
}
