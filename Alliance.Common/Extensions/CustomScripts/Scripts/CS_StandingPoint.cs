using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using System;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
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

			//if (GameNetwork.IsServerOrRecorder)
			//{
			//    UserAgent?.SetInitialFrame(GameEntity.GetGlobalFrame().Advance(0.4f).origin, GameEntity.GetGlobalFrame().rotation.f.AsVec2);
			//    MatrixFrame leftHand = GameEntity.GetGlobalFrame().Advance(2f).Elevate(-0.7f).Strafe(-0.2f);
			//    leftHand.Rotate(-MathHelper.ToRadian(120), GameEntity.GetGlobalFrame().rotation.f);
			//    leftHand.Rotate(MathHelper.ToRadian(60), GameEntity.GetGlobalFrame().rotation.u);
			//    MatrixFrame rightHand = GameEntity.GetGlobalFrame().Advance(2f).Elevate(-0.7f).Strafe(0.2f);
			//    rightHand.Rotate(-MathHelper.ToRadian(90), GameEntity.GetGlobalFrame().rotation.f);
			//    rightHand.Rotate(MathHelper.ToRadian(60), GameEntity.GetGlobalFrame().rotation.u);
			//    UserAgent?.SetHandInverseKinematicsFrame(ref leftHand, ref rightHand);
			//}
		}

		public override void OnUse(Agent userAgent)
		{
			if (!_init) Init();

			if (GameNetwork.IsServerOrRecorder)
			{
				// Make agent invulnerable
				userAgent.SetMortalityState(MortalityState.Invulnerable);
				// Teleport agent to exact position
				userAgent.TeleportToPosition(GameEntity.GlobalPosition);

				// Play animation
				if (Animation != null) AnimationSystem.Instance.PlayAnimation(userAgent, Animation, true);
			}

			OnUseEvent?.Invoke(userAgent);

			base.OnUse(userAgent);
		}

		public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
		{
			base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);

			// Teleport agent to exact position
			if (GameNetwork.IsServerOrRecorder)
			{
				// Free hands
				UserAgent?.ClearHandInverseKinematics();
				// Play animation
				AnimationSystem.Instance.PlayAnimation(userAgent, AnimationOnStop, true);
				// Make agent mortal
				userAgent.SetMortalityState(MortalityState.Mortal);
				// Teleport agent to exact position
				userAgent.TeleportToPosition(GameEntity.GlobalPosition);
			}

			OnUseStoppedEvent?.Invoke(userAgent);
		}
	}
}
