using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using TaleWorlds.Engine;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Base class for Actions that can be performed during a scenario.
	/// Can be implemented in either Common, Client, or Server projects for specific behavior.
	/// Target-specific overrides (Client_/Server_) are resolved at load time through the ActionOverrideRegistry.
	/// </summary>
	[Serializable]
	public abstract class ActionBase
	{
		public virtual ActionTask Execute() => ActionTask.CompletedTask;

		public virtual void Register(WeakGameEntity entity)
		{
			RegisterZones(entity);
		}

		/// <summary>
		/// Walks this action's field graph and registers every embedded <see cref="Zone"/> with its host entity. 
		/// Handles <c>ValueSource&lt;Zone&gt;</c> expression trees (zones nested inside functions, literal zone values, etc.).
		/// </summary>
		protected void RegisterZones(WeakGameEntity entity)
		{
			ZoneRegistrar.RegisterAll(this, entity);
		}
	}
}