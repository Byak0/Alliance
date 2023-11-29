using TaleWorlds.InputSystem;

namespace Alliance.Client.Core.KeyBinder.Models
{
    public class BindedKey
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public InputKey DefaultInputKey { get; set; }
        internal int KeyId { get; set; }
    }
}
