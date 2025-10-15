#if !SERVER
using System.Linq;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview
{
	public class AL_CharacterTableauWidget : TextureWidget
	{
		private ButtonWidget _swapPlacesButtonWidget;
		private string _bannerCode;
		private string _bodyProperties;
		private string _charStringId;
		private string _equipmentCode;
		private string _mountCreationKey;
		private string _idleAction;
		private string _idleFaceAnim;
		private string _customAnimation;
		private int _leftHandWieldedEquipmentIndex;
		private int _rightHandWieldedEquipmentIndex;
		private uint _armorColor1;
		private uint _armorColor2;
		private int _stanceIndex;
		private int _race;
		private bool _isEquipmentAnimActive;
		private bool _isFemale;
		private bool _isCharacterMountSwapped;
		private bool _isBannerShownInBackground;
		private bool _isPlayingCustomAnimations;
		private bool _shouldLoopCustomAnimation;
		private float _customAnimationProgressRatio;
		private float _customRenderScale;
		private float _customAnimationWaitDuration;
		private float _cameraFov;
		private float _cameraZoom;
		private float _cameraElevation;
		private float _cameraStrafe;
		private float _cameraYaw;
		private float _cameraPitch;
		private float _cameraRoll;
		private float _cameraAnimDuration;
		private bool _enableLight;

		[Editor(false)]
		public float CameraFov
		{
			get => _cameraFov;
			set
			{
				if (_cameraFov != value)
				{
					_cameraFov = value;
					OnPropertyChanged(value, nameof(CameraFov));
					SetTextureProviderProperty("CameraFov", value);
				}
			}
		}

		[Editor(false)]
		public float CameraZoom
		{
			get => _cameraZoom;
			set
			{
				if (_cameraZoom != value)
				{
					_cameraZoom = value;
					OnPropertyChanged(value, nameof(CameraZoom));
					SetTextureProviderProperty(nameof(CameraZoom), value);
				}
			}
		}

		[Editor(false)]
		public float CameraElevation
		{
			get => _cameraElevation;
			set
			{
				if (_cameraElevation != value)
				{
					_cameraElevation = value;
					OnPropertyChanged(value, nameof(CameraElevation));
					SetTextureProviderProperty(nameof(CameraElevation), value);
				}
			}
		}

		[Editor(false)]
		public float CameraStrafe
		{
			get => _cameraStrafe;
			set
			{
				if (_cameraStrafe != value)
				{
					_cameraStrafe = value;
					OnPropertyChanged(value, nameof(CameraStrafe));
					SetTextureProviderProperty(nameof(CameraStrafe), value);
				}
			}
		}

		[Editor(false)]
		public float CameraYaw
		{
			get => _cameraYaw;
			set
			{
				if (_cameraYaw != value)
				{
					_cameraYaw = value;
					OnPropertyChanged(value, nameof(CameraYaw));
					SetTextureProviderProperty(nameof(CameraYaw), value);
				}
			}
		}

		[Editor(false)]
		public float CameraPitch
		{
			get => _cameraPitch;
			set
			{
				if (_cameraPitch != value)
				{
					_cameraPitch = value;
					OnPropertyChanged(value, nameof(CameraPitch));
					SetTextureProviderProperty(nameof(CameraPitch), value);
				}
			}
		}

		[Editor(false)]
		public float CameraRoll
		{
			get => _cameraRoll;
			set
			{
				if (_cameraRoll != value)
				{
					_cameraRoll = value;
					OnPropertyChanged(value, nameof(CameraRoll));
					SetTextureProviderProperty(nameof(CameraRoll), value);
				}
			}
		}

		[Editor(false)]
		public bool ApplyCameraChange
		{
			get => true;
			set
			{
				if (!value) return;

				SetTextureProviderProperty(nameof(ApplyCameraChange), true);
			}
		}

		[Editor(false)]
		public float CameraAnimDuration
		{
			get => _cameraAnimDuration;
			set
			{
				if (_cameraAnimDuration != value)
				{
					_cameraAnimDuration = value;
					OnPropertyChanged(value, nameof(CameraAnimDuration));
					SetTextureProviderProperty(nameof(CameraAnimDuration), value);
				}
			}
		}

		[Editor(false)]
		public bool EnableLight
		{
			get
			{
				return _enableLight;
			}
			set
			{
				if (value != _enableLight)
				{
					_enableLight = value;
					OnPropertyChanged(value, "EnableLight");
					SetTextureProviderProperty("EnableLight", value);
				}
			}
		}

		[Editor(false)]
		public string BannerCodeText
		{
			get
			{
				return _bannerCode;
			}
			set
			{
				if (value != _bannerCode)
				{
					_bannerCode = value;
					OnPropertyChanged(value, "BannerCodeText");
					SetTextureProviderProperty("BannerCodeText", value);
				}
			}
		}

		[Editor(false)]
		public ButtonWidget SwapPlacesButtonWidget
		{
			get
			{
				return _swapPlacesButtonWidget;
			}
			set
			{
				if (value != _swapPlacesButtonWidget)
				{
					_swapPlacesButtonWidget = value;
					OnPropertyChanged(value, "SwapPlacesButtonWidget");
					if (value != null)
					{
						_swapPlacesButtonWidget.ClickEventHandlers.Add(OnSwapClick);
					}
				}
			}
		}

		[Editor(false)]
		public string BodyProperties
		{
			get
			{
				return _bodyProperties;
			}
			set
			{
				if (value != _bodyProperties)
				{
					_bodyProperties = value;
					OnPropertyChanged(value, "BodyProperties");
					SetTextureProviderProperty("BodyProperties", value);
				}
			}
		}

		[Editor(false)]
		public float CustomAnimationProgressRatio
		{
			get
			{
				return _customAnimationProgressRatio;
			}
			set
			{
				if (value != _customAnimationProgressRatio)
				{
					_customAnimationProgressRatio = value;
					OnPropertyChanged(value, "CustomAnimationProgressRatio");
				}
			}
		}

		[Editor(false)]
		public float CustomRenderScale
		{
			get
			{
				return _customRenderScale;
			}
			set
			{
				if (value != _customRenderScale)
				{
					_customRenderScale = value;
					OnPropertyChanged(value, "CustomRenderScale");
					SetTextureProviderProperty("CustomRenderScale", value);
				}
			}
		}

		[Editor(false)]
		public float CustomAnimationWaitDuration
		{
			get
			{
				return _customAnimationWaitDuration;
			}
			set
			{
				if (value != _customAnimationWaitDuration)
				{
					_customAnimationWaitDuration = value;
					OnPropertyChanged(value, "CustomAnimationWaitDuration");
					SetTextureProviderProperty("CustomAnimationWaitDuration", value);
				}
			}
		}

		[Editor(false)]
		public string CharStringId
		{
			get
			{
				return _charStringId;
			}
			set
			{
				if (value != _charStringId)
				{
					_charStringId = value;
					OnPropertyChanged(value, "CharStringId");
					SetTextureProviderProperty("CharStringId", value);
				}
			}
		}

		[Editor(false)]
		public int StanceIndex
		{
			get
			{
				return _stanceIndex;
			}
			set
			{
				if (value != _stanceIndex)
				{
					_stanceIndex = value;
					OnPropertyChanged(value, "StanceIndex");
					SetTextureProviderProperty("StanceIndex", value);
				}
			}
		}

		[Editor(false)]
		public bool IsEquipmentAnimActive
		{
			get
			{
				return _isEquipmentAnimActive;
			}
			set
			{
				if (value != _isEquipmentAnimActive)
				{
					_isEquipmentAnimActive = value;
					OnPropertyChanged(value, "IsEquipmentAnimActive");
					SetTextureProviderProperty("IsEquipmentAnimActive", value);
				}
			}
		}

		[Editor(false)]
		public bool IsFemale
		{
			get
			{
				return _isFemale;
			}
			set
			{
				if (value != _isFemale)
				{
					_isFemale = value;
					OnPropertyChanged(value, "IsFemale");
					SetTextureProviderProperty("IsFemale", value);
				}
			}
		}

		[Editor(false)]
		public int Race
		{
			get
			{
				return _race;
			}
			set
			{
				if (value != _race)
				{
					_race = value;
					OnPropertyChanged(value, "Race");
					SetTextureProviderProperty("Race", value);
				}
			}
		}

		[Editor(false)]
		public string EquipmentCode
		{
			get
			{
				return _equipmentCode;
			}
			set
			{
				if (value != _equipmentCode)
				{
					_equipmentCode = value;
					OnPropertyChanged(value, "EquipmentCode");
					SetTextureProviderProperty("EquipmentCode", value);
				}
			}
		}

		[Editor(false)]
		public string MountCreationKey
		{
			get
			{
				return _mountCreationKey;
			}
			set
			{
				if (value != _mountCreationKey)
				{
					_mountCreationKey = value;
					OnPropertyChanged(value, "MountCreationKey");
					SetTextureProviderProperty("MountCreationKey", value);
				}
			}
		}

		[Editor(false)]
		public string IdleAction
		{
			get
			{
				return _idleAction;
			}
			set
			{
				if (value != _idleAction)
				{
					_idleAction = value;
					OnPropertyChanged(value, "IdleAction");
					SetTextureProviderProperty("IdleAction", value);
				}
			}
		}

		[Editor(false)]
		public string IdleFaceAnim
		{
			get
			{
				return _idleFaceAnim;
			}
			set
			{
				if (value != _idleFaceAnim)
				{
					_idleFaceAnim = value;
					OnPropertyChanged(value, "IdleFaceAnim");
					SetTextureProviderProperty("IdleFaceAnim", value);
				}
			}
		}

		[Editor(false)]
		public string CustomAnimation
		{
			get
			{
				return _customAnimation;
			}
			set
			{
				if (value != _customAnimation)
				{
					_customAnimation = value;
					OnPropertyChanged(value, "CustomAnimation");
					SetTextureProviderProperty("CustomAnimation", value);
				}
			}
		}

		[Editor(false)]
		public int LeftHandWieldedEquipmentIndex
		{
			get
			{
				return _leftHandWieldedEquipmentIndex;
			}
			set
			{
				if (value != _leftHandWieldedEquipmentIndex)
				{
					_leftHandWieldedEquipmentIndex = value;
					OnPropertyChanged(value, "LeftHandWieldedEquipmentIndex");
					SetTextureProviderProperty("LeftHandWieldedEquipmentIndex", value);
				}
			}
		}

		[Editor(false)]
		public int RightHandWieldedEquipmentIndex
		{
			get
			{
				return _rightHandWieldedEquipmentIndex;
			}
			set
			{
				if (value != _rightHandWieldedEquipmentIndex)
				{
					_rightHandWieldedEquipmentIndex = value;
					OnPropertyChanged(value, "RightHandWieldedEquipmentIndex");
					SetTextureProviderProperty("RightHandWieldedEquipmentIndex", value);
				}
			}
		}

		[Editor(false)]
		public uint ArmorColor1
		{
			get
			{
				return _armorColor1;
			}
			set
			{
				if (value != _armorColor1)
				{
					_armorColor1 = value;
					OnPropertyChanged(value, "ArmorColor1");
					SetTextureProviderProperty("ArmorColor1", value);
				}
			}
		}

		[Editor(false)]
		public uint ArmorColor2
		{
			get
			{
				return _armorColor2;
			}
			set
			{
				if (value != _armorColor2)
				{
					_armorColor2 = value;
					OnPropertyChanged(value, "ArmorColor2");
					SetTextureProviderProperty("ArmorColor2", value);
				}
			}
		}

		[Editor(false)]
		public bool IsBannerShownInBackground
		{
			get
			{
				return _isBannerShownInBackground;
			}
			set
			{
				if (value != _isBannerShownInBackground)
				{
					_isBannerShownInBackground = value;
					OnPropertyChanged(value, "IsBannerShownInBackground");
					SetTextureProviderProperty("IsBannerShownInBackground", value);
				}
			}
		}

		[Editor(false)]
		public bool IsPlayingCustomAnimations
		{
			get
			{
				return _isPlayingCustomAnimations;
			}
			set
			{
				if (value != _isPlayingCustomAnimations)
				{
					_isPlayingCustomAnimations = value;
					OnPropertyChanged(value, "IsPlayingCustomAnimations");
					SetTextureProviderProperty("IsPlayingCustomAnimations", value);
				}
			}
		}

		[Editor(false)]
		public bool ShouldLoopCustomAnimation
		{
			get
			{
				return _shouldLoopCustomAnimation;
			}
			set
			{
				if (value != _shouldLoopCustomAnimation)
				{
					_shouldLoopCustomAnimation = value;
					OnPropertyChanged(value, "ShouldLoopCustomAnimation");
					SetTextureProviderProperty("ShouldLoopCustomAnimation", value);
				}
			}
		}

		public AL_CharacterTableauWidget(UIContext context)
			: base(context)
		{
			TextureProviderName = "AL_CharacterTableauTextureProvider";
		}

		protected override void OnMousePressed()
		{
			SetTextureProviderProperty("CurrentlyRotating", true);
		}

		protected override void OnMouseReleased()
		{
			SetTextureProviderProperty("CurrentlyRotating", false);
		}

		private void OnSwapClick(Widget obj)
		{
			_isCharacterMountSwapped = !_isCharacterMountSwapped;
			SetTextureProviderProperty("TriggerCharacterMountPlacesSwap", _isCharacterMountSwapped);
		}

		protected override void OnUpdate(float dt)
		{
			// Exit early if no char is defined, to prevent crash
			if (_charStringId == null) return;

			base.OnUpdate(dt);
			if ((LeftHandWieldedEquipmentIndex != -1 || RightHandWieldedEquipmentIndex != -1) && !IsRecursivelyVisible())
			{
				LeftHandWieldedEquipmentIndex = -1;
				RightHandWieldedEquipmentIndex = -1;
			}

			if (IsPlayingCustomAnimations && TextureProvider != null && !(bool)GetTextureProviderProperty("IsPlayingCustomAnimations"))
			{
				IsPlayingCustomAnimations = false;
			}

			if (TextureProvider != null)
			{
				CustomAnimationProgressRatio = (float)GetTextureProviderProperty("CustomAnimationProgressRatio");
			}
		}

		protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
		{
			_isRenderRequestedPreviousFrame = true;
			if (TextureProvider != null)
			{
				Texture = TextureProvider.GetTexture(twoDimensionContext, string.Empty);
				SimpleMaterial simpleMaterial = drawContext.CreateSimpleMaterial();
				StyleLayer styleLayer = ReadOnlyBrush?.GetStyleOrDefault(CurrentState).GetLayers()?.FirstOrDefault() ?? null;
				simpleMaterial.OverlayEnabled = false;
				simpleMaterial.CircularMaskingEnabled = false;
				simpleMaterial.Texture = Texture;
				simpleMaterial.AlphaFactor = (styleLayer?.AlphaFactor ?? 1f) * ReadOnlyBrush.GlobalAlphaFactor * Context.ContextAlpha;
				simpleMaterial.ColorFactor = (styleLayer?.ColorFactor ?? 1f) * ReadOnlyBrush.GlobalColorFactor;
				simpleMaterial.HueFactor = styleLayer?.HueFactor ?? 0f;
				simpleMaterial.SaturationFactor = styleLayer?.SaturationFactor ?? 0f;
				simpleMaterial.ValueFactor = styleLayer?.ValueFactor ?? 0f;
				simpleMaterial.Color = (styleLayer?.Color ?? TaleWorlds.Library.Color.White) * ReadOnlyBrush.GlobalColor;
				Vector2 globalPosition = GlobalPosition;
				float x = globalPosition.X;
				float y = globalPosition.Y;
				_ = Size;
				_ = Size;
				DrawObject2D drawObject2D = null;
				if (_cachedQuad != null && _cachedQuadSize == Size)
				{
					drawObject2D = _cachedQuad;
				}

				if (drawObject2D == null)
				{
					drawObject2D = (_cachedQuad = DrawObject2D.CreateQuad(Size));
					_cachedQuadSize = Size;
				}

				if (drawContext.CircularMaskEnabled)
				{
					simpleMaterial.CircularMaskingEnabled = true;
					simpleMaterial.CircularMaskingCenter = drawContext.CircularMaskCenter;
					simpleMaterial.CircularMaskingRadius = drawContext.CircularMaskRadius;
					simpleMaterial.CircularMaskingSmoothingRadius = drawContext.CircularMaskSmoothingRadius;
				}

				drawContext.Draw(x, y, simpleMaterial, drawObject2D, Size.X, Size.Y);
			}
		}
	}
}
#endif