using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.TroopSpawner.Utilities
{
    /// <summary>
    /// Helper for sending spawn requests to the server.
    /// </summary>
    public class SpawnRequestHelper
    {
        /// <summary>
        /// "Classic" recruit command. Will request a recruitment based on the current SpawnTroopsModel content.
        /// The troops will spawn at the closest spawnpoint available relative to the player.
        /// </summary>
        public static void RequestSpawnTroop()
        {
            RequestSpawnTroop(SpawnTroopsModel.Instance.SelectedTroop, SpawnTroopsModel.Instance.FormationSelected, SpawnTroopsModel.Instance.TroopCount, SpawnTroopsModel.Instance.Difficulty);
        }

        /// <summary>
        /// "Classic" recruit command. Will request a recruitment based on the given parameters.
        /// The troops will spawn at the closest spawnpoint available relative to the player.
        /// </summary>
        public static void RequestSpawnTroop(BasicCharacterObject troop, int formation, int troopCount, float difficulty)
        {
            // Get either camera or agent position 
            MatrixFrame _spawnFrame = Mission.Current.GetCameraFrame();
            if (Agent.Main?.Position != null) _spawnFrame = new MatrixFrame(Mat3.Identity, Agent.Main.Position);

            // Play a sound because why not
            Vec3 position = _spawnFrame.origin + _spawnFrame.rotation.u;
            MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/battle_winning"), position);

            // Send a request to spawn to the server
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestSpawnTroop(
                _spawnFrame,
                false,
                troop,
                formation,
                troopCount,
                difficulty));
            GameNetwork.EndModuleEventAsClient();
        }

        /// <summary>
        /// Special recruit command for admins.
        /// Will spawn one troop at the given position, based on the current SpawnTroopsModel content.
        /// </summary>
        public static void AdminRequestSpawnTroop(Vec3 groundPos)
        {
            AdminRequestSpawnTroop(groundPos, SpawnTroopsModel.Instance.SelectedTroop, SpawnTroopsModel.Instance.FormationSelected, SpawnTroopsModel.Instance.TroopCount, SpawnTroopsModel.Instance.Difficulty);
        }

        /// <summary>
        /// Special recruit command for admins.
        /// Will spawn troops from the given parameters.
        /// </summary>
        public static void AdminRequestSpawnTroop(Vec3 groundPos, BasicCharacterObject troop, int formation, int troopCount, float difficulty)
        {
            if (!GameNetwork.MyPeer.IsAdmin()) return;
            MatrixFrame _spawnFrame = new MatrixFrame(Mat3.Identity, groundPos);
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestSpawnTroop(
                _spawnFrame,
                true,
                troop,
                formation,
                troopCount,
                difficulty));
            GameNetwork.EndModuleEventAsClient();
        }

        /// <summary>
        /// Dev command. Spawn the thing at given location.
        /// </summary>
        public static void RequestSpawnTheThing(Vec3 groundPos)
        {
            if (!GameNetwork.MyPeer.IsDev()) return;
            MatrixFrame _spawnFrame = new MatrixFrame(Mat3.Identity, groundPos);
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestSpawnThing(_spawnFrame));
            GameNetwork.EndModuleEventAsClient();
        }
    }
}
