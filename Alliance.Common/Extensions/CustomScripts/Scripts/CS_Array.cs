using Alliance.Common.Core.Utils;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
	/// <summary>
	/// Blender-like Array: duplicate objects, apply relative/constant offsets, use optional object offset.
	/// </summary>
	public class CS_Array : ScriptComponentBehavior
	{
		public string REMINDER = "Save often, can crash anytime";
		public SimpleButton HOW_TO_USE;

		// Source & count
		public string EntityToDuplicate = "entity_name";
		public int Count = 2;

		// Offsets (can be combined)
		public bool UseRelativeOffset = true;
		public Vec3 RelativeOffset = new Vec3(1f, 0f, 0f); // Blender default: 1 on X

		public bool UseConstantOffset = false;
		public Vec3 ConstantOffset = new Vec3(0f, 0f, 0f);

		public bool UseObjectOffset = false;
		public string ObjectOffsetEntityName = "";

		// Tagging
		public bool ApplyTag = false;
		public string TagToApply = "";
		public bool AddSuffixToTag = false;
		public int SuffixStartingIndex = 0;

		// UI
		public SimpleButton GENERATE;
		public SimpleButton RESET;
		public bool Live = false;

		private float _lastLiveUpdateTime = 0f;
		private const float LIVE_UPDATE_INTERVAL = 0.5f;
		private bool _isGenerating = false;
		private MatrixFrame _entityPreviousFrame = new MatrixFrame();
		private MatrixFrame _offsetPreviousFrame = new MatrixFrame();

		// React to properties changes in the editor
		protected override void OnEditorVariableChanged(string variableName)
		{
			if (variableName == nameof(HOW_TO_USE)) { DisplayHowToUse(); return; }
			if (variableName == nameof(GENERATE)) { Generate(); return; }
			if (variableName == nameof(RESET)) { Reset(); return; }
			if (variableName == nameof(Live) && Live) { Generate(); return; }
			if (Live && IsLiveRelevant(variableName)) Generate();
		}

		private bool IsLiveRelevant(string propertyName) =>
			propertyName == nameof(EntityToDuplicate) || propertyName == nameof(Count) ||
			propertyName == nameof(UseRelativeOffset) || propertyName == nameof(RelativeOffset) ||
			propertyName == nameof(UseConstantOffset) || propertyName == nameof(ConstantOffset) ||
			propertyName == nameof(UseObjectOffset) || propertyName == nameof(ObjectOffsetEntityName) ||
			propertyName == nameof(ApplyTag) || propertyName == nameof(TagToApply) ||
			propertyName == nameof(AddSuffixToTag) || propertyName == nameof(SuffixStartingIndex);

		protected override void OnEditorTick(float dt)
		{
			base.OnEditorTick(dt);

			if (Live && !EntityToDuplicate.IsEmpty())
			{
				if (_lastLiveUpdateTime <= LIVE_UPDATE_INTERVAL)
				{
					_lastLiveUpdateTime += dt;
					return;
				}
				_lastLiveUpdateTime = 0f;

				GameEntity sourceEntity = Scene.GetFirstEntityWithName(EntityToDuplicate);
				if (sourceEntity == null) return;

				// Regenerate if source entity moved
				if (_entityPreviousFrame != sourceEntity.GetGlobalFrame())
				{
					_entityPreviousFrame = sourceEntity.GetGlobalFrame();
					Generate();
				}
				// Also regenerate if object offset entity moved
				else if (UseObjectOffset && !ObjectOffsetEntityName.IsEmpty())
				{
					GameEntity offsetEntity = Scene.GetFirstEntityWithName(ObjectOffsetEntityName);
					if (offsetEntity != null && _offsetPreviousFrame != offsetEntity.GetGlobalFrame())
					{
						_offsetPreviousFrame = offsetEntity.GetGlobalFrame();
						Generate();
					}
				}
			}

		}

		private void DisplayHowToUse()
		{
			string message = "================ CS_Array ================\n" +
				"This script is similar to Blender's \"Array\" modifier.\n" +
				"1. Set the source entity name.\n" +
				"2. Specify the number of duplicates.\n" +
				"3. Choose relative/constant offsets or an object offset. You can mix them.\n" +
				"4. Optionally apply a tag to each duplicate/and add a suffix.\n" +
				"5. Click 'Generate' to create the duplicates.\n" +
				"6. Remove the script once you're satisfied with generation.\n" +
				"Note: Toggle live mode to regenerate on changes.\n" +
				"==========================================";
			Log(message, LogLevel.Information);
		}

		private void Reset()
		{
			GameEntity.RemoveAllChildren();
			Log("[CS_Array] Reset: removed generated children.", LogLevel.Information);
		}

		public void Generate()
		{
			if (_isGenerating) return;
			_isGenerating = true;

			try
			{
				Reset();

				if (Count <= 0)
				{
					Log("[CS_Array] Nothing to generate: Count <= 0.", LogLevel.Warning);
					return;
				}

				if (EntityToDuplicate.IsEmpty() || EntityToDuplicate.Contains(' '))
				{
					Log("[CS_Array] Invalid EntityToDuplicate name.", LogLevel.Warning);
					return;
				}

				GameEntity sourceEntity = Scene.GetFirstEntityWithName(EntityToDuplicate);
				if (sourceEntity == null)
				{
					Log($"[CS_Array] Source '{EntityToDuplicate}' not found.", LogLevel.Warning);
					return;
				}

				// Base (original) global frame
				MatrixFrame sourceFrame = sourceEntity.GetGlobalFrame();

				// Relative offset uses LOCAL mesh size, not transform scale (Blender behavior)
				Vec3 sizeLocal = EntityUtils.GetLocalSizeOrFallback(sourceEntity);

				// Per-iteration local translation (applied once per step)
				Vec3 stepLocal = new Vec3(
					(UseRelativeOffset ? RelativeOffset.X * sizeLocal.X : 0f) + (UseConstantOffset ? ConstantOffset.X : 0f),
					(UseRelativeOffset ? RelativeOffset.Y * sizeLocal.Y : 0f) + (UseConstantOffset ? ConstantOffset.Y : 0f),
					(UseRelativeOffset ? RelativeOffset.Z * sizeLocal.Z : 0f) + (UseConstantOffset ? ConstantOffset.Z : 0f)
				);

				// Object Offset relative (source^-1 * object)
				MatrixFrame objectOffsetRel = MatrixFrame.Identity;
				bool useObjectOffset = UseObjectOffset && !string.IsNullOrEmpty(ObjectOffsetEntityName);

				if (useObjectOffset)
				{
					GameEntity driver = Scene.GetFirstEntityWithName(ObjectOffsetEntityName);
					if (driver == null)
					{
						useObjectOffset = false;
						Log($"[CS_Array] ObjectOffset '{ObjectOffsetEntityName}' not found; ignoring.", LogLevel.Warning);
					}
					else
					{
						objectOffsetRel = sourceFrame.TransformToLocal(driver.GetGlobalFrame());
					}
				}

				// Accumulate from the source pose; include original as first duplicate (index 0)
				MatrixFrame cloneFrame = sourceFrame;

				for (int i = 0; i < Count; i++)
				{
					if (i > 0)
					{
						if (useObjectOffset) cloneFrame = cloneFrame.TransformToParent(objectOffsetRel); // rotate/scale/translate once

						cloneFrame.origin = cloneFrame.TransformToParent(stepLocal);   // then translate once in local axes
					}

					// Create editor-modifiable copy
					if (!EntityUtils.TryCreateEditableCopy(Scene, sourceEntity, out GameEntity clone))
					{
						Log($"[CS_Array] Aborting generation, can't copy entity", LogLevel.Error);
						return;
					}

					clone.SetGlobalFrame(cloneFrame);

					// Parent under this tool entity for easy cleanup
					GameEntity.AddChild(clone);

					clone.Name = $"{SuffixStartingIndex + i}_{EntityToDuplicate}";

					if (ApplyTag) clone.AddTag(AddSuffixToTag ? $"{TagToApply}{SuffixStartingIndex + i}" : TagToApply);
				}

				Log($"[CS_Array] Generated {Count} entities.", LogLevel.Information);

				MBEditor.UpdateSceneTree(); // Refresh editor tree to show new entities
			}
			finally
			{
				_isGenerating = false;
			}
		}
	}
}