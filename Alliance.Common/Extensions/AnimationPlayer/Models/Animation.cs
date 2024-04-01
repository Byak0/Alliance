using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AnimationPlayer.Models
{
    /// <summary>
    /// Contains necessary informations to store and configure an animation
    /// </summary>
    [Serializable]
    public class Animation
    {
        public int Index { get; set; }
        public string Name { get; set; }
        [XmlIgnore]
        public ActionIndexCache Action { get; set; }
        [XmlIgnore]
        public List<MBActionSet> ActionSets { get; set; }
        public float Speed { get; set; }
        public float MaxDuration { get; set; }

        public Animation()
        {
        }

        public Animation(int index, ActionIndexCache action, List<MBActionSet> actionSets, string name = "", float speed = 1f, float maxDuration = 0f)
        {
            Index = index;
            Action = action;
            ActionSets = actionSets;
            Name = name != "" ? name : Action.Name;
            Speed = speed;
            MaxDuration = maxDuration;
        }

        public Animation(int index, string name = "", float speed = 1f, float maxDuration = 0f)
        {
            Index = index;
            Action = AnimationSystem.Instance.IndexToActionDictionary[index];
            ActionSets = AnimationSystem.Instance.IndexToActionSetDictionary[index];
            Name = name != "" ? name : Action.Name;
            Speed = speed;
            MaxDuration = maxDuration != 0f ? maxDuration : AnimationSystem.Instance.IndexToDurationDictionary[index];
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            Action = AnimationSystem.Instance.IndexToActionDictionary[Index];
            ActionSets = AnimationSystem.Instance.IndexToActionSetDictionary[Index];
        }
    }
}
