#if !SERVER
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

		public bool UseFollowPath = false;
		public string PathName = "";
		public float PathStep = 1f;
		public Vec3 RotationInfluence = new Vec3(1f, 1f, 1f);   // X=Side, Y=Up, Z=Forward
		public Vec3 PositionInfluence = new Vec3(1f, 1f, 1f);   // X, Y, Z position blend

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
			propertyName == nameof(AddSuffixToTag) || propertyName == nameof(SuffixStartingIndex) ||
			propertyName == nameof(UseFollowPath) || propertyName == nameof(PathName) ||
			propertyName == nameof(PositionInfluence) || propertyName == nameof(RotationInfluence) ||
			propertyName == nameof(PathStep);

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

				MatrixFrame sourceFrame = new MatrixFrame(sourceEntity.GetGlobalFrame().rotation, Vec3.Zero);
				Vec3 sizeLocal = EntityUtils.GetLocalSizeOrFallback(sourceEntity);
				Vec3 stepLocal = new Vec3(
					(UseRelativeOffset ? RelativeOffset.X * sizeLocal.X : 0f) + (UseConstantOffset ? ConstantOffset.X : 0f),
					(UseRelativeOffset ? RelativeOffset.Y * sizeLocal.Y : 0f) + (UseConstantOffset ? ConstantOffset.Y : 0f),
					(UseRelativeOffset ? RelativeOffset.Z * sizeLocal.Z : 0f) + (UseConstantOffset ? ConstantOffset.Z : 0f)
				);

				MatrixFrame objectOffsetRel = MatrixFrame.Identity;
				if (UseObjectOffset && !string.IsNullOrEmpty(ObjectOffsetEntityName))
				{
					GameEntity driver = Scene.GetFirstEntityWithName(ObjectOffsetEntityName);
					if (driver != null)
					{
						objectOffsetRel = sourceFrame.TransformToLocal(driver.GetGlobalFrame());
					}
					else
					{
						Log($"[CS_Array] ObjectOffset '{ObjectOffsetEntityName}' not found; ignoring.", LogLevel.Warning);
					}
				}

				Path path = null;
				bool usePath = UseFollowPath && !string.IsNullOrEmpty(PathName);
				if (usePath)
				{
					path = Scene.GetPathWithName(PathName);
					if (path == null)
					{
						Log($"[CS_Array] Path '{PathName}' not found; ignoring FollowPath.", LogLevel.Warning);
						usePath = false;
					}
				}

				ClampInfluenceValues();

				Vec3 totalOffsetLocal = Vec3.Zero;
				MatrixFrame cloneFrame = sourceFrame;

				for (int i = 0; i < Count; i++)
				{
					float distance = i * PathStep;

					if (usePath)
					{
						if (distance > path.TotalDistance)
						{
							Log($"[CS_Array] Skipping entity {i}, distance {distance} exceeds path length {path.TotalDistance}", LogLevel.Warning);
							break;
						}

						Vec3 currentPos = path.GetFrameForDistance(distance).origin;
						Vec3 nextPos = path.GetFrameForDistance(MathF.Min(distance + 0.1f, path.TotalDistance)).origin;

						Vec3 pathDir = (nextPos - currentPos).NormalizedCopy();
						Vec3 worldUp = Vec3.Up;
						if (MathF.Abs(Vec3.DotProduct(pathDir, worldUp)) > 0.999f)
							worldUp = Vec3.Side;

						// Create path frame
						Vec3 pathF = pathDir;
						Vec3 pathS = Vec3.CrossProduct(worldUp, pathF).NormalizedCopy();
						Vec3 pathU = Vec3.CrossProduct(pathF, pathS).NormalizedCopy();
						Mat3 pathRot = new Mat3(pathS, pathF, pathU);
						pathRot.Orthonormalize();
						MatrixFrame pathFrame = new MatrixFrame(pathRot, currentPos);

						// Blend rotation & position
						cloneFrame = EntityUtils.BlendEulerRotation(sourceFrame, pathFrame, RotationInfluence.x, RotationInfluence.y, RotationInfluence.z);
						cloneFrame = EntityUtils.BlendPositionTowards(cloneFrame, pathFrame, PositionInfluence.x, PositionInfluence.y, PositionInfluence.z);

						if ((UseRelativeOffset || UseConstantOffset) && i > 0)
						{
							totalOffsetLocal += stepLocal;
							cloneFrame.origin += cloneFrame.rotation.TransformToParent(totalOffsetLocal);
						}
					}
					else
					{
						if (i > 0)
						{
							if (UseObjectOffset)
								cloneFrame = cloneFrame.TransformToParent(objectOffsetRel);

							totalOffsetLocal += stepLocal;
							cloneFrame.origin += cloneFrame.rotation.TransformToParent(totalOffsetLocal);
						}
						else
						{
							cloneFrame = sourceFrame;
						}
					}

					if (!EntityUtils.TryCreateEditableCopy(Scene, sourceEntity, out GameEntity clone))
					{
						Log("[CS_Array] Aborting generation, can't copy entity", LogLevel.Error);
						return;
					}

					clone.SetGlobalFrame(cloneFrame);

					// Parent under this tool entity for easy cleanup
					GameEntity.AddChild(clone.WeakEntity);

					clone.Name = $"{SuffixStartingIndex + i}_{EntityToDuplicate}";

					if (ApplyTag)
						clone.AddTag(AddSuffixToTag ? $"{TagToApply}{SuffixStartingIndex + i}" : TagToApply);
				}

				Log($"[CS_Array] Generated {Count} entities{(usePath ? " along path" : "")}.", LogLevel.Information);
				MBEditor.UpdateSceneTree(true); // Refresh editor tree to show new entities
			}
			finally
			{
				_isGenerating = false;
			}
		}

		private void ClampInfluenceValues()
		{
			RotationInfluence.x = MathF.Clamp(RotationInfluence.x, 0f, 1f);
			RotationInfluence.y = MathF.Clamp(RotationInfluence.y, 0f, 1f);
			RotationInfluence.z = MathF.Clamp(RotationInfluence.z, 0f, 1f);

			PositionInfluence.x = MathF.Clamp(PositionInfluence.x, 0f, 1f);
			PositionInfluence.y = MathF.Clamp(PositionInfluence.y, 0f, 1f);
			PositionInfluence.z = MathF.Clamp(PositionInfluence.z, 0f, 1f);
		}
	}
}
#endif