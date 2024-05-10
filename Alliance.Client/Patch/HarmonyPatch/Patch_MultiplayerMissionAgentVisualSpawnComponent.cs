using Alliance.Common.Extensions.Audio;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
    class Patch_MultiplayerMissionAgentVisualSpawnComponent
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MultiplayerMissionAgentVisualSpawnComponent));
        
        // Using our own spawn frame selection helper because the original one is fking private
        private static AllianceVisualSpawnFrameSelectionHelper SpawnFrameSelectionHelper => _spawnFrameSelectionHelper ??= new AllianceVisualSpawnFrameSelectionHelper();
        private static AllianceVisualSpawnFrameSelectionHelper _spawnFrameSelectionHelper;

        private static List<string> RandomUrukCheers = new List<string>
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
        private static int _lastUrukCheerIndex = -1;

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(MultiplayerMissionAgentVisualSpawnComponent).GetMethod(nameof(MultiplayerMissionAgentVisualSpawnComponent.SpawnAgentVisualsForPeer),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MultiplayerMissionAgentVisualSpawnComponent).GetMethod(
                        nameof(Prefix_SpawnAgentVisualsForPeer), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log("Alliance - ERROR in " + nameof(Patch_MultiplayerMissionAgentVisualSpawnComponent), LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Fix characters with custom races spawning as default humans.
        /// </summary>
        public static bool Prefix_SpawnAgentVisualsForPeer(MultiplayerMissionAgentVisualSpawnComponent __instance, MissionPeer missionPeer, AgentBuildData buildData, int selectedEquipmentSetIndex = -1, bool isBot = false, int totalTroopCount = 0)
        {
            GameNetwork.MyPeer?.GetComponent<MissionPeer>();
            if (buildData.AgentVisualsIndex == 0)
            {
                missionPeer.ClearAllVisuals();
            }

            missionPeer.ClearVisuals(buildData.AgentVisualsIndex);

            // Always free spawn point
            SpawnFrameSelectionHelper.FreeSpawnPointFromPlayer(missionPeer.Peer);

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

            ItemObject horse = equipment[EquipmentIndex.Horse].Item;
            MatrixFrame frame = SpawnFrameSelectionHelper.GetSpawnPointFrameForPlayer(missionPeer.Peer, missionPeer.Team.Side, buildData.AgentVisualsIndex, totalTroopCount, horse != null);
            ActionIndexCache actionIndexCache = ((horse == null) ? SpawningBehaviorBase.PoseActionInfantry : SpawningBehaviorBase.PoseActionCavalry);
            MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(buildData.AgentCharacter);
            MBReadOnlyList<MPPerkObject> selectedPerks = missionPeer.SelectedPerks;
            //float parameter = 0.1f + MBRandom.RandomFloat * 0.8f;
            IAgentVisual agentVisual = null;
            if (horse != null)
            {
                Monster monster = horse.HorseComponent.Monster;
                AgentVisualsData agentVisualsData = new AgentVisualsData().Equipment(equipment).Scale(horse.ScaleFactor).Frame(MatrixFrame.Identity)
                    .ActionSet(MBGlobals.GetActionSet(monster.ActionSetCode))
                    .Scene(Mission.Current.Scene)
                    .Monster(monster)
                    .PrepareImmediately(prepareImmediately: false)
                    .MountCreationKey(MountCreationKey.GetRandomMountKeyString(horse, MBRandom.RandomInt()));
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
                    if (horse.StringId == "mp_aserai_camel")
                    {
                        Debug.Print("Client is spawning a camel for without mountCustomAction from the perk.", 0, Debug.DebugColor.White, 17179869184uL);
                        actionIndexCache2 = (isBot ? ActionIndexCache.Create("act_camel_idle_1") : ActionIndexCache.Create("act_hero_mount_idle_camel"));
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
                    //agentVisual.GetVisuals().GetSkeleton().SetAnimationParameterAtChannel(0, parameter);
                    //agentVisual.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.1f, frame2, tickAnimsForChildren: true);
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

            Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(buildData.AgentRace);
            IAgentVisual agentVisual2 = Mission.Current.AgentVisualCreator.Create(new AgentVisualsData().Equipment(equipment).BodyProperties(buildData.AgentBodyProperties).Frame(frame)
                .ActionSet(MBActionSet.GetActionSet(baseMonsterFromRace.ActionSetCode))
                .Scene(Mission.Current.Scene)
                .Monster(baseMonsterFromRace)
                // Add race to the agent visuals data
                .Race(buildData.AgentRace)
                .PrepareImmediately(prepareImmediately: false)
                .UseMorphAnims(useMorphAnims: true)
                .SkeletonType(buildData.AgentIsFemale ? SkeletonType.Female : SkeletonType.Male)
                .ClothColor1(buildData.AgentClothingColor1)
                .ClothColor2(buildData.AgentClothingColor2)
                .AddColorRandomness(buildData.AgentVisualsIndex != 0)
                .ActionCode(actionIndexCache3), "Mission::SpawnAgentVisuals", needBatchedVersionForWeaponMeshes: true, forceUseFaceCache: false);
            agentVisual2.SetAction(actionIndexCache3);
            //agentVisual2.GetVisuals().GetSkeleton().SetAnimationParameterAtChannel(0, parameter);
            //agentVisual2.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.1f, frame, tickAnimsForChildren: true);
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
                    PlayRandomUrukCheer(frame.origin);
                }

                //__instance.OnMyAgentVisualSpawned?.Invoke();

                // Invoke OnMyAgentVisualSpawned using reflection 
                FieldInfo eventField = typeof(MultiplayerMissionAgentVisualSpawnComponent).GetField("OnMyAgentVisualSpawned", BindingFlags.Instance | BindingFlags.NonPublic);
                // Get the delegate attached to the field
                Delegate eventDelegate = (Delegate)eventField.GetValue(__instance);
                // Invoke the delegate if it's not null
                eventDelegate?.DynamicInvoke();
            }

            Log("Added visuals for " + missionPeer.Name + ".", LogLevel.Debug);

            return false;
        }

        private static void PlayRandomUrukCheer(Vec3 origin)
        {
            _lastUrukCheerIndex = (_lastUrukCheerIndex + 1) % RandomUrukCheers.Count;
            AudioPlayer.Instance.Play(RandomUrukCheers[_lastUrukCheerIndex], 0.5f, soundOrigin: origin);
        }

        public static bool Prefix_RemoveAgentVisuals(MultiplayerMissionAgentVisualSpawnComponent __instance, MissionPeer missionPeer, bool sync = false)
        {
            missionPeer.ClearAllVisuals();
            if (!GameNetwork.IsDedicatedServer && !missionPeer.Peer.IsMine)
            {
                SpawnFrameSelectionHelper.FreeSpawnPointFromPlayer(missionPeer.Peer);
            }

            if (missionPeer.IsMine)
            {
                //__instance.OnMyAgentVisualRemoved();

                // Invoke OnMyAgentVisualRemoved using reflection 
                FieldInfo eventField = typeof(MultiplayerMissionAgentVisualSpawnComponent).GetField("OnMyAgentVisualRemoved", BindingFlags.Instance | BindingFlags.NonPublic);
                // Get the delegate attached to the field
                Delegate eventDelegate = (Delegate)eventField.GetValue(__instance);
                // Invoke the delegate if it's not null
                eventDelegate?.DynamicInvoke();
            }

            Log("Removed visuals for " + missionPeer.Name + ".", LogLevel.Debug);

            return false;
        }
    }

    /// <summary>
    /// Our own VisualSpawnFrameSelectionHelper class because the original one is fking private
    /// </summary>
    public class AllianceVisualSpawnFrameSelectionHelper
    {
        private GameEntity[] _visualSpawnPoints;

        private GameEntity[] _visualAttackerSpawnPoints;

        private GameEntity[] _visualDefenderSpawnPoints;

        private VirtualPlayer[] _visualSpawnPointUsers;

        public AllianceVisualSpawnFrameSelectionHelper()
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

                int num3 = ((num >= 0) ? num : num2);
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

                Debug.FailedAssert("Couldn't find a valid spawn point for player.", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MultiplayerMissionAgentVisualSpawnComponent.cs", "GetSpawnPointFrameForPlayer", 139);
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
                }
            }
        }
    }
}

