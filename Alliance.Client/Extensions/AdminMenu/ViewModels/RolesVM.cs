using System;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.AdminMenu.ViewModels
{
    public class RolesVM : ViewModel
    {
        private SelectorVM<RoleVM> _role;
        public string SelectedRoleId;

        public RolesVM()
        {
            _role = new SelectorVM<RoleVM>(0, new Action<SelectorVM<RoleVM>>(OnRoleChange));
            _role.AddItem(new RoleVM("0", "Test", false));
            _role.AddItem(new RoleVM("0", "Test1", false));
            _role.AddItem(new RoleVM("0", "Test2", false));
            _role.AddItem(new RoleVM("0", "Test3", false));
            SelectedRoleId = "0";
            _role.SelectedIndex = 0;
        }

        private void OnRoleChange(SelectorVM<RoleVM> selector)
        {
            SelectedRoleId = selector.SelectedItem.RoleId;
        }

        [DataSourceProperty]
        public SelectorVM<RoleVM> Role
        {
            get
            {
                return _role;
            }
            set
            {
                if (value != _role)
                {
                    _role = value;
                    OnPropertyChangedWithValue(value, "Role");
                }
            }
        }
    }

    public class RoleVM : SelectorItemVM
    {
        public string RoleId { get; private set; }
        public string Name { get; private set; }
        public bool IsRankSelected { get; private set; }

        public RoleVM(string roleId, string name, bool isSelected) : base(name)
        {
            RoleId = roleId;
            Name = name;
            IsSelected = isSelected;
        }
    }
}
