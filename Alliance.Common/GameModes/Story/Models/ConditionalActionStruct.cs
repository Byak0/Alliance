using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Conditions;
using System.Collections.Generic;
using System.Xml.Serialization;
using TaleWorlds.Engine;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// A conditional action is a set of conditions and actions that are triggered when the conditions are met.
	/// </summary>
	public class ConditionalActionStruct
	{
		public string Name = "Conditional Action";
		[ConfigProperty(label: "Conditions", tooltip: "If multiple conditions are set, they must all be true to trigger the actions.")]
		public List<Condition> Conditions = new List<Condition>();
		[ConfigProperty(label: "Actions", tooltip: "Actions triggered when conditions are met.")]
		public List<ActionBase> Actions = new List<ActionBase>();
		[ConfigProperty(label: "Enabled", tooltip: "Enable or disable the conditional action.")]
		public bool Enabled = true;
		[ConfigProperty(label: "One Time Only", tooltip: "If true, the conditional action will only trigger once.")]
		public bool OneTimeOnly = false;
		[ConfigProperty(label: "Refresh Delay", tooltip: "Delay between condition checks in seconds. Longer delays are preferable for performance.")]
		public float RefreshDelay = 1f;

		[ConfigProperty(isEditable: false)]
		[XmlIgnore]
		internal float _refreshTimer = 0f;
		[ConfigProperty(isEditable: false)]
		[XmlIgnore]
		internal bool _enabled = false;

		[ConfigProperty(isEditable: false)]
		[XmlIgnore]
		public GameEntity ParentEntity = null;

		public ConditionalActionStruct() { }

		public void Register(GameEntity entity = null)
		{
			_enabled = Enabled;

			if (entity != null)
			{
				ParentEntity = entity;
			}
			foreach (Condition condition in Conditions)
			{
				condition.Register(entity);
			}
			foreach (ActionBase action in Actions)
			{
				action.Register(entity);
			}
		}

		/// <summary>
		/// Check if the conditions are met and execute the actions if they are.
		/// </summary>
		public void Tick(float dt)
		{
			if (_enabled)
			{
				_refreshTimer += dt;
				if (_refreshTimer < RefreshDelay) return;
				_refreshTimer = 0f;

				bool conditionsMet = true;
				foreach (Condition condition in Conditions)
				{
					if (!condition.Evaluate(ScenarioManager.Instance))
					{
						conditionsMet = false;
						break;
					}
				}

				if (conditionsMet)
				{
					foreach (ActionBase action in Actions)
					{
						action.Execute();
					}

					if (OneTimeOnly)
					{
						_enabled = false;
					}
				}
			}
		}
	}
}
