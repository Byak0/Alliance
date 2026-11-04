#if !SERVER
using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview
{
	/// <summary>
	/// Scene shared by every character/formation preview tableau: one procedural scene with 32
	/// spawn slots (character, mount, banner, camera frames) handed out to widgets.
	/// The scene lives for the whole session - see docs/native_limitations.md#tableau-scene-lifetime.
	/// </summary>
	public static class ALCharacterTableauSceneCache
	{
		private static Scene _scene;

		private static MatrixFrame[] _characterSlots;
		private static MatrixFrame[] _mountSlots;
		private static MatrixFrame[] _bannerSlots;
		private static MatrixFrame[] _cameraSlots;

		private static bool[] _slotUsed;
		private static int _sharedCursor;

		private const int SlotCount = 32;
		private const float SlotSpacing = 4.0f;
		private const float SlotLineX = -60f;

		public static void Initialize()
		{
			if (_scene != null)
				return;

			// Procedural empty scene: reading the native inventory_character_scene deadlocks
			// intermittently in the kit editor.
			// Workaround: tableau-inventory-scene-deadlock, see docs/native_limitations.md
			_scene = Scene.CreateNewScene(initialize_physics: false, enable_decals: false);
			_scene.SetName("AL_CharacterTableau");
			_scene.DisableStaticShadows(value: true);
			// Known open issue: a fresh scene carries a default fog that hazes tableau renders.
			// see docs/native_limitations.md#scene-default-fog-hazes-tableau

			_characterSlots = new MatrixFrame[SlotCount];
			_mountSlots = new MatrixFrame[SlotCount];
			_bannerSlots = new MatrixFrame[SlotCount];
			_cameraSlots = new MatrixFrame[SlotCount];

			for (int i = 0; i < SlotCount; i++)
			{
				MatrixFrame character = MatrixFrame.Identity;
				character.origin.x = SlotLineX - i * SlotSpacing;
				_characterSlots[i] = character;

				MatrixFrame mount = character;
				mount.origin.x -= 3f;
				_mountSlots[i] = mount;

				MatrixFrame banner = character;
				banner.origin.x += 2f;
				banner.origin.y -= 0.5f;
				_bannerSlots[i] = banner;

				// Characters face +Y with identity rotation - the camera sits on their facing side.
				MatrixFrame camera = character;
				camera.origin.y += 3.6f;
				camera.origin.z += 1.15f;
				_cameraSlots[i] = camera;
			}

			_slotUsed = new bool[SlotCount];
		}

		public static Scene GetScene() => _scene;

		public static SlotData AcquireSlot()
		{
			for (int i = 0; i < SlotCount; i++)
			{
				if (!_slotUsed[i])
				{
					_slotUsed[i] = true;
					return SlotDataOf(i);
				}
			}

			// Exhausted (a list refresh can create the new widgets before finalizing the old
			// ones): share a slot instead of crashing - the two previews briefly overlap.
			Log("ALCharacterTableauSceneCache: all slots in use - sharing a slot.", LogLevel.Warning);
			return SlotDataOf(_sharedCursor++ % SlotCount);
		}

		public static void ReleaseSlot(int index)
		{
			if (index >= 0 && index < SlotCount)
			{
				_slotUsed[index] = false;
			}
			// The scene itself is never destroyed: Workaround: tableau-scene-lifetime,
			// see docs/native_limitations.md
		}

		private static SlotData SlotDataOf(int index)
		{
			return new SlotData
			{
				Index = index,
				CharacterFrame = _characterSlots[index],
				MountFrame = _mountSlots[index],
				BannerFrame = _bannerSlots[index],
				CameraFrame = _cameraSlots[index]
			};
		}

		public struct SlotData
		{
			public int Index;
			public MatrixFrame CharacterFrame;
			public MatrixFrame MountFrame;
			public MatrixFrame BannerFrame;
			public MatrixFrame CameraFrame;
		}
	}
}
#endif
