using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.CustomScripts.Scripts;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Check if a CS_UsableObject has been used.
	/// </summary>
	[PhraseTemplate("The script {ObjectId} has been used. Repeat: {AllowMultipleUses}. Restrict to parent entity: {ParentEntityOnly}")]
	public class ObjectUsedCondition : Condition
	{
		[ConfigProperty(label: "Object ID", tooltip: "ID of the script to listen to.")]
		public string ObjectId;
		[ConfigProperty(label: "Allow multiple use", tooltip: "If enabled, condition can be triggered every time the object is used.")]
		public bool AllowMultipleUses;
		[ConfigProperty(label: "Restrict to parent entity", tooltip: "If enabled, condition will only check its parent entity and children (entity MUST have any MissionObject script, can be AL_TriggerAction).")]
		public bool ParentEntityOnly;
		[ConfigProperty(label: "Captured user", tooltip: "Agent who used the object will be stored under this temporary variable name within this scripted event.")]
		[VariableOutput(typeof(Agent))]
		public string User = "User";

		private bool _used;
		private WeakGameEntity _gameEntity;
		private Agent _lastUser;

		public ObjectUsedCondition() { }

		public override void Register(WeakGameEntity gameEntity)
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

			if (ParentEntityOnly && _gameEntity.IsValid)
			{
				if (_gameEntity.GetFirstScriptOfType<CS_UsableObject>()?.ObjectId == ObjectId)
				{
					usableObjects.Add(_gameEntity.GetFirstScriptOfType<CS_UsableObject>());
				}
				// Retrieve all usable objects with the correct ObjectId among entity children
				foreach (WeakGameEntity entity in _gameEntity.GetChildren())
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

		private void OnUse(Agent userAgent)
		{
			_used = true;
			_lastUser = userAgent;

			if (!AllowMultipleUses)
			{
				Unregister();
			}
		}

		public override bool Evaluate(VariableStore context)
		{
			// If the object has been used, return true and reset the used flag (unless AllowMultipleUses is false)
			if (_used)
			{
				context?.Set(User, _lastUser);
				_used = !AllowMultipleUses;
				return true;
			}

			return false;
		}
	}
}
