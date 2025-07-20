using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.CustomScripts.Scripts;
using System.Collections.Generic;
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
		[ConfigProperty(label: "Object ID", tooltip: "ID of the script to listen to.")]
		public string ObjectId;
		[ConfigProperty(label: "Allow multiple use", tooltip: "If enabled, condition can be triggered every time the object is used.")]
		public bool AllowMultipleUses;
		[ConfigProperty(label: "Restrict to parent entity", tooltip: "If enabled, condition will only check its parent entity and children (entity MUST have any MissionObject script, can be AL_TriggerAction).")]
		public bool ParentEntityOnly;

		private bool _used;
		private GameEntity _gameEntity;

		public ObjectUsedCondition() { }

		public override void Register(GameEntity gameEntity = null)
		{
			if (Mission.Current == null) return;

			_gameEntity = gameEntity;

			List<CS_UsableObject> usableObjects = FindUsableObjects();

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

			List<CS_UsableObject> usableObjects = FindUsableObjects();

			// For each matching usable object, stop listening to its OnUse event
			foreach (CS_UsableObject usableObject in usableObjects)
			{
				usableObject.OnUse -= OnUse;
			}
		}

		private List<CS_UsableObject> FindUsableObjects()
		{
			List<CS_UsableObject> usableObjects = new List<CS_UsableObject>();

			if (ParentEntityOnly && _gameEntity != null)
			{
				if (_gameEntity.GetFirstScriptOfType<CS_UsableObject>()?.ObjectId == ObjectId)
				{
					usableObjects.Add(_gameEntity.GetFirstScriptOfType<CS_UsableObject>());
				}
				// Retrieve all usable objects with the correct ObjectId among entity children
				foreach (GameEntity entity in _gameEntity.GetChildren())
				{
					CS_UsableObject cs_UsableObject = entity.GetFirstScriptOfType<CS_UsableObject>();
					if (cs_UsableObject?.ObjectId == ObjectId)
					{
						usableObjects.Add(cs_UsableObject);
					}
				}
			}
			else
			{
				foreach (MissionObject missionObject in Mission.Current.MissionObjects)
				{
					if (missionObject is CS_UsableObject cs_UsableObject && cs_UsableObject.ObjectId == ObjectId)
					{
						usableObjects.Add(cs_UsableObject);
					}
				}
			}

			return usableObjects;
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
