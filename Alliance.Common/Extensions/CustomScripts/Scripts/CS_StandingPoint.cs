using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
	public class CS_StandingPoint : StandingPoint
	{
		public string AnimationName = "act_sit_down_on_floor_1";
		public string AnimationNameOnStop = "act_none";

		[EditableScriptComponentVariable(false)]
		public Action<Agent> OnUseEvent;
		[EditableScriptComponentVariable(false)]
		public Action<Agent> OnUseStoppedEvent;

		protected Animation Animation { get; set; }
		protected Animation AnimationOnStop { get; set; }
		private bool _init;

		public CS_StandingPoint() { }

		public void Init()
		{
			Animation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == AnimationName);
			AnimationOnStop = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_none");

			_init = true;
		}

		protected override void OnTick(float dt)
		{
			base.OnTick(dt);

			if (UserAgent != null)
			{
				MatrixFrame targetFrame = GameEntity.GetGlobalFrame();

				// Update agent position
				UserAgent.TeleportToPosition(targetFrame.origin);

				// Update agent visuals position precisely to avoid any stutter
				Mat3 cleanRotation = Mat3.Identity;
				cleanRotation = cleanRotation.TransformToLocal(targetFrame.rotation);
				cleanRotation.Orthonormalize();
				MatrixFrame visualFrame = new MatrixFrame(cleanRotation, targetFrame.origin);
				UserAgent.AgentVisuals.GetEntity().SetGlobalFrame(in visualFrame);
			}
		}

		public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
		{
			if (!_init) Init();
			
			LockUserFrames = false;	
			LockUserPositions = false;
			
			if (GameNetwork.IsClient && userAgent == Agent.Main)
			{
				userAgent.Controller = TaleWorlds.Core.AgentControllerType.None;
			}
			
			userAgent.SetExcludedFromGravity(true, false);
			
			if (GameNetwork.IsServerOrRecorder && Animation != null)
			{
				AnimationSystem.Instance.PlayAnimation(userAgent, Animation, true);
			}
			
			base.OnUse(userAgent, agentBoneIndex);
			OnUseEvent?.Invoke(userAgent);
		}

		public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
		{
			base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);

			userAgent.SetExcludedFromGravity(false, false);
			userAgent.ClearTargetFrame();

			// Reset frame & orientation
			Mat3 cleanRotation = Mat3.Identity;
			MatrixFrame visualFrame = new MatrixFrame(cleanRotation, userAgent.Position);
			userAgent.AgentVisuals.GetEntity().SetGlobalFrame(in visualFrame);
			userAgent.ClearHandInverseKinematics();

			if (GameNetwork.IsClient && userAgent == Agent.Main)
			{
				userAgent.Controller = TaleWorlds.Core.AgentControllerType.Player;
			}

			if (GameNetwork.IsServerOrRecorder)
			{
				AnimationSystem.Instance.PlayAnimation(userAgent, AnimationOnStop, true);
				userAgent.SetMortalityState(MortalityState.Mortal);
				userAgent.TeleportToPosition(GameEntity.GlobalPosition);
			}

			OnUseStoppedEvent?.Invoke(userAgent);
		}
	}
}
