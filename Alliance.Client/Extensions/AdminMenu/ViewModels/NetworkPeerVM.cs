using System;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.AdminMenu.ViewModels
{
    public class NetworkPeerVM : ViewModel
    {
        private Action<NetworkPeerVM> _onSelect;
        private int _agentIndex;
        private string _PeerId;
        private string _Username;
        private bool _isSelected;
        private bool _isFiltered;
        private bool _isMuted;

        public Action<NetworkPeerVM> OnSelect
        {
            get => _onSelect;
            set => _onSelect = value;
        }

        [DataSourceProperty]
        public int AgentIndex
        {
            get
            {
                return _agentIndex;
            }
            set
            {
                if (value != _agentIndex)
                {
                    _agentIndex = value;
                    OnPropertyChangedWithValue(value, "AgentIndex");
                }
            }
        }

        [DataSourceProperty]
        public string PeerId
        {
            get
            {
                return _PeerId;
            }
            set
            {
                if (value != _PeerId)
                {
                    _PeerId = value;
                    OnPropertyChangedWithValue(value, "PeerId");
                }
            }
        }

        [DataSourceProperty]
        public string Username
        {
            get
            {
                return _Username;
            }
            set
            {
                if (value != _Username)
                {
                    _Username = value;
                    OnPropertyChangedWithValue(value, "Username");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, "IsSelected");
                }
            }
        }

        [DataSourceProperty]
        public bool IsFiltered
        {
            get
            {
                return _isFiltered;
            }
            set
            {
                if (value != _isFiltered)
                {
                    _isFiltered = value;
                    OnPropertyChangedWithValue(value, "IsFiltered");
                }
            }
        }
		public bool IsMuted
		{
			get
			{
				return _isMuted;
			}
			set
			{
				if (value != _isMuted)
				{
					_isMuted = value;
					OnPropertyChangedWithValue(value, "IsMuted");
				}
			}
		}

        public void SelectPeer()
        {
            OnSelect(this);
        }
    }
}
