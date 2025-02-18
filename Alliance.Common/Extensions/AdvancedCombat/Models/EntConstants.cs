using System.Collections.Generic;

namespace Alliance.Common.Extensions.AdvancedCombat.Models
{
	public static class EntConstants
	{
		public static readonly List<sbyte> StompCollisionBoneIds = new List<sbyte>
			{
				3, // Left foot
				7, // Right foot
			};

		public const int STOMP_RADIUS = 10; // Zone impacted by stomp
		public const int BASE_STOMP_DAMAGE = 50; // Base damage on stomp

		static EntConstants()
		{
		}
	}
}
