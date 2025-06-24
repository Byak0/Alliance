using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.FlagMarker.Targets;

namespace Alliance.Client.Extensions.ExNativeUI.MarkerUIHandler.ViewModels
{
	public class CustomMissionPeerMarkerTargetVM : MissionMarkerTargetVM
	{
		private const string _partyMemberColor = "#00FF00FF";

		private const string _friendColor = "#FFFF00FF";

		private const string _clanMemberColor = "#00FFFFFF";

		private bool _isFriend;

		private bool _isSelected;

		public bool IsSelected
		{
			get
			{
				return _isSelected;
			}
			set
			{
				_isSelected = value;

				if (_isSelected)
				{
					// Update colors
					uint color = new Color(1f, 0.1f, 0f, 1f).ToUnsignedInteger();
					base.Name = GetDetailedName();
					Distance = 1;
					RefreshColor(0, color);
					OnPropertyChangedWithValue(1, "Distance");
				}
				else
				{
					base.Name = TargetPeer.DisplayedName;
					SetVisual();
				}
			}
		}

		public MissionPeer TargetPeer { get; private set; }

		public override Vec3 WorldPosition
		{
			get
			{
				if (TargetPeer?.ControlledAgent != null)
				{
					return TargetPeer.ControlledAgent.Position + new Vec3(0f, 0f, TargetPeer.ControlledAgent.GetEyeGlobalHeight());
				}

				Debug.FailedAssert("No target found!", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\FlagMarker\\Targets\\MissionPeerMarkerTargetVM.cs", "WorldPosition", 27);
				return Vec3.One;
			}
		}

		protected override float HeightOffset => 0.75f;

		public CustomMissionPeerMarkerTargetVM(MissionPeer peer, bool isFriend)
			: base(MissionMarkerType.Peer)
		{
			TargetPeer = peer;
			_isFriend = isFriend;
			base.Name = peer.DisplayedName;
			SetVisual();
		}

		private string GetDetailedName()
		{
			string detailedInfos = "*[" + TargetPeer.Name + "]*\n\n";
			detailedInfos = detailedInfos.Add($"Death: {TargetPeer.DeathCount}");
			detailedInfos = detailedInfos.Add($"Kill: {TargetPeer.KillCount}");
			detailedInfos = detailedInfos.Add($"Assist: {TargetPeer.AssistCount}");
			detailedInfos = detailedInfos.Add($"Score: {TargetPeer.Score}");

			return detailedInfos;
		}

		private void SetVisual()
		{
			string color = "#FFFFFFFF";
			if (NetworkMain.GameClient.IsInParty && NetworkMain.GameClient.PlayersInParty.Any((PartyPlayerInLobbyClient p) => p.PlayerId.Equals(TargetPeer.Peer.Id)))
			{
				color = "#00FF00FF";
			}
			else if (_isFriend)
			{
				color = "#FFFF00FF";
			}
			else if (NetworkMain.GameClient.IsInClan && NetworkMain.GameClient.PlayersInClan.Any((ClanPlayer p) => p.PlayerId.Equals(TargetPeer.Peer.Id)))
			{
				color = "#00FFFFFF";
			}

			uint color2 = TaleWorlds.Library.Color.ConvertStringToColor("#FFFFFFFF").ToUnsignedInteger();
			uint color3 = TaleWorlds.Library.Color.ConvertStringToColor(color).ToUnsignedInteger();
			RefreshColor(color2, color3);
		}

		public override void UpdateScreenPosition(Camera missionCamera)
		{
			if (TargetPeer?.ControlledAgent != null)
			{
				base.UpdateScreenPosition(missionCamera);
			}
		}
	}
}
