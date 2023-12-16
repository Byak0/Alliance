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
    /// Handle spawn requests to the server.
    /// </summary>
    public class SpawnRequestHelper
    {
        // Classic recruit command - Recruit selected troops at closest spawn point
        public static void RequestSpawnTroop()
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
                SpawnTroopsModel.Instance.SelectedTroop,
                SpawnTroopsModel.Instance.FormationSelected,
                SpawnTroopsModel.Instance.TroopCount,
                SpawnTroopsModel.Instance.Difficulty));
            GameNetwork.EndModuleEventAsClient();
        }

        // Classic recruit command - Recruit selected troops at closest spawn point
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

        // Admin command - Spawn troop at exact location
        public static void AdminRequestSpawnTroop(Vec3 groundPos)
        {
            if (!GameNetwork.MyPeer.IsAdmin()) return;
            MatrixFrame _spawnFrame = new MatrixFrame(Mat3.Identity, groundPos);
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestSpawnTroop(
                _spawnFrame,
                true,
                SpawnTroopsModel.Instance.SelectedTroop,
                SpawnTroopsModel.Instance.FormationSelected,
                1,
                SpawnTroopsModel.Instance.Difficulty));
            GameNetwork.EndModuleEventAsClient();
        }

        // Dev command - Spawn the thing at exact location
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
