using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Base class for conditions.
	/// Conditions are used in Scenarios or CS_ConditionalAction to determine if an action should be executed.
	/// </summary>
	public abstract class Condition
	{
		public enum TargetType
		{
			All,
			Bots,
			Players,
			Officers
		}

		public enum SideType
		{
			All = -1,
			Defender = BattleSideEnum.Defender,
			Attacker = BattleSideEnum.Attacker
		}

		public enum MoveOrderType
		{
			Charge,
			Move,
			Retreat,
			Stop,
			Advance,
			FallBack
		}

		public abstract bool Evaluate(ScenarioManager context);

		public virtual void Register(WeakGameEntity entity)
		{
			RegisterZones(entity);
		}

		/// <summary>
		/// Walks this condition's field graph and registers every embedded <see cref="Zone"/> with its host
		/// entity. Handles <c>ValueSource&lt;Zone&gt;</c> expression trees (zones nested inside functions,
		/// literal zone values, etc.).
		/// </summary>
		protected void RegisterZones(WeakGameEntity entity)
		{
			ZoneRegistrar.RegisterAll(this, entity);
		}

		public virtual void Unregister() { }
	}
}
