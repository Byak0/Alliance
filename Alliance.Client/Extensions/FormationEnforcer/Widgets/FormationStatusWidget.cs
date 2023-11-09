using Alliance.Client.Extensions.TroopSpawner.Widgets;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using HAlign = TaleWorlds.GauntletUI.HorizontalAlignment;
using SP = TaleWorlds.GauntletUI.SizePolicy;
using VAlign = TaleWorlds.GauntletUI.VerticalAlignment;

namespace Alliance.Client.Extensions.FormationEnforcer.Widgets
{
    /*
     * Formation Status widget
     * Show current formation state of player (in formation/skirmish/rambo)
     */
    public class FormationStatusWidget : BrushWidget
    {
        public int SoldierIcons
        {
            get
            {
                return _soldierIcons;
            }
            set
            {
                if (_soldierIcons != value)
                {
                    _soldierIcons = value;
                    RefreshSoldierIcons();
                }
            }
        }

        private int _soldierIcons = 3;

        private Widget _soldierIconsContainer;

        public FormationStatusWidget(UIContext context) : base(context)
        {
            UpdateChildrenStates = true;

            _soldierIconsContainer = new Widget(context);
            _soldierIconsContainer.Init(SP.StretchToParent, SP.StretchToParent,
                hAlign: HAlign.Center, vAlign: VAlign.Center, marginLeft: 28);
            _soldierIconsContainer.UpdateChildrenStates = true;
            AddChild(_soldierIconsContainer);
            RefreshSoldierIcons();
        }

        public void RefreshSoldierIcons()
        {
            _soldierIconsContainer.RemoveAllChildren();

            switch (SoldierIcons)
            {
                case 0:
                    break;
                case 1:
                    _soldierIconsContainer.AddChild(BuildSoldierIcon("Alliance.SoldierRed", 30, 35, -10, 2, 5));
                    break;
                case 2:
                    _soldierIconsContainer.AddChild(BuildSoldierIcon("Alliance.SoldierOrange", 30, 35, -10, 2, 2));
                    _soldierIconsContainer.AddChild(BuildSoldierIcon("Alliance.SoldierOrange", 30, 35, -20, -2, 2));
                    break;
                case 3:
                    _soldierIconsContainer.AddChild(BuildSoldierIcon("Alliance.SoldierGreen", 30, 35, -5, -2, 2));
                    _soldierIconsContainer.AddChild(BuildSoldierIcon("Alliance.SoldierGreen", 30, 35, -15, 2, 2));
                    _soldierIconsContainer.AddChild(BuildSoldierIcon("Alliance.SoldierGreen", 30, 35, -25, -2, 2));
                    break;
                case 4:
                    break;
                case 5:
                    break;
            }
        }

        private BrushWidget BuildSoldierIcon(string brushName, float width, float height, float xOffset, float yOffset, float marginBot = 0, string previousState = "")
        {
            BrushWidget soldierIcon = new BrushWidget(Context);
            soldierIcon.SetState(_soldierIconsContainer.CurrentState);
            soldierIcon.Init(width: SP.Fixed, height: SP.Fixed, suggestedWidth: width, suggestedHeight: height,
                hAlign: HAlign.Left, vAlign: VAlign.Center, marginLeft: 8, marginBottom: marginBot,
                brush: Context.GetBrush(brushName));
            soldierIcon.PositionXOffset = xOffset;
            soldierIcon.PositionYOffset = yOffset;
            return soldierIcon;
        }
    }
}
