using System;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

namespace Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.ViewModels
{
    public class ALHeroClassGroupVM : ViewModel
    {
        public readonly MultiplayerClassDivisions.MPHeroClassGroup HeroClassGroup;

        private readonly Action<HeroPerkVM, MPPerkVM> _onPerkSelect;

        private string _name;

        private string _iconType;

        private string _iconPath;

        private MBBindingList<ALHeroClassVM> _subClasses;

        public bool IsValid => SubClasses.Count > 0;

        [DataSourceProperty]
        public MBBindingList<ALHeroClassVM> SubClasses
        {
            get
            {
                return _subClasses;
            }
            set
            {
                if (value != _subClasses)
                {
                    _subClasses = value;
                    OnPropertyChangedWithValue(value, "SubClasses");
                }
            }
        }

        [DataSourceProperty]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, "Name");
                }
            }
        }

        [DataSourceProperty]
        public string IconType
        {
            get
            {
                return _iconType;
            }
            set
            {
                if (value != _iconType)
                {
                    _iconType = value;
                    OnPropertyChangedWithValue(value, "IconType");
                    IconPath = "TroopBanners\\ClassType_" + value;
                }
            }
        }

        [DataSourceProperty]
        public string IconPath
        {
            get
            {
                return _iconPath;
            }
            set
            {
                if (value != _iconPath)
                {
                    _iconPath = value;
                    OnPropertyChangedWithValue(value, "IconPath");
                }
            }
        }

        public ALHeroClassGroupVM(Action<ALHeroClassVM> onSelect, Action<HeroPerkVM, MPPerkVM> onPerkSelect, MultiplayerClassDivisions.MPHeroClassGroup heroClassGroup, bool useSecondary)
        {
            HeroClassGroup = heroClassGroup;
            _onPerkSelect = onPerkSelect;
            IconType = heroClassGroup.StringId;
            SubClasses = new MBBindingList<ALHeroClassVM>();
            _ = GameNetwork.MyPeer.GetComponent<MissionPeer>().Team;
            foreach (MultiplayerClassDivisions.MPHeroClass item in from h in MultiplayerClassDivisions.GetMPHeroClasses(GameNetwork.MyPeer.GetComponent<MissionPeer>().Culture)
                                                                   where h.ClassGroup.Equals(heroClassGroup)
                                                                   select h)
            {
                SubClasses.Add(new ALHeroClassVM(onSelect, _onPerkSelect, item, useSecondary));
            }

            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            Name = HeroClassGroup.Name.ToString();
            SubClasses.ApplyActionOnAllItems(delegate (ALHeroClassVM x)
            {
                x.RefreshValues();
            });
        }
    }
}
