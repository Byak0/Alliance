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
	public class ZeventTentBehavior : MissionNetwork, IMissionBehavior
	{
		struct TentOrigins
		{
			public int Id;
			public MatrixFrame Frame;
		}

		struct ZeventTentData
		{
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

			if (Mission.Current?.SceneName != ZeventConst.ZEVENT_MAP_NAME) return;

			InitTentOrigins();
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

			ZeventTentData existingTent = _spawnedTents.FirstOrDefault(e => e.TentId == tentId);
			if (existingTent.Name != null)
			{
				// There is already a tent existing, we need to remove it
				string alrTentPref = GetTentPrefab(existingTent.Tier, existingTent.Variant);
				List<GameEntity> entities = new List<GameEntity>();
				// VERY UGLY AND VERY BAD PERFORMANCE BUT THX TALEWORLD (This time at least...) IT WORK FINE
				Mission.Current.Scene.GetEntities(ref entities);
				GameEntity alrTentEnti = entities.Where(e => tentOrigin.Frame.NearlyEquals(e.GetGlobalFrame(), 0.5f) && e.Name.StartsWith("building_medieval_tente_")).FirstOrDefault();

				if (alrTentEnti != null)
				{
					alrTentEnti.SetVisibilityExcludeParents(false);
					alrTentEnti.RemoveAllChildren();
					alrTentEnti.Remove(1);
				}

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
				TentId = tentId,
				Tier = tier,
				Variant = variant,
				TotalDonations = totalDonations,
				Name = name,
				Message = message
			});

			Log($"Spawned tent n°{tentId} for {name}", LogLevel.Debug);

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
