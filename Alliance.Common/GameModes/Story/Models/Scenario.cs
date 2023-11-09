using System.Collections.Generic;

namespace Alliance.Common.GameModes.Story.Models
{
    public class Scenario
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Act> Acts;

        public Scenario(string id, string name, string desc)
        {
            Id = id;
            Name = name;
            Description = desc;
            Acts = new List<Act>();
        }
    }
}
