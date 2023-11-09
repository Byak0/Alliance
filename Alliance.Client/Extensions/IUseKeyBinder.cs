using Alliance.Client.Core.KeyBinder.Models;

namespace Alliance.Client.Extensions
{
    /// <summary>
    /// Implement this interface if you use key binder.
    /// </summary>
    public interface IUseKeyBinder
    {
        public BindedKeyCategory BindedKeys { get; }
    }
}
