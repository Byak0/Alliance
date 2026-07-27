using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Base class for Actions that can be performed during a scenario.
	/// Can be implemented in either Common, Client, or Server projects for specific behavior.
	/// <para>Actions are always executed on the server first, where the context is resolved.
	/// A server action can then send a message ExecuteActionOnClient(this, context) to the client to execute a corresponding client action, passing along any relevant context data.</para>
	/// Target-specific overrides (Client_/Server_) are resolved at load time through the ActionOverrideRegistry.
	/// </summary>
	[Serializable]
	public abstract class ActionBase
	{
		public virtual ActionTask Execute(VariableStore context) => ActionTask.CompletedTask;

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

		protected void ExecuteOnClient(VariableStore context)
		{
			if (GameNetwork.IsServer)
			{
				// TODO 
				//SendExecuteActionOnClient(this, context);
			}
		}
	}
}