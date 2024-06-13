using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
	/// <summary>
	/// This script can store multiple states of an object and switch between them by calling SetState.
	/// Final state can also enable / disable 2 different NavMesh to allow or block AI from using them.
	/// NavigationMeshIdEnabledOnFinalState is disabled at first, and enabled on final state (e.g. for NavMesh under the wall).
	/// NavigationMeshIdDisabledOnFinalState is enabled at first, and disabled on final state (e.g. for NavMesh ON the wall).
	/// </summary>
	public class CS_StateObject : SynchedMissionObject, IFocusable
	{
		public string States;
		public string SoundEffectOnFinalState = "";
		public BattleSideEnum BattleSide = BattleSideEnum.None;
		public int NavigationMeshIdEnabledOnFinalState = -1;
		public int NavigationMeshIdDisabledOnFinalState = -1;
		public int LastStateDelay = 0;

		private GameEntity[] _states;
		private GameEntity _previousState;
		private GameEntity _originalState;
		private string[] _statesNames;

		public FocusableObjectType FocusableObjectType => FocusableObjectType.None;
		public GameEntity CurrentState { get; private set; }
		public int CurrentStateIndex { get; private set; }

		private bool HasState => _statesNames != null && !_statesNames.IsEmpty();

		public event Action OnNextState;

		static CS_StateObject()
		{
		}

		protected override void OnInit()
		{
			base.OnInit();

			if (!string.IsNullOrEmpty(States))
			{
				_statesNames = States.Replace(" ", string.Empty).Split(new char[] { ',' });
				bool hasPhysic = false;
				string[] states = _statesNames;
				_states = new GameEntity[states.Length];
				for (int i = 0; i < states.Length; i++)
				{
					string item = states[i];
					if (!string.IsNullOrEmpty(item))
					{
						GameEntity gameEntity = GameEntity.GetChildren().FirstOrDefault((x) => x.Name == item);
						if (gameEntity != null)
						{
							_states[i] = gameEntity;
							//gameEntity.AddBodyFlags(BodyFlags.Moveable, true);
							PhysicsShape bodyShape = gameEntity.GetBodyShape();
							if (bodyShape != null)
							{
								PhysicsShape.AddPreloadQueueWithName(bodyShape.GetName(), gameEntity.GetGlobalScale());
								hasPhysic = true;
							}
						}
					}
				}

				if (hasPhysic)
				{
					PhysicsShape.ProcessPreloadQueue();
				}
			}

			_originalState = GetOriginalState();
			CurrentState = _originalState;
			//_originalState.AddBodyFlags(BodyFlags.Moveable, true);

			List<GameEntity> originalChildren = new List<GameEntity>();
			GameEntity.GetChildrenRecursive(ref originalChildren);
			IEnumerable<GameEntity> dynamicOriginalChildren = originalChildren.Where((child) => child.BodyFlag.HasAnyFlag(BodyFlags.Dynamic));
			foreach (GameEntity gameEntity4 in dynamicOriginalChildren)
			{
				gameEntity4.SetPhysicsState(false, true);
				gameEntity4.SetFrameChanged();
			}
			GameEntity.SetAnimationSoundActivation(true);

			// At start, disable NavigationMeshIdEnabledOnFinalState
			SetAbilityOfNavmesh(false, true);
		}

		public GameEntity GetOriginalState()
		{
			if (_states.ElementAtOrDefault(0) != null)
			{
				return _states[0];
			}
			else
			{
				return GameEntity;
			}
		}

		protected override void OnEditorInit()
		{
			base.OnEditorInit();

			//_referenceEntity = string.IsNullOrEmpty(ReferenceEntityTag) ? GameEntity : GameEntity.GetChildren().FirstOrDefault((x) => x.HasTag(ReferenceEntityTag));
		}

		protected override void OnEditorVariableChanged(string variableName)
		{
			base.OnEditorVariableChanged(variableName);
			//if (variableName.Equals(ReferenceEntityTag))
			//{
			//	_referenceEntity = string.IsNullOrEmpty(ReferenceEntityTag) ? GameEntity : GameEntity.GetChildren().SingleOrDefault((x) => x.HasTag(ReferenceEntityTag));
			//}
		}

		protected override void OnMissionReset()
		{
			base.OnMissionReset();
			Reset();
		}

		public void Reset()
		{
			RestoreEntity();
			CurrentStateIndex = 0;
		}

		private void RestoreEntity()
		{
			if (_states != null)
			{
				int j;
				int i;
				for (i = 0; i < _states.Count(); i = j + 1)
				{
					GameEntity gameEntity = GameEntity.GetChildren().FirstOrDefault((x) => x.Name == _states[i].ToString());
					if (gameEntity != null)
					{
						Skeleton skeleton = gameEntity.Skeleton;
						if (skeleton != null)
						{
							skeleton.SetAnimationAtChannel(-1, 0, 1f, -1f, 0f);
						}
					}
					j = i;
				}
			}
			if (CurrentState != _originalState)
			{
				CurrentState.SetVisibilityExcludeParents(false);
				CurrentState = _originalState;
			}
			CurrentState.SetVisibilityExcludeParents(true);
			CurrentState.SetPhysicsState(true, true);
			CurrentState.SetFrameChanged();

			// Restore Navmesh
			SetAbilityOfNavmesh(false, true);
		}

		protected override void OnEditorTick(float dt)
		{
			base.OnEditorTick(dt);
		}

		public void SetState(int stateIndex)
		{
			if (stateIndex >= 0 && stateIndex < _states.Count() && CurrentStateIndex != stateIndex)
			{
				if (stateIndex == _states.Count() - 1 && GameNetwork.IsServer)
				{
					DelayedSetState(stateIndex, LastStateDelay);
				}
				else
				{
					ApplyState(stateIndex);
				}
			}
		}

		private async void DelayedSetState(int stateIndex, int delay)
		{
			await Task.Delay(delay);
			ApplyState(stateIndex);
		}

		private void ApplyState(int stateIndex)
		{
			CurrentStateIndex = stateIndex;

			// Hide previous state
			if (CurrentState != null)
			{
				_previousState = CurrentState;
				foreach (GameEntity gameEntity in from child in CurrentState.GetChildren()
												  where child.BodyFlag.HasAnyFlag(BodyFlags.Dynamic)
												  select child)
				{
					gameEntity.SetPhysicsState(false, true);
					gameEntity.SetFrameChanged();
				}
				CurrentState.SetVisibilityExcludeParents(false);
			}

			// Show new state
			CurrentState = _states[stateIndex];
			CurrentState.SetVisibilityExcludeParents(true);
			foreach (GameEntity gameEntity in from child in CurrentState.GetChildren()
											  where child.BodyFlag.HasAnyFlag(BodyFlags.Dynamic)
											  select child)
			{
				gameEntity.SetPhysicsState(true, true);
				gameEntity.SetFrameChanged();
			}

			// Final state actions
			if (stateIndex == _states.Count() - 1)
			{
				SetAbilityOfNavmesh(true, false);
				SyncSound();
			}

			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncObjectState(Id, stateIndex));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);
			}
		}

		public void SyncSound()
		{
			if (string.IsNullOrEmpty(SoundEffectOnFinalState)) return;

			if (GameNetwork.IsClient)
			{
				AudioPlayer.Instance.Play(AudioPlayer.Instance.GetAudioId(SoundEffectOnFinalState), 1f, false, 10000, GameEntity.GetGlobalFrame().origin);
			}
			else if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncSoundDestructible(Id));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
		}

		public void SetAbilityOfNavmesh(bool Navmesh1, bool Navmesh2)
		{
			Log($"Disabling navmesh {NavigationMeshIdEnabledOnFinalState}, enabling navmesh {NavigationMeshIdDisabledOnFinalState}", LogLevel.Debug);

			if (NavigationMeshIdEnabledOnFinalState != -1)
			{
				Scene.SetAbilityOfFacesWithId(NavigationMeshIdEnabledOnFinalState, Navmesh1);
			}
			if (NavigationMeshIdDisabledOnFinalState != -1)
			{
				Scene.SetAbilityOfFacesWithId(NavigationMeshIdDisabledOnFinalState, Navmesh2);
			}

			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncAbilityOfNavmesh(Id, Navmesh1, Navmesh2));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
		}

		public override void WriteToNetwork()
		{
			base.WriteToNetwork();
			GameNetworkMessage.WriteIntToPacket(CurrentStateIndex, CompressionMission.UsableGameObjectDestructionStateCompressionInfo);
		}

		public override void OnAfterReadFromNetwork(ValueTuple<BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord> synchedMissionObjectReadableRecord)
		{
			base.OnAfterReadFromNetwork(synchedMissionObjectReadableRecord);
			CS_StateObjectRecord stateObjRecord = (CS_StateObjectRecord)synchedMissionObjectReadableRecord.Item2;
			if (stateObjRecord.State != 0)
			{
				SetState(stateObjRecord.State);
			}
		}

		[DefineSynchedMissionObjectTypeForMod(typeof(CS_StateObject))]
		public struct CS_StateObjectRecord : ISynchedMissionObjectReadableRecord
		{
			public int State { get; private set; }

			public CS_StateObjectRecord(int state)
			{
				State = state;
			}

			public bool ReadFromNetwork(ref bool bufferReadValid)
			{
				State = GameNetworkMessage.ReadIntFromPacket(CompressionMission.UsableGameObjectDestructionStateCompressionInfo, ref bufferReadValid);
				return bufferReadValid;
			}
		}

		public override void AddStuckMissile(GameEntity missileEntity)
		{
			if (CurrentState != null)
			{
				CurrentState.AddChild(missileEntity, false);
				return;
			}
			GameEntity.AddChild(missileEntity, false);
		}

		protected override bool OnCheckForProblems()
		{
			bool result = base.OnCheckForProblems();

			string[] statesNames = States.Replace(" ", string.Empty).Split(',');
			int i;
			for (i = 0; i < statesNames.Count(); i++)
			{
				if (!string.IsNullOrEmpty(statesNames[i]) && !(GameEntity.GetChildren().FirstOrDefault((x) => x.Name == statesNames[i]) != null))
				{
					MBEditor.AddEntityWarning(GameEntity, "State '" + statesNames[i] + "' is not valid.");
					result = true;
				}
			}

			return result;
		}

		public void OnFocusGain(Agent userAgent)
		{
		}

		public void OnFocusLose(Agent userAgent)
		{
		}

		public TextObject GetInfoTextForBeingNotInteractable(Agent userAgent)
		{
			return new TextObject();
		}

		public string GetDescriptionText(GameEntity gameEntity = null)
		{
			int num;
			if (int.TryParse(gameEntity.Name.Split(new char[] { '_' }).Last(), out num))
			{
				string text = gameEntity.Name;
				text = text.Remove(text.Count() - num.ToString().Count());
				text += "x";
				TextObject textObject;
				if (GameTexts.TryGetText("str_destructible_component", out textObject, text))
				{
					return textObject.ToString();
				}
				return "";
			}
			else
			{
				TextObject textObject2;
				if (GameTexts.TryGetText("str_destructible_component", out textObject2, gameEntity.Name))
				{
					return textObject2.ToString();
				}
				return "";
			}
		}
	}
}
