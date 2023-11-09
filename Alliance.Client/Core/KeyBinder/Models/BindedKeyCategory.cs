using System.Collections.Generic;

namespace Alliance.Client.Core.KeyBinder.Models
{
    public class BindedKeyCategory
    {
        public string CategoryId { get; set; }

        public string Category { get; set; }

        public ICollection<BindedKey> Keys { get; set; } = new List<BindedKey>();
    }

}
