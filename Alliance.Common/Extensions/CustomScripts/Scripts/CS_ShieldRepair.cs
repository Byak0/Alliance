using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
    /// <summary>
    /// This script is attached to an object and allows interaction with it.
    /// On use, it will repair the user's shield until NumberOfUseMax is reached (-1 for no limit)
    /// Up to 3 actions/animations can be set up and chained before activating the repair.
    /// The script is built to handle most animations but the result is not guaranteed.
    /// Some animations may still cause issues. Use AnimationMaxDuration to prevent long or looping animations.
    /// </summary>
    public class CS_ShieldRepair : CS_UsableObject
    {
        protected CS_ShieldRepair()
        {
        }

        protected override void AfterUse(Agent userAgent, bool actionCompleted = true)
        {
            base.AfterUse(userAgent);

            userAgent.RestoreShieldHitPoints();
        }

        static CS_ShieldRepair()
        {
        }
    }
}
