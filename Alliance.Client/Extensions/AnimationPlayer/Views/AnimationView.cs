using Alliance.Client.Extensions.AnimationPlayer.ViewModels;
using Alliance.Common.Core.KeyBinder;
using Alliance.Common.Core.KeyBinder.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.AnimationPlayer.Views
{
	/// <summary>
	/// View responsible for showing the Animation menu.
	/// Checks gamekeys inputs to show/hide menu and request animations.
	/// </summary>
	[DefaultView]
	public class AnimationView : MissionView, IUseKeyBinder
	{
		private static string KeyCategoryId = "anim_sys";
		BindedKeyCategory IUseKeyBinder.BindedKeys => new BindedKeyCategory()
		{
			CategoryId = KeyCategoryId,
			Category = "Alliance - Animation System",
			Keys = new List<BindedKey>()
				{
					new BindedKey()
					{
						Id = "key_anim_menu",
						Description = "Open the animation menu.",
						Name = "Animation menu",
						DefaultInputKey = InputKey.J,
					},
					new BindedKey()
					{
						Id = "key_anim_self",
						Description = "Press this key + the animation shortcut to play animation on yourself.",
						Name = "Play animation on self",
						DefaultInputKey = InputKey.RightAlt,
					},
					new BindedKey()
					{
						Id = "key_anim_target",
						Description = "Press this key + the animation shortcut to play animation on target.",
						Name = "Play animation on target",
						DefaultInputKey = InputKey.RightControl,
					},
					new BindedKey()
					{
						Id = "key_anim_formation",
						Description = "Press this key + the animation shortcut to play animation on formation.",
						Name = "Play animation on formation",
						DefaultInputKey = InputKey.LeftControl,
					},
					new BindedKey()
					{
						Id = "key_anim_shortcut1",
						Description = "Press a target shortcut + this key to play animation 1.",
						Name = "Animation shortcut 1",
						DefaultInputKey = InputKey.Numpad1,
					},
					new BindedKey()
					{
						Id = "key_anim_shortcut2",
						Description = "Press a target shortcut + this key to play animation 2.",
						Name = "Animation shortcut 2",
						DefaultInputKey = InputKey.Numpad2,
					},
					new BindedKey()
					{
						Id = "key_anim_shortcut3",
						Description = "Press a target shortcut + this key to play animation 3.",
						Name = "Animation shortcut 3",
						DefaultInputKey = InputKey.Numpad3,
					},
					new BindedKey()
					{
						Id = "key_anim_shortcut4",
						Description = "Press a target shortcut + this key to play animation 4.",
						Name = "Animation shortcut 4",
						DefaultInputKey = InputKey.Numpad4,
					},
					new BindedKey()
					{
						Id = "key_anim_shortcut5",
						Description = "Press a target shortcut + this key to play animation 5.",
						Name = "Animation shortcut 5",
						DefaultInputKey = InputKey.Numpad5,
					},
					new BindedKey()
					{
						Id = "key_anim_shortcut6",
						Description = "Press a target shortcut + this key to play animation 6.",
						Name = "Animation shortcut 6",
						DefaultInputKey = InputKey.Numpad6,
					},
					new BindedKey()
					{
						Id = "key_anim_shortcut7",
						Description = "Press a target shortcut + this key to play animation 7.",
						Name = "Animation shortcut 7",
						DefaultInputKey = InputKey.Numpad7,
					},
					new BindedKey()
					{
						Id = "key_anim_shortcut8",
						Description = "Press a target shortcut + this key to play animation 8.",
						Name = "Animation shortcut 8",
						DefaultInputKey = InputKey.Numpad8,
					},
					new BindedKey()
					{
						Id = "key_anim_shortcut9",
						Description = "Press a target shortcut + this key to play animation 9.",
						Name = "Animation shortcut 9",
						DefaultInputKey = InputKey.Numpad9,
					}
				}
		};

		public bool IsMenuOpen;

		private GauntletLayer _layer;
		private AnimationVM _dataSource;
		private GameKey _menuKey;
		private GameKey _selfKey;
		private GameKey _targetKey;
		private GameKey _formationKey;
		private List<GameKey> _animationKeys;
		private bool _initialized;

		public AnimationView()
		{
		}

		public override void EarlyStart()
		{
			AnimationRequestEmitter.Instance.LastRequest = 0;
			_menuKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_anim_menu");
			_selfKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_anim_self");
			_targetKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_anim_target");
			_formationKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_anim_formation");
			_animationKeys = new List<GameKey>();
			for (int i = 1; i <= 9; i++)
			{
				_animationKeys.Add(HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_anim_shortcut" + i));
			}
		}

		public override void OnMissionScreenFinalize()
		{
			if (_initialized)
			{
				_layer.InputRestrictions.ResetInputRestrictions();
				MissionScreen.RemoveLayer(_layer);
				_dataSource.OnCloseMenu -= OnCloseMenu;
				_dataSource.OnFinalize();
				_dataSource = null;
				_layer = null;
				_initialized = false;
			}
		}

		private void Init()
		{
			try
			{
				AnimationUserStore.Instance.Init();
				_dataSource = new AnimationVM();
				_dataSource.OnCloseMenu += OnCloseMenu;
				_layer = new GauntletLayer(25) { };
				_layer.InputRestrictions.SetInputRestrictions();
				_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
				_layer.LoadMovie("AnimationMenu", _dataSource);
				SpriteData spriteData = UIResourceManager.SpriteData;
				TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
				ResourceDepot uiResourceDepot = UIResourceManager.UIResourceDepot;
				spriteData.SpriteCategories["ui_mplobby"].Load(resourceContext, uiResourceDepot);
				MissionScreen.AddLayer(_layer);
				_initialized = true;
			}
			catch (Exception ex)
			{
				Log($"Alliance - Error opening animation menu :", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}

		private void OpenMenu(bool editMode = false)
		{
			if (!_initialized) Init();
			if (_initialized)
			{
				_layer.InputRestrictions.SetInputRestrictions();
				_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
				ScreenManager.TrySetFocus(_layer);
				if (editMode)
				{
					_dataSource.ShowAdminMenu = true;
				}
				else
				{
					_dataSource.ShowPlayerMenu = true;
				}
				_dataSource.IsVisible = true;
				IsMenuOpen = true;
			}
		}

		private void CloseMenu()
		{
			_layer.InputRestrictions.ResetInputRestrictions();
			_dataSource.ShowAdminMenu = false;
			_dataSource.ShowPlayerMenu = false;
			_dataSource.IsVisible = false;
			IsMenuOpen = false;
		}

		private void OnCloseMenu(object sender, EventArgs e)
		{
			CloseMenu();
		}

		public override void OnMissionScreenTick(float dt)
		{
			TickInputs();
		}

		private void TickInputs()
		{
			if (IsMenuOpen)
			{
				if (Input.IsKeyPressed(_menuKey.KeyboardKey.InputKey) || Input.IsKeyPressed(_menuKey.ControllerKey.InputKey) || _layer.Input.IsKeyPressed(InputKey.RightMouseButton) || _layer.Input.IsHotKeyReleased("Exit"))
				{
					CloseMenu();
				}
			}
			else
			{
				if (Input.IsKeyPressed(_menuKey.KeyboardKey.InputKey) || Input.IsKeyPressed(_menuKey.ControllerKey.InputKey))
				{
					if (Input.IsKeyPressed(InputKey.LeftControl) || Input.IsKeyDown(InputKey.LeftControl))
					{
						if (GameNetwork.MyPeer.IsAdmin())
						{
							OpenMenu(editMode: true);
						}
					}
					else
					{
						OpenMenu();
					}
				}
			}

			CheckTargetShortcuts();
		}

		private void CheckTargetShortcuts()
		{
			TargetType targetType = TargetType.None;

			if (Input.IsKeyDown(_selfKey.KeyboardKey.InputKey) || Input.IsKeyDown(_selfKey.ControllerKey.InputKey))
			{
				targetType = TargetType.Self;
			}
			else if (Input.IsKeyDown(_targetKey.KeyboardKey.InputKey) || Input.IsKeyDown(_targetKey.ControllerKey.InputKey))
			{
				targetType = TargetType.Target;
			}
			else if (Input.IsKeyDown(_formationKey.KeyboardKey.InputKey) || Input.IsKeyDown(_formationKey.ControllerKey.InputKey))
			{
				targetType = TargetType.Formation;
			}

			if (targetType != TargetType.None)
			{
				CheckAnimationShortcuts(targetType);
			}
		}

		private void CheckAnimationShortcuts(TargetType targetType)
		{
			//AnimationSet animSet = AnimationUserStore.Instance.AnimationSets.ElementAtOrDefault(_dataSource != null ? _dataSource.SelectedSet : 0);
			AnimationSet animSet = _dataSource != null ? _dataSource.SelectedAnimSet : AnimationUserStore.Instance.AnimationSets?.ElementAtOrDefault(0);

			if (_dataSource == null || animSet == null) return;

			for (int i = 0; i < _animationKeys.Count; i++)
			{
				GameKey animationKey = _animationKeys[i];
				if (Input.IsKeyPressed(animationKey.KeyboardKey.InputKey) || Input.IsKeyPressed(animationKey.ControllerKey.InputKey))
				{
					if (IsMenuOpen)
					{
						CloseMenu();
					}
					AnimationSequence animationSequence = AnimationUserStore.Instance.AnimationSequences.Find(x => x.Index == animSet.BindedAnimSequence[i]?.AnimSequenceIndex);
					switch (targetType)
					{
						case TargetType.Self:
							AnimationRequestEmitter.Instance.RequestAnimationSequenceForTarget(animationSequence, Agent.Main);
							break;
						case TargetType.Target:
							AnimationRequestEmitter.Instance.RequestAnimationSequenceForTarget(animationSequence, GetTargettedAgent());
							break;
						case TargetType.Formation:
							MBReadOnlyList<Formation> formations = Mission.Current.PlayerTeam?.PlayerOrderController?.SelectedFormations;
							if (formations != null && formations.Count > 0)
							{
								foreach (Formation formation in formations)
								{
									AnimationRequestEmitter.Instance.RequestAnimationSequenceForFormation(animationSequence, formation);
								}
							}
							else if (Agent.Main?.Formation != null)
							{
								AnimationRequestEmitter.Instance.RequestAnimationSequenceForFormation(animationSequence, Agent.Main.Formation);
							}
							break;
					}
				}
			}
		}

		/// <summary>
		/// Return agent under cursor (middle of the screen if cursor is not visible).
		/// </summary>        
		private Agent GetTargettedAgent()
		{
			MissionScreen.ScreenPointToWorldRay(Input.GetMousePositionRanged(), out var rayBegin, out var rayEnd);
			return Mission.Current.RayCastForClosestAgent(rayBegin, rayEnd, out _, -1, 0.1f);
		}
	}

	public enum TargetType
	{
		None,
		Self,
		Target,
		Formation
	}
}
