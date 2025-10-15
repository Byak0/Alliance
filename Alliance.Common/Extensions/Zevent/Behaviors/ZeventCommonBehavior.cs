using Alliance.Common.Extensions.CustomScripts.Scripts;
using Alliance.Common.Extensions.UsableEntity.Utilities;
using Alliance.Common.Extensions.Zevent.NetworkMessages.FromServer;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.Zevent.Behaviors
{
	public class ZeventCommonBehavior : MissionNetwork, IMissionBehavior
	{
		struct TentOrigins
		{
			public int Id;
			public MatrixFrame Frame;
		}

		struct ZeventTentData
		{
			public GameEntity Entity;
			public int TentId;
			public int Tier;
			public int Variant;
			public int TotalDonations;
			public string Name;
			public string Message;
		}

		private List<TentOrigins> _tentOrigins = new List<TentOrigins>();
		private List<ZeventTentData> _spawnedTents = new List<ZeventTentData>();

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();

			if (Mission.Current?.SceneName != ZeventConst.ZEVENT_MAP_NAME && Mission.Current?.SceneName != ZeventConst.ZEVENT_MAP_NIGHT_NAME) return;

			InitTentOrigins();

			// Set corpse fade out time to 2 second
			Mission.Current.SetMissionCorpseFadeOutTimeInSeconds(2f);
		}

		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
		}

		protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
		{
			base.HandleNewClientAfterSynchronized(networkPeer);
			SyncTentsForClient(networkPeer);
		}

		private void SyncTentsForClient(NetworkCommunicator networkPeer)
		{
			if (!GameNetwork.IsServer) return;
			foreach (ZeventTentData tent in _spawnedTents)
			{
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new ZEventInitTent(tent.TentId, tent.Tier, tent.Variant, tent.TotalDonations, tent.Name, tent.Message));
				GameNetwork.EndModuleEventAsServer();
			}
		}

		private void InitTentOrigins()
		{
			IEnumerable<GameEntity> tentOrigins = Mission.Current.Scene.FindEntitiesWithTag("tent_origin");
			foreach (GameEntity entity in tentOrigins)
			{
				if (int.TryParse(entity.GetTagValue("tent_id_"), out int id))
				{
					_tentOrigins.Add(new TentOrigins { Id = id, Frame = entity.GetGlobalFrame() });
				}
			}
		}

		private string GetTentPrefab(int tier, int variant)
		{
			return $"building_medieval_tente_t{tier}_{variant}";
		}

		public void SpawnTent(int tentId, int tier, int variant, int totalDonations, string name, string message)
		{
			TentOrigins tentOrigin = _tentOrigins.FirstOrDefault(t => t.Id == tentId);
			if (tentOrigin.Id == 0)
			{
				Log($"Can't find tent origin for donator {name} with tag {tentId}", LogLevel.Warning);
				return;
			}

			bool isNew = false;

			ZeventTentData existingTent = _spawnedTents.FirstOrDefault(e => e.TentId == tentId);
			if (existingTent.Entity != null)
			{
				isNew = true;
				// Remove existing entity before spawning a new one
				existingTent.Entity.SetVisibilityExcludeParents(false);
				existingTent.Entity.RemoveAllChildren();
				existingTent.Entity.Remove(1);
				_spawnedTents.Remove(existingTent);
			}

			string tentPrefab = GetTentPrefab(tier, variant);
			GameEntity tentEntity = GameEntity.Instantiate(Mission.Current.Scene, tentPrefab, false);
			tentEntity.SetGlobalFrame(tentOrigin.Frame);
			CS_TextPanel textPanel = tentEntity.GetFirstScriptInFamilyDescending<CS_TextPanel>();
			if (textPanel != null)
			{
				//N°123 Inconnu123456\n"Salut les loulous"\n Tier 1 (10E)
				string text = $"N°{tentId} {name}\n\"{message}\"\n Tier {tier} ({totalDonations} E)";
				textPanel.UpdateText(text);
				textPanel.Render();
			}

			_spawnedTents.Add(new ZeventTentData
			{
				Entity = tentEntity,
				TentId = tentId,
				Tier = tier,
				Variant = variant,
				TotalDonations = totalDonations,
				Name = name,
				Message = message
			});

			if (isNew)
				Log($"Une nouvelle tente a été créée pour {name} (emplacement {tentId}/tier {tier}) !", LogLevel.Information);
			else
				Log($"La tente de {name} a été mise à jour (emplacement {tentId}/tier {tier}) !", LogLevel.Information);

			if (GameNetwork.IsServer)
			{
				// Broadcast to all clients to spawn the tent too
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new ZEventInitTent(tentId, tier, variant, totalDonations, name, message));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}
	}
}
