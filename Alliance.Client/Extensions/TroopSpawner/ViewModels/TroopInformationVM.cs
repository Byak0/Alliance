using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

namespace Alliance.Client.Extensions.TroopSpawner.ViewModels
{
    /// <summary>
    /// Custom VM based on native HeroInformationVM.
    /// Can handle troop characters in addition to heroes.
    /// </summary>
    public class TroopInformationVM : ViewModel
    {
        private TextObject _armySizeHintWithDefaultValue = new TextObject("{=aalbxe7z}Army Size");

        private ShallowItemVM.ItemGroup _latestSelectedItemGroup;

        private HintViewModel _armySizeHint;

        private HintViewModel _movementSpeedHint;

        private HintViewModel _hitPointsHint;

        private HintViewModel _armorHint;

        private ShallowItemVM _item1;

        private ShallowItemVM _item2;

        private ShallowItemVM _item3;

        private ShallowItemVM _item4;

        private ShallowItemVM _itemHorse;

        private ShallowItemVM _itemSelected;

        private string _information;

        private string _nameText;

        private string _equipmentText;

        private int _movementSpeed;

        private int _hitPoints;

        private int _armySize;

        private int _armor;

        private bool _armyAvailable;

        public MultiplayerClassDivisions.MPHeroClass HeroClass { get; private set; }

        [DataSourceProperty]
        public HintViewModel ArmySizeHint
        {
            get
            {
                return _armySizeHint;
            }
            set
            {
                if (value != _armySizeHint)
                {
                    _armySizeHint = value;
                    OnPropertyChangedWithValue(value, "ArmySizeHint");
                }
            }
        }

        [DataSourceProperty]
        public HintViewModel MovementSpeedHint
        {
            get
            {
                return _movementSpeedHint;
            }
            set
            {
                if (value != _movementSpeedHint)
                {
                    _movementSpeedHint = value;
                    OnPropertyChangedWithValue(value, "MovementSpeedHint");
                }
            }
        }

        [DataSourceProperty]
        public HintViewModel HitPointsHint
        {
            get
            {
                return _hitPointsHint;
            }
            set
            {
                if (value != _hitPointsHint)
                {
                    _hitPointsHint = value;
                    OnPropertyChangedWithValue(value, "HitPointsHint");
                }
            }
        }

        [DataSourceProperty]
        public HintViewModel ArmorHint
        {
            get
            {
                return _armorHint;
            }
            set
            {
                if (value != _armorHint)
                {
                    _armorHint = value;
                    OnPropertyChangedWithValue(value, "ArmorHint");
                }
            }
        }

        [DataSourceProperty]
        public ShallowItemVM Item1
        {
            get
            {
                return _item1;
            }
            set
            {
                if (value != _item1)
                {
                    _item1 = value;
                    OnPropertyChangedWithValue(value, "Item1");
                }
            }
        }

        [DataSourceProperty]
        public ShallowItemVM Item2
        {
            get
            {
                return _item2;
            }
            set
            {
                if (value != _item2)
                {
                    _item2 = value;
                    OnPropertyChangedWithValue(value, "Item2");
                }
            }
        }

        [DataSourceProperty]
        public ShallowItemVM Item3
        {
            get
            {
                return _item3;
            }
            set
            {
                if (value != _item3)
                {
                    _item3 = value;
                    OnPropertyChangedWithValue(value, "Item3");
                }
            }
        }

        [DataSourceProperty]
        public ShallowItemVM Item4
        {
            get
            {
                return _item4;
            }
            set
            {
                if (value != _item4)
                {
                    _item4 = value;
                    OnPropertyChangedWithValue(value, "Item4");
                }
            }
        }

        [DataSourceProperty]
        public ShallowItemVM ItemHorse
        {
            get
            {
                return _itemHorse;
            }
            set
            {
                if (value != _itemHorse)
                {
                    _itemHorse = value;
                    OnPropertyChangedWithValue(value, "ItemHorse");
                }
            }
        }

        [DataSourceProperty]
        public ShallowItemVM ItemSelected
        {
            get
            {
                return _itemSelected;
            }
            set
            {
                if (value != _itemSelected)
                {
                    _itemSelected = value;
                    OnPropertyChangedWithValue(value, "ItemSelected");
                }
            }
        }

        [DataSourceProperty]
        public string Information
        {
            get
            {
                return _information;
            }
            set
            {
                if (value != _information)
                {
                    _information = value;
                    OnPropertyChangedWithValue(value, "Information");
                }
            }
        }

        [DataSourceProperty]
        public string EquipmentText
        {
            get
            {
                return _equipmentText;
            }
            set
            {
                if (value != _equipmentText)
                {
                    _equipmentText = value;
                    OnPropertyChangedWithValue(value, "EquipmentText");
                }
            }
        }

        [DataSourceProperty]
        public string NameText
        {
            get
            {
                return _nameText;
            }
            set
            {
                if (value != _nameText)
                {
                    _nameText = value;
                    OnPropertyChangedWithValue(value, "NameText");
                }
            }
        }

        [DataSourceProperty]
        public int MovementSpeed
        {
            get
            {
                return _movementSpeed;
            }
            set
            {
                if (value != _movementSpeed)
                {
                    _movementSpeed = value;
                    OnPropertyChangedWithValue(value, "MovementSpeed");
                }
            }
        }

        [DataSourceProperty]
        public int ArmySize
        {
            get
            {
                return _armySize;
            }
            set
            {
                if (value != _armySize)
                {
                    _armySize = value;
                    OnPropertyChangedWithValue(value, "ArmySize");
                }
            }
        }

        [DataSourceProperty]
        public int HitPoints
        {
            get
            {
                return _hitPoints;
            }
            set
            {
                if (value != _hitPoints)
                {
                    _hitPoints = value;
                    OnPropertyChangedWithValue(value, "HitPoints");
                }
            }
        }

        [DataSourceProperty]
        public int Armor
        {
            get
            {
                return _armor;
            }
            set
            {
                if (value != _armor)
                {
                    _armor = value;
                    OnPropertyChangedWithValue(value, "Armor");
                }
            }
        }

        [DataSourceProperty]
        public bool IsArmyAvailable
        {
            get
            {
                return _armyAvailable;
            }
            set
            {
                if (value != _armyAvailable)
                {
                    _armyAvailable = value;
                    OnPropertyChangedWithValue(value, "IsArmyAvailable");
                }
            }
        }

        public TroopInformationVM()
        {
            _latestSelectedItemGroup = ShallowItemVM.ItemGroup.None;
            Item1 = new ShallowItemVM(UpdateHighlightedItem);
            Item2 = new ShallowItemVM(UpdateHighlightedItem);
            Item3 = new ShallowItemVM(UpdateHighlightedItem);
            Item4 = new ShallowItemVM(UpdateHighlightedItem);
            ItemHorse = new ShallowItemVM(UpdateHighlightedItem);
            IsArmyAvailable = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0;
            SetFirstSelectedItem();
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            ArmySizeHint = new HintViewModel(GameTexts.FindText("str_army_size"));
            MovementSpeedHint = new HintViewModel(GameTexts.FindText("str_movement_speed"));
            HitPointsHint = new HintViewModel(GameTexts.FindText("str_hitpoints"));
            ArmorHint = new HintViewModel(GameTexts.FindText("str_armor"));
            EquipmentText = GameTexts.FindText("str_equipment").ToString();
            _item1?.RefreshValues();
            _item2?.RefreshValues();
            _item3?.RefreshValues();
            _item4?.RefreshValues();
            _itemHorse?.RefreshValues();
            _itemSelected?.RefreshValues();
            if (HeroClass != null)
            {
                NameText = HeroClass.HeroName.ToString();
            }
        }

        public void RefreshWith(MultiplayerClassDivisions.MPHeroClass heroClass, BasicCharacterObject character, List<IReadOnlyPerkObject> perks)
        {
            HeroClass = heroClass;
            bool isHero = heroClass.HeroCharacter == character;
            Equipment equipment = character.Equipment.Clone();
            MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(perks);
            IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: true);
            if (enumerable != null)
            {
                foreach (var item in enumerable)
                {
                    equipment[item.Item1] = item.Item2;
                }
            }

            ItemHorse.RefreshWith(EquipmentIndex.ArmorItemEndSlot, equipment);
            Item1.RefreshWith(EquipmentIndex.WeaponItemBeginSlot, equipment);
            Item2.RefreshWith(EquipmentIndex.Weapon1, equipment);
            Item3.RefreshWith(EquipmentIndex.Weapon2, equipment);
            Item4.RefreshWith(EquipmentIndex.Weapon3, equipment);
            Information = heroClass.HeroInformation?.ToString();
            NameText = character.Name.ToString();
            int num = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue();
            if (num == 0)
            {
                num = 25;
                _armySizeHintWithDefaultValue.SetTextVariable("OPTION_VALUE", 25);
                ArmySizeHint.HintText = _armySizeHintWithDefaultValue;
            }
            else
            {
                ArmySizeHint.HintText = GameTexts.FindText("str_army_size");
            }

            ArmySize = MPPerkObject.GetTroopCount(heroClass, num, onSpawnPerkHandler);
            MovementSpeed = (int)((isHero ? HeroClass.HeroMovementSpeedMultiplier : HeroClass.TroopMovementSpeedMultiplier) * 100f);
            HitPoints = heroClass.Health;
            Armor = (int)(onSpawnPerkHandler?.GetDrivenPropertyBonusOnSpawn(isPlayer: true, DrivenProperty.ArmorTorso, HeroClass.ArmorValue) ?? 0f) + HeroClass.ArmorValue;
            if (!TrySetSelectedItemByType(_latestSelectedItemGroup))
            {
                SetFirstSelectedItem();
            }
        }

        private bool TrySetSelectedItemByType(ShallowItemVM.ItemGroup itemGroup)
        {
            if (Item1.IsValid && Item1.Type == itemGroup)
            {
                UpdateHighlightedItem(Item1);
                return true;
            }

            if (Item2.IsValid && Item2.Type == itemGroup)
            {
                UpdateHighlightedItem(Item2);
                return true;
            }

            if (Item3.IsValid && Item3.Type == itemGroup)
            {
                UpdateHighlightedItem(Item3);
                return true;
            }

            if (Item4.IsValid && Item4.Type == itemGroup)
            {
                UpdateHighlightedItem(Item4);
                return true;
            }

            if (ItemHorse.IsValid && ItemHorse.Type == itemGroup)
            {
                UpdateHighlightedItem(ItemHorse);
                return true;
            }

            return false;
        }

        private void SetFirstSelectedItem()
        {
            ShallowItemVM itemSelected = ItemSelected;
            if (itemSelected == null || !itemSelected.IsValid)
            {
                if (Item1.IsValid)
                {
                    UpdateHighlightedItem(Item1);
                }
                else if (Item2.IsValid)
                {
                    UpdateHighlightedItem(Item2);
                }
                else if (Item3.IsValid)
                {
                    UpdateHighlightedItem(Item3);
                }
                else if (Item4.IsValid)
                {
                    UpdateHighlightedItem(Item4);
                }
                else if (ItemHorse.IsValid)
                {
                    UpdateHighlightedItem(ItemHorse);
                }
            }
        }

        public void UpdateHighlightedItem(ShallowItemVM item)
        {
            ItemSelected = item;
            Item1.IsSelected = false;
            Item2.IsSelected = false;
            Item3.IsSelected = false;
            Item4.IsSelected = false;
            ItemHorse.IsSelected = false;
            item.IsSelected = true;
            _latestSelectedItemGroup = item.Type;
        }
    }
}
