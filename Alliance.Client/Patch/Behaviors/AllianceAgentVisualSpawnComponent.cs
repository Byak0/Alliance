using Alliance.Common.Extensions.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Patch.Behaviors
{
    /// <summary>
    /// This class replaces the default MultiplayerMissionAgentVisualSpawnComponent.
    /// Calls to the original class are redirected to this one by the Harmony patch.
    /// Allows the preview of custom races and add some extra features.
    /// </summary>
    public class AllianceAgentVisualSpawnComponent : MissionNetwork
    {
        private List<string> _randomUrukCheers = new List<string>
            {
                "LOTR/Isengard/Voice/Uruk/AAAh.wav",
                "LOTR/Isengard/Voice/Uruk/Laugh 1.wav",
                "LOTR/Isengard/Voice/Uruk/Grunt.wav",
                "LOTR/Isengard/Voice/Uruk/Laugh 2.wav",
                "LOTR/Isengard/Voice/Uruk/Yell 1.wav",
                "LOTR/Isengard/Voice/Uruk/Laugh 3.wav",
                "LOTR/Isengard/Voice/Uruk/Yell 2.wav",
                "LOTR/Isengard/Voice/Uruk/Laugh 4.wav",
                "LOTR/Isengard/Voice/Uruk/Yell 3.wav",
                "LOTR/Isengard/Voice/Uruk/Meat is back on the menu.wav",
                "LOTR/Isengard/Voice/Uruk/Saruman.wav"
            };
        private int _lastUrukCheerIndex = -1;

        private VisualSpawnFrameSelectionHelper _spawnFrameSelectionHelper;

        public event Action OnMyAgentVisualSpawned;

        public event Action OnMyAgentSpawnedFromVisual;

        public event Action OnMyAgentVisualRemoved;

        public void SpawnAgentVisualsForPeer(MissionPeer missionPeer, AgentBuildData buildData, int selectedEquipmentSetIndex = -1, bool isBot = false, int totalTroopCount = 0)
        {
            GameNetwork.MyPeer?.GetComponent<MissionPeer>();
            if (buildData.AgentVisualsIndex == 0)
            {
                missionPeer.ClearAllVisuals();
            }

            missionPeer.ClearVisuals(buildData.AgentVisualsIndex);
            Equipment equipment = new Equipment(buildData.AgentOverridenSpawnEquipment);

            // Remove banner from equipment visuals to prevent weird appearance
            for (int i = 0; i < 5; i++)
            {
                EquipmentElement equipmentElement = equipment[i];
                if (!equipmentElement.IsEmpty && equipmentElement.Item.PrimaryWeapon.WeaponClass == WeaponClass.Banner)
                {
                    equipment[i] = new EquipmentElement(null);
                }
            }

            ItemObject item = equipment[10].Item;
            MatrixFrame frame = _spawnFrameSelectionHelper.GetSpawnPointFrameForPlayer(missionPeer.Peer, missionPeer.Team.Side, buildData.AgentVisualsIndex, totalTroopCount, item != null);
            ActionIndexCache actionIndexCache = item == null ? SpawningBehaviorBase.PoseActionInfantry : SpawningBehaviorBase.PoseActionCavalry;
            MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(buildData.AgentCharacter);
            MBReadOnlyList<MPPerkObject> selectedPerks = missionPeer.SelectedPerks;
            IAgentVisual agentVisual = null;
            if (item != null)
            {
                Monster monster = item.HorseComponent.Monster;
                AgentVisualsData agentVisualsData = new AgentVisualsData().Equipment(equipment).Scale(item.ScaleFactor).Frame(MatrixFrame.Identity)
                    .ActionSet(MBGlobals.GetActionSet(monster.ActionSetCode))
                    .Scene(Mission.Current.Scene)
                    .Monster(monster)
                    .PrepareImmediately(prepareImmediately: false)
                    .MountCreationKey(MountCreationKey.GetRandomMountKeyString(item, MBRandom.RandomInt()));
                agentVisual = Mission.Current.AgentVisualCreator.Create(agentVisualsData, "Agent " + buildData.AgentCharacter.StringId + " mount", needBatchedVersionForWeaponMeshes: true, forceUseFaceCache: false);
                MatrixFrame frame2 = frame;
                frame2.rotation.ApplyScaleLocal(agentVisualsData.ScaleData);
                ActionIndexCache actionIndexCache2 = ActionIndexCache.act_none;
                foreach (MPPerkObject item2 in selectedPerks)
                {
                    if (!isBot && item2.HeroMountIdleAnimOverride != null)
                    {
                        actionIndexCache2 = ActionIndexCache.Create(item2.HeroMountIdleAnimOverride);
                        break;
                    }

                    if (isBot && item2.TroopMountIdleAnimOverride != null)
                    {
                        actionIndexCache2 = ActionIndexCache.Create(item2.TroopMountIdleAnimOverride);
                        break;
                    }
                }

                if (actionIndexCache2 == ActionIndexCache.act_none)
                {
                    if (item.StringId == "mp_aserai_camel")
                    {
                        Debug.Print("Client is spawning a camel for without mountCustomAction from the perk.", 0, Debug.DebugColor.White, 17179869184uL);
                        actionIndexCache2 = isBot ? ActionIndexCache.Create("act_camel_idle_1") : ActionIndexCache.Create("act_hero_mount_idle_camel");
                    }
                    else
                    {
                        if (!isBot && !string.IsNullOrEmpty(mPHeroClassForCharacter.HeroMountIdleAnim))
                        {
                            actionIndexCache2 = ActionIndexCache.Create(mPHeroClassForCharacter.HeroMountIdleAnim);
                        }

                        if (isBot && !string.IsNullOrEmpty(mPHeroClassForCharacter.TroopMountIdleAnim))
                        {
                            actionIndexCache2 = ActionIndexCache.Create(mPHeroClassForCharacter.TroopMountIdleAnim);
                        }
                    }
                }

                if (actionIndexCache2 != ActionIndexCache.act_none)
                {
                    agentVisual.SetAction(actionIndexCache2);
                }

                agentVisual.GetVisuals().GetEntity().SetFrame(ref frame2);
            }

            ActionIndexCache actionIndexCache3 = actionIndexCache;
            if (agentVisual != null)
            {
                actionIndexCache3 = agentVisual.GetVisuals().GetSkeleton().GetActionAtChannel(0);
            }
            else
            {
                foreach (MPPerkObject item3 in selectedPerks)
                {
                    if (!isBot && item3.HeroIdleAnimOverride != null)
                    {
                        actionIndexCache3 = ActionIndexCache.Create(item3.HeroIdleAnimOverride);
                        break;
                    }

                    if (isBot && item3.TroopIdleAnimOverride != null)
                    {
                        actionIndexCache3 = ActionIndexCache.Create(item3.TroopIdleAnimOverride);
                        break;
                    }
                }

                if (actionIndexCache3 == actionIndexCache)
                {
                    if (!isBot && !string.IsNullOrEmpty(mPHeroClassForCharacter.HeroIdleAnim))
                    {
                        actionIndexCache3 = ActionIndexCache.Create(mPHeroClassForCharacter.HeroIdleAnim);
                    }

                    if (isBot && !string.IsNullOrEmpty(mPHeroClassForCharacter.TroopIdleAnim))
                    {
                        actionIndexCache3 = ActionIndexCache.Create(mPHeroClassForCharacter.TroopIdleAnim);
                    }
                }
            }

            Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(buildData.AgentCharacter.Race);
            IAgentVisual agentVisual2 = Mission.Current.AgentVisualCreator.Create(new AgentVisualsData().Equipment(equipment).BodyProperties(buildData.AgentBodyProperties).Frame(frame)
                .ActionSet(MBActionSet.GetActionSet(baseMonsterFromRace.ActionSetCode))
                .Scene(Mission.Current.Scene)
                .Monster(baseMonsterFromRace)
                // Add race to the agent visuals data to allow the preview of custom races
                .Race(buildData.AgentRace)
                .PrepareImmediately(prepareImmediately: false)
                .UseMorphAnims(useMorphAnims: true)
                .SkeletonType(buildData.AgentIsFemale ? SkeletonType.Female : SkeletonType.Male)
                .ClothColor1(buildData.AgentClothingColor1)
                .ClothColor2(buildData.AgentClothingColor2)
                .AddColorRandomness(buildData.AgentVisualsIndex != 0)
                .ActionCode(actionIndexCache3), "Mission::SpawnAgentVisuals", needBatchedVersionForWeaponMeshes: true, forceUseFaceCache: false);
            agentVisual2.SetAction(actionIndexCache3);
            agentVisual2.GetVisuals().SetFrame(ref frame);
            agentVisual2.SetCharacterObjectID(buildData.AgentCharacter.StringId);
            equipment.GetInitialWeaponIndicesToEquip(out var mainHandWeaponIndex, out var offHandWeaponIndex, out var isMainHandNotUsableWithOneHand);
            if (isMainHandNotUsableWithOneHand)
            {
                offHandWeaponIndex = EquipmentIndex.None;
            }

            agentVisual2.GetVisuals().SetWieldedWeaponIndices((int)mainHandWeaponIndex, (int)offHandWeaponIndex);
            PeerVisualsHolder peerVisualsHolder = new PeerVisualsHolder(missionPeer, buildData.AgentVisualsIndex, agentVisual2, agentVisual);
            missionPeer.OnVisualsSpawned(peerVisualsHolder, peerVisualsHolder.VisualsIndex);
            if (missionPeer.IsMine && buildData.AgentVisualsIndex == 0)
            {
                // Play random cheer for Uruk because why not
                if (baseMonsterFromRace.StringId == "uruk")
                {
                    _lastUrukCheerIndex = (_lastUrukCheerIndex + 1) % _randomUrukCheers.Count;
                    AudioPlayer.Instance.Play(_randomUrukCheers[_lastUrukCheerIndex], 0.5f, soundOrigin: frame.origin);
                }

                OnMyAgentVisualSpawned?.Invoke();

                // Also invoke the original events since some native components rely on them
                MultiplayerMissionAgentVisualSpawnComponent originalComponent = Mission.Current.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>();
                // Invoke OnMyAgentVisualSpawned using reflection 
                FieldInfo eventField = typeof(MultiplayerMissionAgentVisualSpawnComponent).GetField("OnMyAgentVisualSpawned", BindingFlags.Instance | BindingFlags.NonPublic);
                // Get the delegate attached to the field
                Delegate eventDelegate = (Delegate)eventField.GetValue(originalComponent);
                // Invoke the delegate if it's not null
                eventDelegate?.DynamicInvoke();
            }
        }

        public void RemoveAgentVisuals(MissionPeer missionPeer, bool sync = false)
        {
            missionPeer.ClearAllVisuals();
            if (!GameNetwork.IsDedicatedServer && !missionPeer.Peer.IsMine)
            {
                _spawnFrameSelectionHelper.FreeSpawnPointFromPlayer(missionPeer.Peer);
            }

            if (missionPeer.IsMine)
            {
                OnMyAgentVisualRemoved?.Invoke();

                // Also invoke the original events since some native components rely on them
                MultiplayerMissionAgentVisualSpawnComponent originalComponent = Mission.Current.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>();
                // Invoke OnMyAgentVisualRemoved using reflection 
                FieldInfo eventField = typeof(MultiplayerMissionAgentVisualSpawnComponent).GetField("OnMyAgentVisualRemoved", BindingFlags.Instance | BindingFlags.NonPublic);
                // Get the delegate attached to the field
                Delegate eventDelegate = (Delegate)eventField.GetValue(originalComponent);
                // Invoke the delegate if it's not null
                eventDelegate?.DynamicInvoke();
            }

            Debug.Print("Removed visuals for " + missionPeer.Name + ".", 0, Debug.DebugColor.White, 17179869184uL);
        }

        public void OnMyAgentSpawned()
        {
            OnMyAgentSpawnedFromVisual?.Invoke();

            // Also invoke the original events since some native components rely on them
            MultiplayerMissionAgentVisualSpawnComponent originalComponent = Mission.Current.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>();
            // Invoke OnMyAgentSpawnedFromVisual using reflection 
            FieldInfo eventField = typeof(MultiplayerMissionAgentVisualSpawnComponent).GetField("OnMyAgentSpawnedFromVisual", BindingFlags.Instance | BindingFlags.NonPublic);
            // Get the delegate attached to the field
            Delegate eventDelegate = (Delegate)eventField.GetValue(originalComponent);
            // Invoke the delegate if it's not null
            eventDelegate?.DynamicInvoke();
        }

        public override void OnPreMissionTick(float dt)
        {
            if (!GameNetwork.IsDedicatedServer && _spawnFrameSelectionHelper == null && Mission.Current != null && GameNetwork.MyPeer != null)
            {
                _spawnFrameSelectionHelper = new VisualSpawnFrameSelectionHelper();
            }
        }
    }

    public class VisualSpawnFrameSelectionHelper
    {
        private GameEntity[] _visualSpawnPoints;

        private GameEntity[] _visualAttackerSpawnPoints;

        private GameEntity[] _visualDefenderSpawnPoints;

        private VirtualPlayer[] _visualSpawnPointUsers;

        public VisualSpawnFrameSelectionHelper()
        {
            _visualSpawnPoints = new GameEntity[6];
            _visualAttackerSpawnPoints = new GameEntity[6];
            _visualDefenderSpawnPoints = new GameEntity[6];
            _visualSpawnPointUsers = new VirtualPlayer[6];
            for (int i = 0; i < 6; i++)
            {
                List<GameEntity> list = Mission.Current.Scene.FindEntitiesWithTag("sp_visual_" + i).ToList();
                if (list.Count > 0)
                {
                    _visualSpawnPoints[i] = list[0];
                }

                list = Mission.Current.Scene.FindEntitiesWithTag("sp_visual_attacker_" + i).ToList();
                if (list.Count > 0)
                {
                    _visualAttackerSpawnPoints[i] = list[0];
                }

                list = Mission.Current.Scene.FindEntitiesWithTag("sp_visual_defender_" + i).ToList();
                if (list.Count > 0)
                {
                    _visualDefenderSpawnPoints[i] = list[0];
                }
            }

            _visualSpawnPointUsers[0] = GameNetwork.MyPeer.VirtualPlayer;
        }

        public MatrixFrame GetSpawnPointFrameForPlayer(VirtualPlayer player, BattleSideEnum side, int agentVisualIndex, int totalTroopCount, bool isMounted = false)
        {
            if (agentVisualIndex == 0)
            {
                int num = -1;
                int num2 = -1;
                for (int i = 0; i < _visualSpawnPointUsers.Length; i++)
                {
                    if (_visualSpawnPointUsers[i] == player)
                    {
                        num = i;
                        break;
                    }

                    if (num2 < 0 && _visualSpawnPointUsers[i] == null)
                    {
                        num2 = i;
                    }
                }

                int num3 = num >= 0 ? num : num2;
                if (num3 >= 0)
                {
                    _visualSpawnPointUsers[num3] = player;
                    GameEntity gameEntity = null;
                    switch (side)
                    {
                        case BattleSideEnum.Attacker:
                            gameEntity = _visualAttackerSpawnPoints[num3];
                            break;
                        case BattleSideEnum.Defender:
                            gameEntity = _visualDefenderSpawnPoints[num3];
                            break;
                    }

                    MatrixFrame result = gameEntity?.GetGlobalFrame() ?? _visualSpawnPoints[num3].GetGlobalFrame();
                    result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                    return result;
                }

                Debug.FailedAssert("Couldn't find a valid spawn point for player.", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\ALAgentVisualSpawnComponent.cs", "GetSpawnPointFrameForPlayer", 139);
                return MatrixFrame.Identity;
            }

            Vec3 origin = _visualSpawnPoints[3].GetGlobalFrame().origin;
            Vec3 origin2 = _visualSpawnPoints[1].GetGlobalFrame().origin;
            Vec3 origin3 = _visualSpawnPoints[5].GetGlobalFrame().origin;
            Mat3 rotation = _visualSpawnPoints[0].GetGlobalFrame().rotation;
            rotation.MakeUnit();
            List<WorldFrame> formationFramesForBeforeFormationCreation = Formation.GetFormationFramesForBeforeFormationCreation(origin2.Distance(origin3), totalTroopCount, isMounted, new WorldPosition(Mission.Current.Scene, origin), rotation);
            if (formationFramesForBeforeFormationCreation.Count < agentVisualIndex)
            {
                return new MatrixFrame(rotation, origin);
            }

            return formationFramesForBeforeFormationCreation[agentVisualIndex - 1].ToGroundMatrixFrame();
        }

        public void FreeSpawnPointFromPlayer(VirtualPlayer player)
        {
            for (int i = 0; i < _visualSpawnPointUsers.Length; i++)
            {
                if (_visualSpawnPointUsers[i] == player)
                {
                    _visualSpawnPointUsers[i] = null;
                    break;
                }
            }
        }
    }
}
