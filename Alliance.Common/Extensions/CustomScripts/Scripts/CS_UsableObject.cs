using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
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
        public int NumberOfUseMax = -1;

        public string TextWhenUsable = "Use object";
        public string DefaultText = "Usable object";

        public string Action1 = "act_pickup_down_begin";
        public string Action2 = "act_pickup_down_end";
        public string Action3 = "act_eating";

        public float AnimationMaxDuration = 3f;

        public string SoundEffectOnUse = "";

        protected ActionIndexCache[] _indexActions;
        protected ActionIndexValueCache[] _indexActionsValueCache;
        protected float[] _actionDurations;
        protected int _lastAction = -1;

        protected Dictionary<StandingPoint, AnimState[]> standingPointsState;
        protected Dictionary<StandingPoint, float[]> stdPointsAnimDuration;
        protected Dictionary<ActionIndexCache, MBActionSet> actionSetForAction;

        protected bool _isVisible = true;
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
                        GameNetwork.WriteMessage(new SyncNumberOfUse(this, value));
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
            _isVisible = GameEntity.IsVisibleIncludeParents();

            // Initialize Actions
            _indexActions = new ActionIndexCache[3];
            _indexActions[0] = ActionIndexCache.Create(Action1);
            _indexActions[1] = ActionIndexCache.Create(Action2);
            _indexActions[2] = ActionIndexCache.Create(Action3);
            _indexActionsValueCache = new ActionIndexValueCache[3];
            for (int i = 0; i < _indexActions.Length; i++)
            {
                _indexActionsValueCache[i] = ActionIndexValueCache.Create(_indexActions[i]);
            }

            if (_indexActions[0].Index != -1) _lastAction = 0;
            if (_indexActions[1].Index != -1) _lastAction = 1;
            if (_indexActions[2].Index != -1) _lastAction = 2;

            // Find the correct ActionSet for each Action
            actionSetForAction = new Dictionary<ActionIndexCache, MBActionSet>();
            MBActionSet actionSet;
            int actionSetCount = MBActionSet.GetNumberOfActionSets();
            for (int i = 0; i < actionSetCount; i++)
            {
                actionSet = MBActionSet.GetActionSetWithIndex(i);
                for (int j = 0; j < _indexActions.Length; j++)
                {
                    if (MBActionSet.CheckActionAnimationClipExists(actionSet, _indexActions[j]))
                    {
                        if (!actionSetForAction.ContainsKey(_indexActions[j])) actionSetForAction.Add(_indexActions[j], actionSet);
                    }
                }
            }

            // Find the actions durations with the correct ActionSets
            _actionDurations = new float[3];
            for (int act = 0; act <= _lastAction; act++)
            {
                _actionDurations[act] = MBActionSet.GetActionAnimationDuration(actionSetForAction[_indexActions[act]], _indexActions[act]);
                MBAnimation.PrefetchAnimationClip(actionSetForAction[_indexActions[act]], _indexActions[act]);
            }
        }

        protected override void OnMissionReset()
        {
            base.OnMissionReset();
            NumberOfUse = 0;
        }

        public override void AfterMissionStart()
        {
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

        /* 
         * Issues remaining :
         * - act_dungeon_prisoner_sleep_wakeup goes full beyblade
         * -// deaths animations (and possibly others) gets you stuck on the ground / seems ok
         * -// act_stand_up_floor_3 instantly stands up / seems ok
         */
        private void TickAux(bool isParallel, float dt)
        {
            if (_isVisible && !GameNetwork.IsClientOrReplay)
            {
                foreach (StandingPoint standingPoint in StandingPoints)
                {
                    if (standingPoint.HasUser)
                    {
                        Agent userAgent = standingPoint.UserAgent;
                        ActionIndexCache currentAction = userAgent.GetCurrentAction(0);
                        float currentActionProgress = userAgent.GetCurrentActionProgress(0);
                        ActionIndexCache userAction = userAgent.GetCurrentAction(1);

                        // If there is no action to launch OR player is doing another action OR last action is finished, stop using object
                        //if (_lastAction == -1 || userAction != ActionIndexCache.act_none || (_lastAction != -1 && currentAction == _indexActions[_lastAction] && standingPointsState[standingPoint][_lastAction] == AnimState.Finished))
                        if (_lastAction == -1 || userAction != ActionIndexCache.act_none || _lastAction != -1 && standingPointsState[standingPoint][_lastAction] == AnimState.Finished)
                        {
                            // Debug client
                            //if (userAction != ActionIndexCache.act_none) Log("Calling FinishUsingObject because userAction was " + userAction.Name + ". Last action was " + currentAction.Name + " at " + currentActionProgress * 100 + "% progress with ActionSet " + userAgent.ActionSet.GetName() + " as finished", Colors.Magenta));
                            //else Log("Calling FinishUsingObject. Last action was " + currentAction.Name + " at " + currentActionProgress * 100 + "% progress with ActionSet " + userAgent.ActionSet.GetName() + " as finished", Colors.Magenta));
                            //for (int i = 0; i <= _lastAction; i++) Log("standingPointsState[standingPoint][" + i + "]" + standingPointsState[standingPoint][i].ToString(), Colors.Magenta));
                            //Log("---------------------------------------------", Colors.Magenta));
                            // Debug server
                            //if (userAction != ActionIndexCache.act_none) Log("Calling FinishUsingObject because userAction was " + userAction.Name + ". Last action was " + currentAction.Name + " at " + currentActionProgress * 100 + "% progress with ActionSet " + userAgent.ActionSet.GetName() + " as finished", 0, Debug.DebugColor.White);
                            //else Log("Calling FinishUsingObject. Last action was " + currentAction.Name + " at " + currentActionProgress * 100 + "% progress with ActionSet " + userAgent.ActionSet.GetName() + " as finished", 0, Debug.DebugColor.White);
                            //for (int i = 0; i <= _lastAction; i++) Log("standingPointsState[standingPoint][" + i + "]" + standingPointsState[standingPoint][i].ToString(), 0, Debug.DebugColor.White);
                            //Log("---------------------------------------------", 0, Debug.DebugColor.White);

                            // Reset actions states for next use
                            // We force a unique call to AfterUse in the main thread otherwise there could be concurrency issues in userAgent.StopUsingGameObject
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
                                // If current action differs from act AND act has not started
                                if (/*currentAction != _indexActions[act] && */standingPointsState[standingPoint][act] == AnimState.NotStarted)
                                {
                                    //Debug client
                                    //if (act == 0 && standingPointsState[standingPoint][0] == AnimState.NotStarted) for (int i = 0; i <= _lastAction; i++) Log(i + "-action[standingPoint][" + i + "]" + standingPointsState[standingPoint][i].ToString(), Colors.Magenta));
                                    //Debug server
                                    //if (act == 0 && standingPointsState[standingPoint][0] == AnimState.NotStarted) for (int i = 0; i <= _lastAction; i++) Log(i + "-action[standingPoint][" + i + "]" + standingPointsState[standingPoint][i].ToString(), 0, Debug.DebugColor.White);
                                    // If act is the first action OR previous action is finished, proceed
                                    if (act == 0 || standingPointsState[standingPoint][act - 1] == AnimState.Finished)
                                    {
                                        if (act == 0 && SoundEffectOnUse != "")
                                        {
                                            SyncSound(userAgent);
                                        }
                                        //string startOrSkipped = "Skipped";
                                        // If act is automatically launched by the previous one, skip SetActionChannel call
                                        //if (act != 0) Log(MBActionSet.GetActionAnimationContinueToAction(userAgent.ActionSet, _indexActions[act - 1]).Name, 0, Debug.DebugColor.White);
                                        if (act == 0 || MBActionSet.GetActionAnimationContinueToAction(userAgent.ActionSet, _indexActionsValueCache[act - 1]) != _indexActions[act])
                                        {
                                            // If act requires a different ActionSet, apply the correct one to userAgent
                                            if (!actionSetForAction[_indexActions[act]].Equals(userAgent.ActionSet))
                                            {
                                                AnimationSystemData animationSystemData = userAgent.Monster.FillAnimationSystemData(actionSetForAction[_indexActions[act]], userAgent.Character.GetStepSize(), false);
                                                userAgent.SetActionSet(ref animationSystemData);
                                            }
                                            SyncSetActionChannel(userAgent, act);
                                            //userAgent.SetActionChannel(0, _indexActions[act], false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
                                            //startOrSkipped = "Started";
                                        }
                                        standingPointsState[standingPoint][act] = AnimState.Playing;
                                        stdPointsAnimDuration[standingPoint][act] = 0;
                                        //Debug client
                                        //Log(act + "-" + startOrSkipped + " action " + _indexActions[act].Name + " with ActionSet " + userAgent.ActionSet.GetName(), Colors.Magenta));
                                        //Debug server
                                        //Log(act + "-" + startOrSkipped + " action " + _indexActions[act].Name + " with ActionSet " + userAgent.ActionSet.GetName(), 0, Debug.DebugColor.White);
                                    }
                                }
                                // If current action has automatically ended
                                // OR has exceeded AnimationMaxDuration
                                // OR is stuck at 100% progress, mark it as finished
                                else if (standingPointsState[standingPoint][act] == AnimState.Playing)
                                {
                                    stdPointsAnimDuration[standingPoint][act] += dt;
                                    if (_actionDurations[act] > AnimationMaxDuration && currentActionProgress >= AnimationMaxDuration / _actionDurations[act]
                                        || currentActionProgress == 1
                                        || stdPointsAnimDuration[standingPoint][act] > AnimationMaxDuration
                                        || stdPointsAnimDuration[standingPoint][act] > _actionDurations[act])
                                    {
                                        // Debug client
                                        //string causeStr = "Unknown";
                                        //if (_actionDurations[act] > AnimationMaxDuration && currentActionProgress >= AnimationMaxDuration / _actionDurations[act]) causeStr = "Exceeded progress";
                                        //if (currentActionProgress == 1 && standingPointsState[standingPoint][act] == AnimState.Playing) causeStr = "Progress at 100%";
                                        //if (stdPointsAnimDuration[standingPoint][act] > AnimationMaxDuration && standingPointsState[standingPoint][act] == AnimState.Playing) causeStr = "Exceeded TimeLimit";
                                        //Log("_actionDurations[" + act + "] | AnimationMaxDuration : " + _actionDurations[act] + " | " + AnimationMaxDuration));
                                        //Log(act + "- Marking action " + _indexActions[act].Name + " at " + currentActionProgress * 100 + "% progress with ActionSet " + userAgent.ActionSet.GetName() + " as finished cause " + causeStr, Colors.Magenta));
                                        // Debug server
                                        //Log("_actionDurations[" + act + "] | AnimationMaxDuration : " + _actionDurations[act] + " | " + AnimationMaxDuration, 0, Debug.DebugColor.White);
                                        //Log(act + "- Marking action " + _indexActions[act].Name + " at " + currentActionProgress * 100 + "% progress with ActionSet " + userAgent.ActionSet.GetName() + " as finished cause " + causeStr, 0, Debug.DebugColor.White);
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
                SoundEvent eventRef = SoundEvent.CreateEvent(SoundEvent.GetEventIdFromString(SoundEffectOnUse), Scene);
                eventRef.SetPosition(GameEntity.GetGlobalFrame().origin);
                eventRef.Play();
                DelayedStop(eventRef);
            }
            else if (GameNetwork.IsServerOrRecorder)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncSoundObject(this, userAgent));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
            }
        }

        // Delayed stop to prevent ambient sounds from looping
        private async void DelayedStop(SoundEvent eventRef)
        {
            await Task.Delay(11000);
            eventRef.Stop();
        }

        public void SyncSetActionChannel(Agent userAgent, int act)
        {
            userAgent.SetActionChannel(0, _indexActions[act], true, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
            if (GameNetwork.IsServerOrRecorder)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncSetActionChannel(this, userAgent, act));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
            }
        }

        protected virtual void AfterUse(Agent userAgent, bool actionCompleted = true)
        {
            if (NumberOfUseMax > 0)
            {
                NumberOfUse++;

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

        /*protected virtual void UpdateAmmoMesh()
        {
            int num = NumberOfUseMax - _numberOfUse;
            if (GameEntity != null)
            {
                for (int i = 0; i < GameEntity.MultiMeshComponentCount; i++)
                {
                    MetaMesh metaMesh = GameEntity.GetMetaMesh(i);
                    for (int j = 0; j < metaMesh.MeshCount; j++)
                    {
                        metaMesh.GetMeshAtIndex(j).SetVectorArgument(0f, (float)num, 0f, 0f);
                    }
                }
            }
        }*/

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
