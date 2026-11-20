using Alliance.Common.Extensions.ToggleEntities.NetworkMessages;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.ToggleEntities.Behaviors
{
	/// <summary>
	/// Used to toggle entities visibility based on their tag.
	/// Keep tags and visibility in memory for new players joining the mission.
	/// </summary>
	public class ToggleEntitiesBehavior : MissionNetwork, IMissionBehavior
	{
		private Dictionary<string, bool> _tagVisibility = new Dictionary<string, bool>();
		private Dictionary<KeyValuePair<MissionObjectId, string>, bool> _localTagVisibility = new Dictionary<KeyValuePair<MissionObjectId, string>, bool>();

		public ToggleEntitiesBehavior() : base()
		{
		}

		public void SetTagVisibility(string tag, bool visible)
		{
			if (Mission.Current?.Scene == null) return;

			_tagVisibility[tag] = visible;

			foreach (GameEntity entity in Mission.Current.Scene.FindEntitiesWithTag(tag))
			{
				entity.SetVisibilityExcludeParents(visible);
			}

			// Broadcast to all clients
			ToggleEntitiesMsg.SyncToggleEntities(tag, visible);
		}

		public void SetLocalTagVisibility(MissionObject missionObject, string tag, bool visible)
		{
			if (Mission.Current?.Scene == null || missionObject == null) return;
			var key = new KeyValuePair<MissionObjectId, string>(missionObject.Id, tag);
			_localTagVisibility[key] = visible;
			foreach (WeakGameEntity entity in missionObject.GameEntity.CollectChildrenEntitiesWithTag(tag))
			{
				entity.SetVisibilityExcludeParents(visible);
			}
			if (missionObject.GameEntity.HasTag(tag))
			{
				missionObject.GameEntity.SetVisibilityExcludeParents(visible);
			}

			// Broadcast to all clients
			ToggleEntitiesMsg.SyncToggleEntitiesLocal(tag, visible, missionObject.Id);
		}

		protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
		{
			// Send the tags visibility state to the new peer
			foreach (var kvp in _tagVisibility)
			{
				ToggleEntitiesMsg.SyncToggleEntities(networkPeer, kvp.Key, kvp.Value);
			}

			foreach (var kvp in _localTagVisibility)
			{
				ToggleEntitiesMsg.SyncToggleEntitiesLocal(networkPeer, kvp.Key.Value, kvp.Value, kvp.Key.Key);
			}
		}
	}
}
