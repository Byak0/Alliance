using Alliance.Common.Extensions.FlagsTracker.Scripts;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.FlagMarker.Targets;

namespace Alliance.Client.Extensions.FlagsTracker.ViewModels
{
	public class CaptureZoneVM : MissionMarkerTargetVM
	{
		public CS_CapturableZone TargetFlag { get; private set; }

		protected override float HeightOffset => 3f;

		private float _flagProgress;

		private bool _isVisible;

		public override Vec3 WorldPosition
		{
			get
			{
				if (TargetFlag != null)
				{
					return TargetFlag.Position;
				}

				Debug.FailedAssert("No target found!", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Multiplayer\\FlagMarker\\Targets\\MissionFlagMarkerTargetVM.cs", "WorldPosition", 24);
				return Vec3.One;
			}
		}


		[DataSourceProperty]
		public float FlagProgress
		{
			get
			{
				return _flagProgress;
			}
			set
			{
				if (value != _flagProgress)
				{
					_flagProgress = value;
					OnPropertyChangedWithValue(value, "FlagProgress");
				}
			}
		}

		[DataSourceProperty]
		public bool IsVisible
		{
			get
			{
				return _isVisible;
			}
			set
			{
				if (value != _isVisible)
				{
					_isVisible = value;
					OnPropertyChangedWithValue(value, "IsVisible");
				}
			}
		}

		public CaptureZoneVM(CS_CapturableZone capturableZone) : base(MissionMarkerType.Flag)
		{
			TargetFlag = capturableZone;
			Name = capturableZone.ZoneName;

			OnOwnerChanged(null);
		}

		public override void UpdateScreenPosition(Camera missionCamera)
		{
			Vec3 worldPoint = WorldPosition;
			// HeightOffset for better display
			worldPoint.z += HeightOffset;
			// We retrieve the proportional position of point relative to screen
			Vec3 vector = missionCamera.WorldPointToViewPortPoint(ref worldPoint);
			// Check if position is behind us
			if (vector.z < 0f)
			{
				// Revert X
				vector.x = 1f - vector.x;
				// Force display on the bottom
				vector.y = 0f;
			}
			// Project the proportional position to actual screen coordinates
			vector.x = MathF.Clamp(vector.x, 0f, 1f) * Screen.RealScreenResolutionWidth;
			vector.y = MathF.Clamp(1 - vector.y, 0f, 1f) * Screen.RealScreenResolutionHeight;

			ScreenPosition = vector.AsVec2;
			Distance = (int)(TargetFlag.Position - missionCamera.Position).Length;
			FlagProgress = TargetFlag.GetFlagProgress();
		}

		public void OnOwnerChanged(Team team)
		{
			bool num = team == null || team.TeamIndex == -1;
			uint color = num ? 4284111450u : team.Color;
			uint color2 = num ? uint.MaxValue : team.Color2;
			RefreshColor(color, color2);
		}
	}
}
