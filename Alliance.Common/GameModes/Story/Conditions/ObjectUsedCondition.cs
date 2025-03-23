using Alliance.Common.Extensions.CustomScripts.Scripts;
using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Check if a CS_UsableObject has been used.
	/// </summary>
	public class ObjectUsedCondition : Condition
	{
		[ScenarioEditor(label: "Object ID", tooltip: "ID of the script to listen to.")]
		public string ObjectId;
		[ScenarioEditor(label: "Allow multiple use", tooltip: "If enabled, condition can be triggered every time the object is used.")]
		public bool AllowMultipleUses;
		[ScenarioEditor(label: "Restrict to parent entity", tooltip: "If enabled, condition will only check its parent entity and children (entity MUST have any MissionObject script, can be AL_TriggerAction).")]
		public bool ParentEntityOnly;

		private bool _used;
		private GameEntity _gameEntity;

		public ObjectUsedCondition() { }

		public override void Register(GameEntity gameEntity = null)
		{
			if (Mission.Current == null) return;

			IEnumerable<CS_UsableObject> usableObjects;

			if (ParentEntityOnly && gameEntity != null)
			{
				_gameEntity = gameEntity;
				// Retrieve all usable objects with the correct ObjectId among entity children
				usableObjects = from GameEntity entity in gameEntity.GetChildren()
								where entity.GetFirstScriptOfType<CS_UsableObject>()?.ObjectId == ObjectId
								select entity.GetFirstScriptOfType<CS_UsableObject>();
			}
			else
			{
				// Retrieve all usable objects with the correct ObjectId
				usableObjects = from amo in Mission.Current.MissionObjects
								where amo is CS_UsableObject && (amo as CS_UsableObject).ObjectId == ObjectId
								select (amo as CS_UsableObject);
			}

			// For each matching usable object, listen to its OnUse event
			foreach (CS_UsableObject usableObject in usableObjects)
			{
				Log($"ObjectUsedCondition registering OnUse of entity {usableObject.GameEntity.Name} {usableObject.Id}", LogLevel.Debug);
				usableObject.OnUse += OnUse;
			}
		}

		public override void Unregister()
		{
			if (Mission.Current == null) return;

			IEnumerable<CS_UsableObject> usableObjects;

			if (_gameEntity != null)
			{
				// Retrieve all usable objects with the correct ObjectId among entity children
				usableObjects = from GameEntity entity in _gameEntity.GetChildren()
								where entity.GetFirstScriptOfType<CS_UsableObject>()?.ObjectId == ObjectId
								select entity.GetFirstScriptOfType<CS_UsableObject>();
			}
			else
			{
				// Retrieve all usable objects with the correct ObjectId
				usableObjects = from amo in Mission.Current.MissionObjects
								where amo is CS_UsableObject && (amo as CS_UsableObject).ObjectId == ObjectId
								select (amo as CS_UsableObject);
			}

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
