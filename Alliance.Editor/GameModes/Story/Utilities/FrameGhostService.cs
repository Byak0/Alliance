using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Editor.GameModes.Story.Views;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Editor.GameModes.Story.Utilities
{
	/// <summary>
	/// Owns the in-editor ghost previews for frame-hosting actions. Many actions can be previewed at once
	/// (one ghost each). Each open <see cref="ViewModels.ObjectEditorViewModel"/> for a Spawn/Move action
	/// registers a ghost keyed by the action object; closing that same editor removes only its own ghost.
	/// This keeps <see cref="ViewModels.FieldViewModel"/> free of any ghost/action/prefab knowledge.
	/// </summary>
	public static class FrameGhostService
	{
		private static readonly Dictionary<object, object> _editorToAction = new();

		/// <summary>Called when an ObjectEditorViewModel opens. Registers a ghost if the object is a
		/// frame-hosting action; otherwise does nothing (nested/unrelated editors are left alone).</summary>
		public static void Attach(object editor, object obj, Func<object> scopeGetter)
		{
			switch (obj)
			{
				case SpawnEntityAction sea:
					_editorToAction[editor] = sea;
					EditFrameView.ShowGhost(
						owner: sea,
						labelGetter: () => "Spawn: " + (sea.Prefab ?? "?"),
						keyGetter: () => sea.Prefab ?? "",
						ghostFactory: () => InstantiatePrefab(sea.Prefab),
						frameGetter: () => sea.Frame?.Resolve(null) ?? MatrixFrame.Identity);
					break;
				case MoveEntityAction mea:
					_editorToAction[editor] = mea;
					// Resolve captured variables to their producing prefab for this scope.
					EditorCaptureRegistry.Build(scopeGetter?.Invoke());
					EditFrameView.ShowGhost(
						owner: mea,
						labelGetter: () => "Move: " + MoveLabel(mea),
						keyGetter: () => MoveGhostKey(mea),
						ghostFactory: () => BuildMoveGhost(mea),
						frameGetter: () => mea.Destination?.Resolve(null) ?? MatrixFrame.Identity);
					break;
			}
		}

		/// <summary>Called when an editor closes. Removes only that editor's ghost.</summary>
		public static void Detach(object editor)
		{
			if (_editorToAction.TryGetValue(editor, out object action))
			{
				EditFrameView.HideGhost(action);
				_editorToAction.Remove(editor);
			}
		}

		private static string MoveGhostKey(MoveEntityAction mea)
		{
			WeakGameEntity e = mea.Entity?.Resolve(null) ?? WeakGameEntity.Invalid;
			if (e.IsValid) return "entity:" + e.Pointer;
			if (mea.Entity is VariableValue<WeakGameEntity> vv) return "var:" + (vv.VariableName ?? "");
			return "";
		}

		private static string MoveLabel(MoveEntityAction mea)
		{
			WeakGameEntity e = mea.Entity?.Resolve(null) ?? WeakGameEntity.Invalid;
			if (e.IsValid) return e.Name;
			if (mea.Entity is VariableValue<WeakGameEntity> vv && !string.IsNullOrEmpty(vv.VariableName)) return vv.VariableName;
			return "?";
		}

		private static GameEntity BuildMoveGhost(MoveEntityAction mea)
		{
			if (MBEditor._editorScene == null) return null;
			WeakGameEntity e = mea.Entity?.Resolve(null) ?? WeakGameEntity.Invalid;
			if (e.IsValid) return GameEntity.CopyFrom(MBEditor._editorScene, e, false, false);
			if (mea.Entity is VariableValue<WeakGameEntity> vv && !string.IsNullOrEmpty(vv.VariableName))
			{
				string prefab = EditorCaptureRegistry.ResolvePrefab(vv.VariableName);
				if (!string.IsNullOrWhiteSpace(prefab)) return InstantiatePrefab(prefab);
			}
			return null;
		}

		private static GameEntity InstantiatePrefab(string prefab)
			=> string.IsNullOrWhiteSpace(prefab) || MBEditor._editorScene == null
				? null
				: GameEntity.Instantiate(MBEditor._editorScene, prefab, false, false);
	}
}
