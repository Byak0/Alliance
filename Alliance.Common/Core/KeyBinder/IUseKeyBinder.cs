using Alliance.Common.Core.KeyBinder.Models;

namespace Alliance.Common.Core.KeyBinder
{
	/// <summary>
	/// Implement this interface if you use key binder.
	/// </summary>
	public interface IUseKeyBinder
	{
		public BindedKeyCategory BindedKeys { get; }
	}
}
