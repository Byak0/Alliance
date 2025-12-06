#if !SERVER
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.TwoDimension;

namespace Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview
{
	public class AL_CharacterTableauTextureProvider : TextureProvider
	{
		private AL_CharacterTableau _characterTableau;
		private TaleWorlds.Engine.Texture _texture;
		private Texture _providedTexture;
		private bool _isVisible;
		private float _cameraElevation, _cameraStrafe, _cameraYaw, _cameraPitch, _cameraRoll;

		public AL_CharacterTableau CharacterTableau => _characterTableau;

		public float CameraZoom { get; set; }

		public float CameraElevation
		{
			set => _cameraElevation = value;
		}

		public float CameraStrafe
		{
			set => _cameraStrafe = value;
		}

		public float CameraYaw
		{
			set => _cameraYaw = value;
		}

		public float CameraPitch
		{
			set => _cameraPitch = value;
		}

		public float CameraRoll
		{
			set => _cameraRoll = value;
		}

		public float CameraAnimDuration { get; set; }

		public bool ApplyCameraChange
		{
			set
			{
				if (!value) return;
				ApplyCamera();
			}
		}

		private void ApplyCamera()
		{
			_characterTableau.AnimateCamera(_cameraElevation, _cameraStrafe, CameraZoom, _cameraPitch, _cameraYaw, _cameraRoll, CameraFov, CameraAnimDuration);
		}

		public float CameraFov
		{
			get => _characterTableau.CameraFov;
			set => _characterTableau.CameraFov = value;
		}


		public bool EnableLight
		{
			get
			{
				return _characterTableau?.IsLightEnabled ?? false;
			}
			set
			{
				if (value)
				{
					_characterTableau.EnableLight();
				}
				else
				{
					_characterTableau.DisableLight();
				}
			}
		}

		public float CustomAnimationProgressRatio => _characterTableau.GetCustomAnimationProgressRatio();

		public string BannerCodeText
		{
			set
			{
				_characterTableau.SetBannerCode(value);
			}
		}

		public string BodyProperties
		{
			set
			{
				_characterTableau.SetBodyProperties(value);
			}
		}

		public int StanceIndex
		{
			set
			{
				_characterTableau.SetStanceIndex(value);
			}
		}

		public bool IsFemale
		{
			set
			{
				_characterTableau.SetIsFemale(value);
			}
		}

		public int Race
		{
			set
			{
				_characterTableau.SetRace(value);
			}
		}

		public bool IsBannerShownInBackground
		{
			set
			{
				_characterTableau.SetIsBannerShownInBackground(value);
			}
		}

		public bool IsEquipmentAnimActive
		{
			set
			{
				_characterTableau.SetIsEquipmentAnimActive(value);
			}
		}

		public string EquipmentCode
		{
			set
			{
				_characterTableau.SetEquipmentCode(value);
			}
		}

		public string IdleAction
		{
			set
			{
				_characterTableau.SetIdleAction(value);
			}
		}

		public string IdleFaceAnim
		{
			set
			{
				_characterTableau.SetIdleFaceAnim(value);
			}
		}

		public bool CurrentlyRotating
		{
			set
			{
				_characterTableau.RotateCharacter(value);
			}
		}

		public string MountCreationKey
		{
			set
			{
				_characterTableau.SetMountCreationKey(value);
			}
		}

		public uint ArmorColor1
		{
			set
			{
				_characterTableau.SetArmorColor1(value);
			}
		}

		public uint ArmorColor2
		{
			set
			{
				_characterTableau.SetArmorColor2(value);
			}
		}

		public string CharStringId
		{
			set
			{
				_characterTableau.SetCharStringID(value);
			}
		}

		public bool TriggerCharacterMountPlacesSwap
		{
			set
			{
				_characterTableau.TriggerCharacterMountPlacesSwap();
			}
		}

		public float CustomRenderScale
		{
			set
			{
				_characterTableau.SetCustomRenderScale(value);
			}
		}

		public bool IsPlayingCustomAnimations
		{
			get
			{
				return _characterTableau?.IsRunningCustomAnimation ?? false;
			}
			set
			{
				if (value)
				{
					_characterTableau.StartCustomAnimation();
				}
				else
				{
					_characterTableau.StopCustomAnimation();
				}
			}
		}

		public bool ShouldLoopCustomAnimation
		{
			get
			{
				return _characterTableau.ShouldLoopCustomAnimation;
			}
			set
			{
				_characterTableau.ShouldLoopCustomAnimation = value;
			}
		}

		public int LeftHandWieldedEquipmentIndex
		{
			set
			{
				_characterTableau.SetLeftHandWieldedEquipmentIndex(value);
			}
		}

		public int RightHandWieldedEquipmentIndex
		{
			set
			{
				_characterTableau.SetRightHandWieldedEquipmentIndex(value);
			}
		}

		public float CustomAnimationWaitDuration
		{
			set
			{
				_characterTableau.CustomAnimationWaitDuration = value;
			}
		}

		public string CustomAnimation
		{
			set
			{
				_characterTableau.SetCustomAnimation(value);
			}
		}

		public bool IsVisible
		{
			get
			{
				return _isVisible;
			}
			set
			{
				if (_isVisible != value)
				{
					_isVisible = value;
					_characterTableau.SetEnabled(value);
				}
			}
		}

		public AL_CharacterTableauTextureProvider()
		{
			_characterTableau = new AL_CharacterTableau();
		}

		public override void Clear(bool clearNextFrame)
		{
			_characterTableau.OnFinalize();
			base.Clear(clearNextFrame);
		}

		private void CheckTexture()
		{
			if (_texture != _characterTableau.Texture)
			{
				_texture = _characterTableau.Texture;
				if (_texture != null)
				{
					EngineTexture platformTexture = new EngineTexture(_texture);
					_providedTexture = new Texture(platformTexture);
				}
				else
				{
					_providedTexture = null;
				}
			}
		}

		protected override Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
		{
			CheckTexture();
			return _providedTexture;
		}

		public override void SetTargetSize(int width, int height)
		{
			base.SetTargetSize(width, height);
			_characterTableau.SetTargetSize(width, height);
		}

		public override void Tick(float dt)
		{
			base.Tick(dt);
			CheckTexture();
			_characterTableau.OnTick(dt);
		}
	}
}
#endif