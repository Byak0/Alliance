using Alliance.Common.Core.Security.Extension;
using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.GameModes.PvC.Behaviors
{
    public class PvCRepresentative : MissionRepresentativeBase
    {
        public static PvCRepresentative Main => GameNetwork.MyPeer?.GetComponent<PvCRepresentative>();

        public event EventHandler OnStateChange;

        public Sides Side = Sides.None;

        public bool IsCommander
        {
            get
            {
                return Side == Sides.Commander || Peer.IsCommander();
            }
        }

        public PvCRepresentative()
        {
        }

        public int GetGoldGainsFromKillData(MPPerkObject.MPPerkHandler killerPerkHandler, MPPerkObject.MPPerkHandler assistingHitterPerkHandler, MultiplayerClassDivisions.MPHeroClass victimClass, bool isAssist, bool teamKill)
        {
            if (teamKill || Gold < 0)
            {
                return 0;
            }
            int num;
            if (isAssist)
            {
                num = (killerPerkHandler != null ? killerPerkHandler.GetRewardedGoldOnAssist() : 0) + (assistingHitterPerkHandler != null ? assistingHitterPerkHandler.GetGoldOnAssist() : 0);
            }
            else
            {
                int num2 = ControlledAgent != null ? MultiplayerClassDivisions.GetMPHeroClassForCharacter(ControlledAgent.Character).TroopBattleCost : 0;
                num = killerPerkHandler != null ? killerPerkHandler.GetGoldOnKill(num2, victimClass.TroopBattleCost) : 0;
            }
            if (num > 0)
            {
                /*GameNetwork.BeginModuleEventAsServer(Peer);
                GameNetwork.WriteMessage(new GoldGain(new List<KeyValuePair<ushort, int>>
                {
                    new KeyValuePair<ushort, int>(2048, num)
                }));
                GameNetwork.EndModuleEventAsServer();*/
            }
            return num;
        }

        public int GetGoldGainsFromAllyDeathReward(int baseAmount)
        {
            if (Gold < 0)
            {
                return 0;
            }
            if (baseAmount > 0 && !Peer.Communicator.IsServerPeer && Peer.Communicator.IsConnectionActive)
            {
                /*GameNetwork.BeginModuleEventAsServer(Peer);
                GameNetwork.WriteMessage(new GoldGain(new List<KeyValuePair<ushort, int>>
                {
                    new KeyValuePair<ushort, int>(2048, baseAmount)
                }));
                GameNetwork.EndModuleEventAsServer();*/
            }
            return baseAmount;
        }

        public int GetGoldGainFromKillDataAndUpdateFlags(MultiplayerClassDivisions.MPHeroClass victimClass, bool isAssist)
        {
            int num = 0;
            int num2 = 50;
            List<KeyValuePair<ushort, int>> list = new List<KeyValuePair<ushort, int>>();
            if (ControlledAgent != null)
            {
                num2 += victimClass.TroopBattleCost - MultiplayerClassDivisions.GetMPHeroClassForCharacter(ControlledAgent.Character).TroopBattleCost / 2;
            }
            if (isAssist)
            {
                int num3 = MathF.Max(5, num2 / 10);
                num += num3;
                list.Add(new KeyValuePair<ushort, int>(256, num3));
            }
            else if (ControlledAgent != null)
            {
                int num4 = MathF.Max(10, num2 / 5);
                num += num4;
                list.Add(new KeyValuePair<ushort, int>(128, num4));
            }
            if (list.Count > 0 && !Peer.Communicator.IsServerPeer && Peer.Communicator.IsConnectionActive)
            {
                /*GameNetwork.BeginModuleEventAsServer(Peer);
                GameNetwork.WriteMessage(new GoldGain(list));
                GameNetwork.EndModuleEventAsServer();*/
            }
            return num;
        }

        public enum Sides
        {
            None,
            Player,
            Commander,
            Spectator
        }
    }
}
