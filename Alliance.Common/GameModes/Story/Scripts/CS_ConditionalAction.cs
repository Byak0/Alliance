using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace Alliance.Common.GameModes.Story.Scripts
{
	/// <summary>
	/// Script component allowing map makers to use conditions and actions outside of scenario.
	/// </summary>
	public class CS_ConditionalAction : ScriptComponentBehavior
	{
		// Button to open the editor
		[EditableScriptComponentVariable(true)]
		public SimpleButton EDIT;

		// ConditionalActionStruct is serialized, split and stored as chunks to bypass 255 character limit of editor
		public string Chunk1;
		public string Chunk2;
		public string Chunk3;
		public string Chunk4;
		public string Chunk5;
		public string Chunk6;
		public string Chunk7;
		public string Chunk8;
		public string Chunk9;
		public string Chunk10;

		// Struct for the conditions and actions, retrieved from the serialized string
		private ConditionalActionStruct _conditionalActionStruct = new ConditionalActionStruct();

		private float _refreshTimer = 0f;

		public CS_ConditionalAction()
		{
		}

		protected override void OnInit()
		{
			base.OnInit();
			_conditionalActionStruct = ScenarioSerializer.DeserializeConditionalActionStruct(GetCombinedChunks());
		}

		protected override void OnEditorInit()
		{
			base.OnEditorInit();
			_conditionalActionStruct = ScenarioSerializer.DeserializeConditionalActionStruct(GetCombinedChunks());
		}

		private string GetCombinedChunks()
		{
			return string.Join("", new List<string> { Chunk1, Chunk2, Chunk3, Chunk4, Chunk5, Chunk6, Chunk7, Chunk8, Chunk9, Chunk10 });
		}

		// Update the chunks with the new serialized string
		private void UpdateChunks(string combinedChunks)
		{
			// Clear the chunks
			for (int i = 1; i <= 10; i++)
			{
				UpdateChunk("", i);
			}

			// Update the chunks, 255 characters at a time
			for (int i = 0, j = 1; i < combinedChunks.Length && j <= 10; i += 255, j++)
			{
				UpdateChunk(combinedChunks.Substring(i, Math.Min(255, combinedChunks.Length - i)), j);
			}
		}

		private void UpdateChunk(string chunk, int index)
		{
			switch (index)
			{
				case 1: Chunk1 = chunk; break;
				case 2: Chunk2 = chunk; break;
				case 3: Chunk3 = chunk; break;
				case 4: Chunk4 = chunk; break;
				case 5: Chunk5 = chunk; break;
				case 6: Chunk6 = chunk; break;
				case 7: Chunk7 = chunk; break;
				case 8: Chunk8 = chunk; break;
				case 9: Chunk9 = chunk; break;
				case 10: Chunk10 = chunk; break;
			}
		}

		public void Enable()
		{
			_conditionalActionStruct.Enabled = true;
		}

		public void Disable()
		{
			_conditionalActionStruct.Enabled = false;
		}

		protected override void OnEditorVariableChanged(string variableName)
		{
			switch (variableName)
			{
				case "EDIT":
					OpenEditor();
					break;
			}
		}

		private void OpenEditor()
		{
			_conditionalActionStruct = ScenarioSerializer.DeserializeConditionalActionStruct(GetCombinedChunks());
			object modifiedObject = EditorToolsManager.OpenEditor(_conditionalActionStruct);
			if (modifiedObject != null)
			{
				_conditionalActionStruct = (ConditionalActionStruct)modifiedObject;
				UpdateChunks(ScenarioSerializer.SerializeConditionalActionStruct(_conditionalActionStruct));
			}
		}

		protected override void OnTick(float dt)
		{
			if (_conditionalActionStruct.Enabled)
			{
				_refreshTimer += dt;
				if (_refreshTimer < _conditionalActionStruct.RefreshDelay) return;
				_refreshTimer = 0f;

				bool conditionsMet = true;
				foreach (Condition condition in _conditionalActionStruct.Conditions)
				{
					if (!condition.Evaluate(ScenarioManager.Instance))
					{
						conditionsMet = false;
						break;
					}
				}

				if (conditionsMet)
				{
					foreach (ActionBase action in _conditionalActionStruct.Actions)
					{
						action.Execute();
					}

					if (_conditionalActionStruct.OneTimeOnly)
					{
						_conditionalActionStruct.Enabled = false;
					}
				}
			}
		}
	}

	public class ConditionalActionStruct
	{
		public List<Condition> Conditions = new List<Condition>();
		public List<ActionBase> Actions = new List<ActionBase>();
		public bool Enabled = true;
		public bool OneTimeOnly = false;
		public float RefreshDelay = 1f;

		public ConditionalActionStruct() { }
	}
}
