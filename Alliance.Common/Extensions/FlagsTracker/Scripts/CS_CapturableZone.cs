using Alliance.Common.Extensions.FlagsTracker.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Extensions.FlagsTracker.Scripts
{
	/// <summary>
	/// Custom script for handling capturable zones. 
	/// Optionally tied to a GameEntity containing one flag support, two flags versions (one for each team), 
	/// and bottom/top boundaries in which the active flag will move up and down.
	/// </summary>
	public class CS_CapturableZone : SynchedMissionObject
	{
		public enum FlagDirection { Up, Down }

		// Editor parameters
		public float CaptureRadius = 4f;
		public float RadiusMultiplierWhenContested = 1.5f;
		public float TimeToCapture = 10f;
		public string ZoneId;
		public string ZoneName = "Capturable Zone";
		public BattleSideEnum OriginalOwner = BattleSideEnum.None;
		public bool Visible = true;

		public Vec3 Position { get; set; }
		public BattleSideEnum Owner { get; private set; }
		public BattleSideEnum CapturingTeam { get; private set; }
		public float CaptureProgress { get; private set; }
		public Action<CS_CapturableZone, Team> OnOwnerChange { get; set; }

		private SynchedMissionObject[] _flags = new SynchedMissionObject[2];
		private List<SynchedMissionObject> _flagDependentObjects;
		private SynchedMissionObject _flagHolder;
		private GameEntity _flagBottomBoundary;
		private GameEntity _flagTopBoundary;
		private int _attackerAgentsCount = 0;
		private int _defenderAgentsCount = 0;
		private int _majorTeamAdvantage;
		private BattleSideEnum _majorTeamOnFlag;
		private BattleSideEnum _minorTeamOnFlag;
		private float _captureDuration;
		private float _captureStartTimer;
		private float _startProgress;
		private float _endProgress;
		private bool _isProgressing;
		private float _delay;
		private const float _tolerance = 0.01f;

		public Team OwnerTeam
		{
			get
			{
				return Owner switch
				{
					BattleSideEnum.Defender => Mission.Current.DefenderTeam,
					BattleSideEnum.Attacker => Mission.Current.AttackerTeam,
					_ => null,
				};
			}
		}

		public CS_CapturableZone()
		{
		}

		public override TickRequirement GetTickRequirement()
		{
			return TickRequirement.Tick;
		}

		protected override void OnInit()
		{
			SetScriptComponentToTick(GetTickRequirement());

			Position = GameEntity.GlobalPosition;

			// Recover all the game entities
			_flagHolder = GameEntity?.CollectChildrenEntitiesWithTag("flag_holder")?.FirstOrDefault()?.GetScriptComponents<SynchedMissionObject>().FirstOrDefault();
			if (_flagHolder != null)
			{
				_flags[(int)BattleSideEnum.Defender] = _flagHolder.GameEntity.CollectChildrenEntitiesWithTag("flag_defender").SingleOrDefault().GetScriptComponents<SynchedMissionObject>().SingleOrDefault();
				_flags[(int)BattleSideEnum.Attacker] = _flagHolder.GameEntity.CollectChildrenEntitiesWithTag("flag_attacker").SingleOrDefault().GetScriptComponents<SynchedMissionObject>().SingleOrDefault();
				_flagBottomBoundary = GameEntity.GetChildren().FirstOrDefault((q) => q.HasTag("flag_raising_bottom"));
				_flagTopBoundary = GameEntity.GetChildren().FirstOrDefault((q) => q.HasTag("flag_raising_top"));
				_flagDependentObjects = new List<SynchedMissionObject>();
				foreach (GameEntity gameEntity in Mission.Current.Scene.FindEntitiesWithTag("depends_zone_" + ZoneId).ToList())
				{
					_flagDependentObjects.Add(gameEntity.GetScriptComponents<SynchedMissionObject>().SingleOrDefault());
				}
			}

			// Initialize state and owner
			SetOwner(OriginalOwner);
		}

		public void SetOwner(BattleSideEnum owner)
		{
			CaptureProgress = owner == BattleSideEnum.None ? 0f : 1f;
			Owner = owner;
			CapturingTeam = owner;
			if (_flagHolder != null)
			{
				if (owner == BattleSideEnum.None)
				{
					_flagHolder.GameEntity.SetGlobalFrame(_flagBottomBoundary.GetGlobalFrame());
					HideAllFlags();
				}
				else
				{
					_flagHolder.GameEntity.SetGlobalFrame(_flagTopBoundary.GetGlobalFrame());
					BattleSideEnum notOwner = owner == BattleSideEnum.Attacker ? BattleSideEnum.Defender : BattleSideEnum.Attacker;
					SetFlagVisibility(notOwner, false);
					SetFlagVisibility(owner, true);
				}
			}
			OnOwnerChange?.Invoke(this, OwnerTeam);
		}

		public void SetFlagVisibility(BattleSideEnum side, bool visible)
		{
			if (side == BattleSideEnum.Defender || side == BattleSideEnum.Attacker)
			{
				_flags[(int)side]?.SetVisibleSynched(visible);
			}
		}

		public void HideAllFlags()
		{
			SetFlagVisibility(BattleSideEnum.Defender, false);
			SetFlagVisibility(BattleSideEnum.Attacker, false);
		}

		public void DetermineMajorAndMinorTeams()
		{
			float radius = CaptureProgress > 0 && CaptureProgress < 1 ? CaptureRadius * RadiusMultiplierWhenContested : CaptureRadius;
			MBList<Agent> nearbyAgents = Mission.Current.GetNearbyAgents(Position.AsVec2, radius, new MBList<Agent>());
			_defenderAgentsCount = nearbyAgents.Where(agent => agent.Team.Side == BattleSideEnum.Defender).Count();
			_attackerAgentsCount = nearbyAgents.Where(agent => agent.Team.Side == BattleSideEnum.Attacker).Count();
			if (_defenderAgentsCount > _attackerAgentsCount)
			{
				_majorTeamAdvantage = _defenderAgentsCount - _attackerAgentsCount;
				_majorTeamOnFlag = BattleSideEnum.Defender;
				_minorTeamOnFlag = BattleSideEnum.Attacker;
			}
			else if (_attackerAgentsCount > _defenderAgentsCount)
			{
				_majorTeamAdvantage = _attackerAgentsCount - _defenderAgentsCount;
				_majorTeamOnFlag = BattleSideEnum.Attacker;
				_minorTeamOnFlag = BattleSideEnum.Defender;
			}
			else
			{
				_majorTeamAdvantage = 1;
				_majorTeamOnFlag = BattleSideEnum.None;
				_minorTeamOnFlag = BattleSideEnum.None;
			}
		}

		protected override void OnMissionReset()
		{
			SetOwner(OriginalOwner);
		}

		public void SetVisibleWithAllSynched(bool value, bool forceChildrenVisible = false)
		{
			SetVisibleSynched(value, forceChildrenVisible);
			foreach (SynchedMissionObject synchedMissionObject in _flagDependentObjects)
			{
				synchedMissionObject.SetVisibleSynched(value, false);
			}
		}

		public void SetTeamColorsWithAllSynched(uint color, uint color2)
		{
			foreach (SynchedMissionObject synchedMissionObject in _flagDependentObjects)
			{
				synchedMissionObject.SetTeamColorsSynched(color, color2);
			}
		}

		protected override void OnTick(float dt)
		{
			base.OnTick(dt);

			_delay += dt;
			if (_delay >= 0.2f)
			{
				UpdateCapturableZone();
			}

			if (_isProgressing)
			{
				_captureStartTimer += dt;

				// Calculate the interpolation factor
				float t = _captureStartTimer / _captureDuration;

				// Interpolate the progress value
				CaptureProgress = MathF.Lerp(_startProgress, _endProgress, t);

				// If the duration has passed, stop moving
				if (_captureStartTimer >= _captureDuration)
				{
					_isProgressing = false;
				}
			}

			// Update visuals
			float progress = _flagHolder != null ? GetFlagProgress() : CaptureProgress;
			if (progress > 0 && progress < 1)
			{
				DebugExtensions.RenderDebugCircleOnTerrain(Scene, new MatrixFrame(Mat3.Identity, Position), CaptureRadius * RadiusMultiplierWhenContested, 2868838400U, true, false);
				MBDebug.RenderText(50, 50, $"{ZoneName} contested", time: 10f);
			}
		}

		protected override void OnEditorTick(float dt)
		{
			base.OnEditorTick(dt);
			if (MBEditor.IsEntitySelected(GameEntity))
			{
				DebugExtensions.RenderDebugCircleOnTerrain(Scene, GameEntity.GetGlobalFrame(), CaptureRadius, 2852192000U, true, false);
				DebugExtensions.RenderDebugCircleOnTerrain(Scene, GameEntity.GetGlobalFrame(), CaptureRadius * RadiusMultiplierWhenContested, 2868838400U, true, false);
			}
		}

		public void UpdateCapturableZone()
		{
			// Calculate counts of attacker and defender agents within the zone
			DetermineMajorAndMinorTeams();

			bool enemiesOnFlag = Owner != _majorTeamOnFlag && _majorTeamOnFlag != BattleSideEnum.None;
			bool incompleteCapture = CaptureProgress < 1f && CapturingTeam != BattleSideEnum.None;
			bool enemiesOnCapturedFlag = Owner != BattleSideEnum.None && CapturingTeam != _majorTeamOnFlag && _majorTeamOnFlag != BattleSideEnum.None;
			bool emptyNeutralFlag = Owner == BattleSideEnum.None && _majorTeamOnFlag == BattleSideEnum.None;
			bool capturingTeamInMinority = _minorTeamOnFlag == CapturingTeam;

			// Check progress
			if (enemiesOnFlag || incompleteCapture)
			{
				if (enemiesOnCapturedFlag || emptyNeutralFlag || capturingTeamInMinority)
				{
					UpdateProgress(FlagDirection.Down, _majorTeamAdvantage);
					string losingControl = $"{CapturingTeam} is losing control of {ZoneName} due to {_majorTeamOnFlag} ({CaptureProgress * 100}%)";
					Log(losingControl, LogLevel.Information);
				}
				else
				{
					if (_majorTeamOnFlag != BattleSideEnum.None && CapturingTeam != _majorTeamOnFlag)
					{
						CapturingTeam = _majorTeamOnFlag;
						SetFlagVisibility(_majorTeamOnFlag, true);
						Log($"Showing flag of {CapturingTeam}", LogLevel.Debug);
					}
					UpdateProgress(FlagDirection.Up, _majorTeamAdvantage);
					string capturing = $"{CapturingTeam} is capturing {ZoneName} from {Owner} ({CaptureProgress * 100}%)";
					Log(capturing, LogLevel.Information);
				}
			}

			// Check owner changes
			if (CaptureProgress <= 0f && (Owner != BattleSideEnum.None || CapturingTeam != BattleSideEnum.None))
			{
				Owner = BattleSideEnum.None;
				CapturingTeam = BattleSideEnum.None;
				HideAllFlags();
				ServerSynchronize();
				string reset = $"{ZoneName} reset to None ({CaptureProgress * 100}%)";
				Log(reset, LogLevel.Debug);
			}
			else if (CaptureProgress >= 1f && Owner != CapturingTeam)
			{
				Owner = CapturingTeam;
				ServerSynchronize();
				string capture = $"{Owner} has captured {ZoneName} ({CaptureProgress * 100}%)";
				Log(capture, LogLevel.Information);
			}
		}

		public float GetFlagProgress()
		{
			if (_flagHolder == null) return CaptureProgress;

			float flagPositionZ = _flagHolder.GameEntity.GlobalPosition.z;
			float bottomBoundaryZ = _flagBottomBoundary.GlobalPosition.z;
			float topBoundaryZ = _flagTopBoundary.GlobalPosition.z;
			float progress = (flagPositionZ - bottomBoundaryZ) / (topBoundaryZ - bottomBoundaryZ);

			// If progress is close enough to 1, considering the tolerance, it's considered complete
			if (MathF.Abs(progress - 1f) <= _tolerance) progress = 1f;

			// If progress is close enough to 0, considering the tolerance, it's considered at the starting point
			if (MathF.Abs(progress) <= _tolerance) progress = 0f;

			return MathF.Clamp(progress, 0f, 1f);
		}

		public void UpdateProgress(FlagDirection direction, float progressSpeed)
		{
			float speedMultiplier = MathF.Clamp(progressSpeed, 0f, 4f);
			float directionFactor = direction == FlagDirection.Up ? 1f - CaptureProgress : CaptureProgress;
			float baseSpeed = TimeToCapture / speedMultiplier;
			_startProgress = CaptureProgress;
			_endProgress = direction == FlagDirection.Up ? 1f : 0f;
			_captureDuration = directionFactor * baseSpeed;
			_captureStartTimer = 0f;
			_isProgressing = true;

			if (_flagHolder != null)
			{
				SetMoveFlag(direction, _captureDuration);
			}
		}

		public void SetMoveFlag(FlagDirection direction, float duration)
		{
			// Target frame based on the direction
			MatrixFrame targetFrame;
			if (direction == FlagDirection.Up)
			{
				targetFrame = _flagTopBoundary.GetFrame();
			}
			else
			{
				targetFrame = _flagBottomBoundary.GetFrame();
			}

			_flagHolder.SetFrameSynchedOverTime(ref targetFrame, duration, false);
		}

		public virtual void ServerSynchronize()
		{
			if (GameNetwork.IsServer)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncCapturableZone(Id, Position, Owner));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}
	}
}
