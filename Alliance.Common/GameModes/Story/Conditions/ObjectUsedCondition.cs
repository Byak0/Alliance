using Alliance.Common.Extensions.CustomScripts.Scripts;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Check if a CS_UsableObject has been used.
	/// </summary>
	public class ObjectUsedCondition : Condition
	{
		public string ObjectId;
		public bool AllowMultipleUses;

		private bool _used;

		public ObjectUsedCondition() { }

		public override void Register()
		{
			// Retrieve all usable objects with the correct ObjectId
			IEnumerable<CS_UsableObject> usableObjects = from amo in Mission.Current.MissionObjects
														 where amo is CS_UsableObject && (amo as CS_UsableObject).ObjectId == ObjectId
														 select (amo as CS_UsableObject);

			// For each matching usable object, listen to its OnUse event
			foreach (CS_UsableObject usableObject in usableObjects)
			{
				usableObject.OnUse += OnUse;
			}
		}

		public override void Unregister()
		{
			if (Mission.Current == null) return;

			// Retrieve all usable objects with the correct ObjectId
			IEnumerable<CS_UsableObject> usableObjects = from amo in Mission.Current.MissionObjects
														 where amo is CS_UsableObject && (amo as CS_UsableObject).ObjectId == ObjectId
														 select (amo as CS_UsableObject);

			// For each matching usable object, stop listening to its OnUse event
			foreach (CS_UsableObject usableObject in usableObjects)
			{
				usableObject.OnUse -= OnUse;
			}
		}

		private void OnUse()
		{
			_used = true;
			if (!AllowMultipleUses)
			{
				Unregister();
			}
		}

		public override bool Evaluate(ScenarioManager context)
		{
			// If the object has been used, return true and reset the used flag (unless AllowMultipleUses is false)
			if (_used)
			{
				_used = !AllowMultipleUses;
				return true;
			}

			return false;
		}
	}
}
