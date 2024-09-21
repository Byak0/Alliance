using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
	/// <summary>
	/// This script is attached to an object and allows interaction with it.
	/// Up to 3 actions/animations can be set up and chained, and a sound can be played along.
	/// The script is built to handle most animations but the result is not guaranteed.
	/// Some animations may still cause issues. Use AnimationMaxDuration to prevent long or looping animations.     
	/// </summary>
	public class CS_UsableObject : UsableMachine
	{
		public event Action OnUse;

		public string ObjectId;

		public int NumberOfUseMax = -1;

		public string TextWhenUsable = "Use object";
		public string DefaultText = "Usable object";

		public string Action1 = "act_pickup_down_begin";
		public string Action2 = "act_pickup_down_end";
		public string Action3 = "act_eating";

		public float AnimationMaxDuration = 3f;

		public string SoundEffectOnUse = "";

		protected Animation[] _animations;
		protected float[] _actionDurations;
		protected int _lastAction = -1;

		protected Dictionary<StandingPoint, AnimState[]> standingPointsState;
		protected Dictionary<StandingPoint, float[]> stdPointsAnimDuration;

		protected bool _needsSingleThreadTickOnce;

		protected int _numberOfUse;

		[EditableScriptComponentVariable(false)]
		public int NumberOfUse
		{
			get
			{
				return _numberOfUse;
			}
			set
			{
				if (!_numberOfUse.Equals(value))
				{
					_numberOfUse = MathF.Max(value, 0);
					if (GameNetwork.IsServerOrRecorder)
					{
						GameNetwork.BeginBroadcastModuleEvent();
						GameNetwork.WriteMessage(new SyncNumberOfUse(Id, value));
						GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
					}
				}
			}
		}

		protected CS_UsableObject()
		{
		}

		protected override void OnInit()
		{
			base.OnInit();
			NumberOfUse = 0;
			SetScriptComponentToTick(GetTickRequirement());
			MakeVisibilityCheck = false;
		}

		protected override void OnMissionReset()
		{
			base.OnMissionReset();
			NumberOfUse = 0;
		}

		public override void AfterMissionStart()
		{
			_animations = new Animation[3];
			_animations[0] = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == Action1);
			_animations[1] = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == Action2);
			_animations[2] = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == Action3);

			_lastAction = -1;
			for (int i = 0; i < _animations.Length; i++)
			{
				if (_animations[i] != null)
				{
					_lastAction = i;
				}
			}

			_actionDurations = new float[3];
			for (int act = 0; act <= _lastAction; act++)
			{
				_actionDurations[act] = _animations[act]?.MaxDuration ?? AnimationMaxDuration;
			}

			standingPointsState = new Dictionary<StandingPoint, AnimState[]>();
			stdPointsAnimDuration = new Dictionary<StandingPoint, float[]>();
			if (StandingPoints != null)
			{
				PropertyInfo finfo = typeof(UsableMissionObject).GetProperty("LockUserFrames", BindingFlags.Instance | BindingFlags.NonPublic);
				foreach (StandingPoint standingPoint in StandingPoints)
				{
					finfo.SetValue(standingPoint, true);
					standingPointsState.Add(standingPoint, new AnimState[_lastAction == -1 ? 0 : _lastAction + 1]);
					stdPointsAnimDuration.Add(standingPoint, new float[_lastAction == -1 ? 0 : _lastAction + 1]);
				}
			}
		}

		public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
		{
			TextObject textObject = new TextObject("{KEY} {TEXT} {AMOUNT}", null);
			textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
			textObject.SetTextVariable("TEXT", TextWhenUsable);
			if (NumberOfUseMax > 0) textObject.SetTextVariable("AMOUNT", "[" + (NumberOfUseMax - _numberOfUse) + "/" + NumberOfUseMax + "]");
			return textObject;
		}

		public override string GetDescriptionText(GameEntity gameEntity = null)
		{
			return new TextObject("{TEXT}", null).SetTextVariable("TEXT", DefaultText).ToString();
		}

		public override TickRequirement GetTickRequirement()
		{
			if (GameNetwork.IsClientOrReplay)
			{
				return base.GetTickRequirement();
			}
			return TickRequirement.Tick | TickRequirement.TickParallel | base.GetTickRequirement();
		}

		protected override void OnTickParallel(float dt)
		{
			TickAux(true, dt);
		}

		protected override void OnTick(float dt)
		{
			base.OnTick(dt);
			if (_needsSingleThreadTickOnce)
			{
				_needsSingleThreadTickOnce = false;
				TickAux(false, dt);
			}
		}

		private void TickAux(bool isParallel, float dt)
		{
			if (!GameNetwork.IsClientOrReplay)
			{
				foreach (StandingPoint standingPoint in StandingPoints)
				{
					if (standingPoint.HasUser && standingPoint.UserAgent?.Health > 0)
					{
						Agent userAgent = standingPoint.UserAgent;
						float currentActionProgress = userAgent.GetCurrentActionProgress(0);

						if (_lastAction == -1 || userAgent.GetCurrentAction(1) != ActionIndexCache.act_none || (_lastAction != -1 && standingPointsState[standingPoint][_lastAction] == AnimState.Finished))
						{
							if (isParallel)
							{
								_needsSingleThreadTickOnce = true;
							}
							else
							{
								bool actionCompleted = _lastAction == -1 || standingPointsState[standingPoint][_lastAction] == AnimState.Finished;
								ResetStandingPoint(standingPoint);
								AfterUse(userAgent, actionCompleted);
							}
						}
						else
						{
							for (int act = 0; act <= _lastAction; act++)
							{
								if (standingPointsState[standingPoint][act] == AnimState.NotStarted)
								{
									if (act == 0 || standingPointsState[standingPoint][act - 1] == AnimState.Finished)
									{
										if (act == 0 && SoundEffectOnUse != "")
										{
											SyncSound(userAgent);
										}
										AnimationSystem.Instance.PlayAnimation(userAgent, _animations[act], true);
										standingPointsState[standingPoint][act] = AnimState.Playing;
										stdPointsAnimDuration[standingPoint][act] = 0;
									}
								}
								else if (standingPointsState[standingPoint][act] == AnimState.Playing)
								{
									stdPointsAnimDuration[standingPoint][act] += dt;
									if (_actionDurations[act] > AnimationMaxDuration && currentActionProgress >= AnimationMaxDuration / _actionDurations[act]
										|| currentActionProgress == 1
										|| stdPointsAnimDuration[standingPoint][act] > AnimationMaxDuration
										|| stdPointsAnimDuration[standingPoint][act] > _actionDurations[act])
									{
										standingPointsState[standingPoint][act] = AnimState.Finished;
									}
								}
							}
						}
					}
				}
			}
		}

		private void ResetStandingPoint(StandingPoint standingPoint)
		{
			for (int act = 0; act <= _lastAction; act++)
			{
				standingPointsState[standingPoint][act] = AnimState.NotStarted;
			}
		}

		public void SyncSound(Agent userAgent)
		{
			if (GameNetwork.IsClient)
			{
				AudioPlayer.Instance.Play(AudioPlayer.Instance.GetAudioId(SoundEffectOnUse), 1f, false, 10000, GameEntity.GetGlobalFrame().origin);
			}
			else if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncSoundObject(Id, userAgent.Index));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
		}

		protected virtual void AfterUse(Agent userAgent, bool actionCompleted = true)
		{
			NumberOfUse++;
			OnUse?.Invoke();

			if (NumberOfUseMax > 0)
			{
				if (_numberOfUse == NumberOfUseMax)
				{
					Deactivate();
				}
			}

			userAgent.SetActionChannel(0, ActionIndexCache.act_none, true, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			userAgent.StopUsingGameObject();
		}

		public override OrderType GetOrder(BattleSideEnum side)
		{
			return OrderType.None;
		}

		protected enum AnimState
		{
			NotStarted,
			Playing,
			Finished
		}

		static CS_UsableObject()
		{
		}
	}
}
