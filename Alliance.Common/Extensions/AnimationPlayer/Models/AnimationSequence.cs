using System;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.AnimationPlayer.Models
{
    /// <summary>
    /// Store a sequence of animations
    /// </summary>
    [Serializable]
    public class AnimationSequence
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public List<Animation> Animations { get; set; }

        public AnimationSequence()
        {
        }

        public AnimationSequence(int index, string name)
        {
            Index = index;
            Name = name;
            Animations = new List<Animation>();
        }

        /// <summary>
        /// Move specified animation one place up in the Animations list
        /// </summary>
        public void MoveAnimationUp(Animation animation)
        {
            int previousPlace = Animations.IndexOf(animation);
            if (previousPlace <= 0) return;
            Animations.RemoveAt(previousPlace);
            Animations.Insert(previousPlace - 1, animation);
        }

        /// <summary>
        /// Move specified animation one place down in the Animations list
        /// </summary>
        public void MoveAnimationDown(Animation animation)
        {
            int previousPlace = Animations.IndexOf(animation);
            if (previousPlace >= Animations.Count - 1) return;
            Animations.RemoveAt(previousPlace);
            Animations.Insert(previousPlace + 1, animation);
        }
    }
}
