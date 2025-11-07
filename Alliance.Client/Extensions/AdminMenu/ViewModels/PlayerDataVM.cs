using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.Security.Models;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
using JetBrains.Annotations;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.PlayerServices;

namespace Alliance.Client.Extensions.AdminMenu.ViewModels
{
	public class PlayerDataVM : ViewModel
	{
		private bool _isSudo;

		private bool _isFiltered;
		private bool _isOnline;
		private string _playerStringId;
		private string _username;
		private bool _sudo;
		private bool _admin;
		private bool _vip;
		private int _warningCount;
		private string _lastWarning;
		private int _kickCount;
		private int _banCount;
		private bool _isMuted;
		private bool _isBanned;
		private string _lastBanReason;
		private string _sanctionEnd;
		private AL_PlayerData _playerData;
		private PlayerId _playerId;

		[DataSourceProperty]
		public bool IsSudo
		{
			get
			{
				return _isSudo;
			}
			set
			{
				if (value != _isSudo)
				{
					_isSudo = value;
					OnPropertyChangedWithValue(value, "IsSudo");
				}
			}
		}

		[DataSourceProperty]
		public bool IsFiltered
		{
			get => _isFiltered;
			set
			{
				if (value != _isFiltered)
				{
					_isFiltered = value;
					OnPropertyChangedWithValue(value, "IsFiltered");
				}
			}
		}

		[DataSourceProperty]
		public bool IsOnline
		{
			get => _isOnline;
			set
			{
				if (value != _isOnline)
				{
					_isOnline = value;
					OnPropertyChangedWithValue(value, "IsOnline");
				}
			}
		}

		[DataSourceProperty]
		public string PlayerStringId
		{
			get => _playerStringId;
			set
			{
				if (value != _playerStringId)
				{
					_playerStringId = value;
					OnPropertyChangedWithValue(value, "PlayerStringId");
				}
			}
		}

		[DataSourceProperty]
		public string Username
		{
			get => _username;
			set
			{
				if (value != _username)
				{
					_username = value;
					OnPropertyChangedWithValue(value, "Username");
				}
			}
		}

		[DataSourceProperty]
		public bool Sudo
		{
			get => _sudo;
			set
			{
				if (value != _sudo)
				{
					_sudo = value;
					OnPropertyChangedWithValue(value, "Sudo");
				}
			}
		}

		[DataSourceProperty]
		public bool Admin
		{
			get => _admin;
			set
			{
				if (value != _admin)
				{
					_admin = value;
					OnPropertyChangedWithValue(value, "Admin");
				}
			}
		}

		[DataSourceProperty]
		public bool VIP
		{
			get => _vip;
			set
			{
				if (value != _vip)
				{
					_vip = value;
					OnPropertyChangedWithValue(value, "VIP");
				}
			}
		}

		[DataSourceProperty]
		public int WarningCount
		{
			get => _warningCount;
			set
			{
				if (value != _warningCount)
				{
					_warningCount = value;
					OnPropertyChangedWithValue(value, "WarningCount");
				}
			}
		}

		[DataSourceProperty]
		public string LastWarning
		{
			get => _lastWarning;
			set
			{
				if (value != _lastWarning)
				{
					_lastWarning = value;
					OnPropertyChangedWithValue(value, "LastWarning");
				}
			}
		}

		[DataSourceProperty]
		public int KickCount
		{
			get => _kickCount;
			set
			{
				if (value != _kickCount)
				{
					_kickCount = value;
					OnPropertyChangedWithValue(value, "KickCount");
				}
			}
		}

		[DataSourceProperty]
		public int BanCount
		{
			get => _banCount;
			set
			{
				if (value != _banCount)
				{
					_banCount = value;
					OnPropertyChangedWithValue(value, "BanCount");
				}
			}
		}

		[DataSourceProperty]
		public bool IsMuted
		{
			get => _isMuted;
			set
			{
				if (value != _isMuted)
				{
					_isMuted = value;
					OnPropertyChangedWithValue(value, "IsMuted");
				}
			}
		}

		[DataSourceProperty]
		public bool IsBanned
		{
			get => _isBanned;
			set
			{
				if (value != _isBanned)
				{
					_isBanned = value;
					OnPropertyChangedWithValue(value, "IsBanned");
				}
			}
		}

		[DataSourceProperty]
		public string LastBanReason
		{
			get => _lastBanReason;
			set
			{
				if (value != _lastBanReason)
				{
					_lastBanReason = value;
					OnPropertyChangedWithValue(value, "LastBanReason");
				}
			}
		}

		[DataSourceProperty]
		public string SanctionEnd
		{
			get => _sanctionEnd;
			set
			{
				if (value != _sanctionEnd)
				{
					_sanctionEnd = value;
					OnPropertyChangedWithValue(value, "SanctionEnd");
				}
			}
		}

		public PlayerDataVM(AL_PlayerData playerData)
		{
			_isSudo = GameNetwork.MyPeer.IsSudo();

			_playerData = playerData;
			_playerId = playerData.Id;
			_playerStringId = playerData.Id.ToString();
			_username = playerData.Name ?? "";
			_sudo = playerData.Sudo;
			_admin = playerData.Admin;
			_vip = playerData.VIP;
			_warningCount = playerData.WarningCount;
			_lastWarning = playerData.LastWarning;
			_kickCount = playerData.KickCount;
			_banCount = playerData.BanCount;
			_isBanned = playerData.IsBanned;
			_lastBanReason = playerData.LastBanReason;
			_sanctionEnd = playerData.SanctionEnd != null && playerData.SanctionEnd != System.DateTime.MinValue ? playerData.SanctionEnd.ToString() : "N/A";
			_isMuted = playerData.IsMuted;
			_isFiltered = false;
		}

		public PlayerDataVM(NetworkCommunicator player)
		{
			_isSudo = GameNetwork.MyPeer.IsSudo();

			_playerId = player.VirtualPlayer.Id;
			_playerStringId = _playerId.ToString();
			_username = player.UserName ?? "";
			_isOnline = true;
			_sudo = false;
			_admin = false;
			_vip = false;
			_warningCount = 0;
			_lastWarning = "N/A";
			_kickCount = 0;
			_banCount = 0;
			_isBanned = false;
			_lastBanReason = "N/A";
			_sanctionEnd = "N/A";
			_isMuted = false;
			_isFiltered = false;
		}

		[UsedImplicitly]
		public void ToggleSudo()
		{
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { SetSudo = true, PlayerSelected = _playerId });
		}

		[UsedImplicitly]
		public void ToggleAdmin()
		{
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { SetAdmin = true, PlayerSelected = _playerId });
		}

		[UsedImplicitly]
		public void ToggleVIP()
		{
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { SetVIP = true, PlayerSelected = _playerId });
		}

		[UsedImplicitly]
		public void UnbanPlayer()
		{
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { Unban = true, PlayerSelected = _playerId });
		}

		[UsedImplicitly]
		public void ToggleMute()
		{
			ClientAdminMenuMsg.SendMessageToServer(new AdminClient() { ToggleMutePlayer = true, PlayerSelected = _playerId });
		}

		public void UpdateFrom(AL_PlayerData newData)
		{
			Username = newData.Name;
			Sudo = newData.Sudo;
			Admin = newData.Admin;
			VIP = newData.VIP;
			WarningCount = newData.WarningCount;
			LastWarning = newData.LastWarning;
			KickCount = newData.KickCount;
			BanCount = newData.BanCount;
			IsMuted = newData.IsMuted;
			IsBanned = newData.IsBanned;
			LastBanReason = newData.LastBanReason;
			SanctionEnd = newData.SanctionEnd != null && newData.SanctionEnd != System.DateTime.MinValue ? newData.SanctionEnd.ToString() : "N/A";
		}
	}
}
