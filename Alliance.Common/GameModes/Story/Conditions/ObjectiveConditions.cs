using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Conditions
{
	[Serializable]
	[PhrasePreview("{Side} eliminated")]
	[PhraseTemplate("{Side} has no agents left")]
	public class SideEliminatedCondition : Condition
	{
		[ConfigProperty(label: "Side", tooltip: "Which side to check for elimination.")]
		public SideType Side = SideType.Attacker;

		public SideEliminatedCondition() { }

		public SideEliminatedCondition(SideType side) { Side = side; }

		public override bool Evaluate(VariableStore context)
		{
			if (Mission.Current == null) return false;
			Team team = Side == SideType.Attacker ? Mission.Current.AttackerTeam : Mission.Current.DefenderTeam;
			return team == null || team.ActiveAgents.Count == 0;
		}
	}

	[Serializable]
	[PhrasePreview("{RequiredKills} {Side} killed")]
	[PhraseTemplate("{RequiredKills} kills from {Side}")]
	public class KillCountCondition : Condition
	{
		[ConfigProperty(label: "Side", tooltip: "Which side's deaths to count.")]
		public SideType Side = SideType.Attacker;

		[ConfigProperty(label: "Required kills")]
		public int RequiredKills = 10;

		[NonSerialized]
		private int _killCount;

		public KillCountCondition() { }

		public KillCountCondition(SideType side, int requiredKills)
		{
			Side = side;
			RequiredKills = requiredKills;
		}

		public override void Register(WeakGameEntity entity)
		{
			base.Register(entity);
			_killCount = 0;
			var b = Mission.Current?.GetMissionBehavior<Behaviors.ObjectivesBehavior>();
			if (b != null)
			{
				if (Side == SideType.Attacker) b.UpdateTotalAttackersKilled += OnKill;
				else b.UpdateTotalDefendersKilled += OnKill;
			}
		}

		public override void Unregister()
		{
			var b = Mission.Current?.GetMissionBehavior<Behaviors.ObjectivesBehavior>();
			if (b != null)
			{
				if (Side == SideType.Attacker) b.UpdateTotalAttackersKilled -= OnKill;
				else b.UpdateTotalDefendersKilled -= OnKill;
			}
		}

		private void OnKill(int total) { _killCount = total; }

		public override bool Evaluate(VariableStore context) => _killCount >= RequiredKills;
	}

	[Serializable]
	[PhrasePreview("{Side} captured {ZoneId}")]
	[PhraseTemplate("{Side} controls zone {ZoneId}")]
	public class ZoneCapturedCondition : Condition
	{
		[ConfigProperty(label: "Zone ID", tooltip: "ID of the CS_CapturableZone to check.")]
		public string ZoneId = "";

		[ConfigProperty(label: "Side", tooltip: "Which side should control the zone.")]
		public SideType Side = SideType.Attacker;

		public ZoneCapturedCondition() { }

		public ZoneCapturedCondition(string zoneId, SideType side)
		{
			ZoneId = zoneId;
			Side = side;
		}

		public override bool Evaluate(VariableStore context)
		{
			if (Mission.Current == null || string.IsNullOrEmpty(ZoneId)) return false;
			var zone = Mission.Current.MissionObjects.FindAllWithType<Extensions.FlagsTracker.Scripts.CS_CapturableZone>()
				.FirstOrDefault(cz => cz.ZoneId != null && cz.ZoneId.ToLower() == ZoneId.ToLower());
			return zone != null && (int)zone.Owner == (int)Side;
		}
	}
}
