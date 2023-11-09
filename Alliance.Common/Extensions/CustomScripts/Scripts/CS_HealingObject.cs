using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
    /// <summary>
    /// This script is attached to an object and allows interaction with it.
    /// On use, it will heal the user by the specified HealAmount until NumberOfUseMax is reached(-1 for no limit)
    /// Up to 3 actions/animations can be set up and chained before activating the heal.
    /// The script is built to handle most animations but the result is not guaranteed.
    /// Some animations may still cause issues. Use AnimationMaxDuration to prevent long or looping animations.
    /// </summary>
    public class CS_HealingObject : CS_UsableObject
    {
        public int HealAmount = 20;

        protected CS_HealingObject()
        {
        }

        protected override void AfterUse(Agent userAgent, bool actionCompleted = true)
        {
            base.AfterUse(userAgent);

            if (actionCompleted)
            {
                if (userAgent.HealthLimit - userAgent.Health > HealAmount)
                {
                    userAgent.Health += HealAmount;
                }
                else
                {
                    userAgent.Health = userAgent.HealthLimit;
                }
            }
            else
            {
                Log($"CS_HealingObject didn't heal {userAgent.MissionPeer?.Name} because he didn't complete the animation");
            }
        }

        static CS_HealingObject()
        {
        }
    }
}
