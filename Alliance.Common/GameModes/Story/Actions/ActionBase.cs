using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Base class for Actions that can be performed during a scenario.
	/// Can be implemented in either Common, Client, or Server projects for specific behavior.
	/// Actions are created by the ActionFactory.
	/// </summary>
	[Serializable]
	public abstract class ActionBase
	{
		public virtual void Execute() { }

		public virtual void Register(GameEntity entity = null)
		{
			RegisterZones(entity);
		}

		protected void RegisterZones(GameEntity entity)
		{
			var properties = GetType().GetFields();

			foreach (var property in properties)
			{
				if (property.FieldType == typeof(SerializableZone))
				{
					var zone = property.GetValue(this) as SerializableZone;
					zone?.Register(entity);
				}
				else if (typeof(IEnumerable<SerializableZone>).IsAssignableFrom(property.FieldType))
				{
					var zones = property.GetValue(this) as IEnumerable<SerializableZone>;
					if (zones != null)
					{
						foreach (var zone in zones)
						{
							zone.Register(entity);
						}
					}
				}
			}
		}
	}
}