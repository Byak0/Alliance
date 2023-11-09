using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.ExtendedCharacter.Extension;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromClient;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Common.GameModes.PvC.Behaviors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using HAlign = TaleWorlds.GauntletUI.HorizontalAlignment;
using SP = TaleWorlds.GauntletUI.SizePolicy;
using VAlign = TaleWorlds.GauntletUI.VerticalAlignment;

namespace Alliance.Client.Extensions.TroopSpawner.Widgets
{
    /*
     * Main class containing all necessary widgets for the SpawnTroop menu
     * 
     * It is composed as follow :
     * 
     * > Widget mainList - Main container
     *     > Widget leftList - Left container        
     *         > PvCTroopListWidget troopList - Troop List
     *     > Widget rightList - Right container
     *         > Widget title - Title
     *         > Widget rightCenterList - Right center container
     *             > CharacterTableauWidget troopModel - 3D troop preview
     *             > BasicContainer buttonsContainer - Troop count buttons
     *                 > List<ScrollableButtonWidget> - Troop count buttons
     *         > Widget rigtBottomList - Right bottom container for formations
     *             > ListPanel formationList - List of formations
     *     > Widget totalTroopCost - Total troop cost
     *     > ButtonWidget spawnButton - Recruit button
     *     > Widget totalGold - Total gold
     *
     */

    public class SpawnTroopsWidget : BrushWidget
    {
        Widget mainList;
        Widget leftList;
        PvCTroopListWidget troopList;
        Widget rightList;
        Widget title;
        Widget rightCenterList;
        CharacterTableauWidget troopModel;
        BasicContainer buttonsContainer;
        List<ScrollableButtonWidget> troopCountButtons;
        Widget rightBottomList;
        ListPanel formationList;
        Widget totalTroopCost;
        TextWidget totalTroopCostText;
        ButtonWidget spawnButton;
        Widget totalGold;
        TextWidget totalGoldText;

        // Native VM required for 3D troop preview
        private CharacterViewModel _unitCharacterVM;

        private int _nbClick;
        private float _lastClick;

        public SpawnTroopsWidget(UIContext context) : base(context)
        {
            _unitCharacterVM = new CharacterViewModel(CharacterViewModel.StanceTypes.CelebrateVictory);

            // Get PvCRepresentative for gold and refresh when needed
            if (Config.Instance.UseTroopCost && PvCRepresentative.Main != null) PvCRepresentative.Main.OnGoldUpdated += RefreshGold;

            // Root widget
            this.Init(width: SP.Fixed, height: SP.Fixed, 1500, 900, hAlign: HAlign.Center, vAlign: VAlign.Center, brush: Context.GetBrush("Alliance.MainFrame"));
            AddChild(MainList());

            // Listen to changes on SpawnTroopsModel and refresh what's needed
            SpawnTroopsModel.Instance.OnTroopSelected += OnTroopSelected;
            SpawnTroopsModel.Instance.OnTroopSpawned += OnTroopSpawn;
            SpawnTroopsModel.Instance.OnFormationUpdated += OnFormationUpdate;

            RefreshCost();
            RefreshRecruitButton();
        }

        // Main widget englobing everything
        private Widget MainList()
        {
            mainList = new Widget(Context);
            mainList.Init(width: SP.StretchToParent, height: SP.CoverChildren);
            mainList.AddChild(LeftList());
            mainList.AddChild(RightList());
            if (Config.Instance.UseTroopCost && PvCRepresentative.Main != null) mainList.AddChild(TotalTroopCost());
            mainList.AddChild(SpawnButton());
            if (Config.Instance.UseTroopCost && PvCRepresentative.Main != null) mainList.AddChild(TotalGold());
            return mainList;
        }

        // Widget containing all elements on the left
        private Widget LeftList()
        {
            leftList = new Widget(Context);
            leftList.Init(width: SP.Fixed, height: SP.Fixed, 500, 900, HAlign.Left);

            CultureFilterWidget cultureFilter = new CultureFilterWidget(Context);
            cultureFilter.MarginTop = -1;

            leftList.AddChild(cultureFilter);
            leftList.AddChild(TroopList());

            PvCSliderWidget difficultySlider = new PvCSliderWidget(Context, 0, 4, 1, OnSliderChange);
            difficultySlider.Init(width: SP.Fixed, height: SP.Fixed, 300, 60, marginBottom: 240, hAlign: HAlign.Center, vAlign: VAlign.Bottom);
            leftList.AddChild(difficultySlider);

            return leftList;
        }

        private void OnSliderChange(PropertyOwnerObject obj, string propertyName, float value)
        {
            switch (value)
            {
                case 0:
                    SpawnTroopsModel.Instance.Difficulty = 0.5f; break;
                case 1:
                    SpawnTroopsModel.Instance.Difficulty = 1f; break;
                case 2:
                    SpawnTroopsModel.Instance.Difficulty = 1.5f; break;
                case 3:
                    SpawnTroopsModel.Instance.Difficulty = 2f; break;
                case 4:
                    SpawnTroopsModel.Instance.Difficulty = 2.5f; break;
            }
            RefreshCost();
        }

        // Custom Widget handling troop list and filters
        private PvCTroopListWidget TroopList()
        {
            troopList = new PvCTroopListWidget(Context);
            troopList.Init(width: SP.Fixed, height: SP.Fixed, 480, 700, marginTop: 80, marginLeft: 12, vAlign: VAlign.Top);
            return troopList;
        }

        // Widget containing all elements on the right
        private Widget RightList()
        {
            rightList = new Widget(Context);
            rightList.Init(width: SP.Fixed, height: SP.Fixed, 1000, 900, HAlign.Right);
            rightList.AddChild(Title());
            rightList.AddChild(RightCenterList());
            rightList.AddChild(RightBottomList());
            return rightList;
        }

        // Menu Title
        private Widget Title()
        {
            title = new Widget(Context);
            title.Init(width: SP.StretchToParent, height: SP.Fixed, suggestedHeight: 100, vAlign: VAlign.Top);
            TextWidget titleTxt = new TextWidget(Context);
            titleTxt.Init(width: SP.StretchToParent, height: SP.StretchToParent, hAlign: HAlign.Center, vAlign: VAlign.Center,
                          brush: Context.GetBrush("Popup.Button.Text"));
            titleTxt.Text = "";
            title.AddChild(titleTxt);
            return title;
        }

        // Widget containing elements on the center right (3D model + troop count buttons)
        private Widget RightCenterList()
        {
            rightCenterList = new Widget(Context);
            rightCenterList.Init(width: SP.StretchToParent, height: SP.Fixed, suggestedHeight: 450, vAlign: VAlign.Top, marginTop: 100);
            rightCenterList.AddChild(TroopModel());
            rightCenterList.AddChild(TroopCountButtons());
            return rightCenterList;
        }

        // 3D troop preview
        private CharacterTableauWidget TroopModel()
        {
            troopModel = new CharacterTableauWidget(Context);
            troopModel.Init(width: SP.Fixed, height: SP.Fixed, 500, 400, vAlign: VAlign.Top, marginLeft: 100);
            troopModel.EventFire += OnTroopModelClick;
            // Show the selected troop from SpawnTroopsModel
            RefreshTroopModel(SpawnTroopsModel.Instance.SelectedTroop);
            return troopModel;
        }

        // Make the 3D troop preview do some moves
        private void OnTroopModelClick(Widget widget, string eventName, object[] args)
        {
            if (eventName == "MouseMove" && Input.IsKeyReleased(InputKey.LeftMouseButton))
            {
                _nbClick++;

                Vec3 position = Mission.Current.GetCameraFrame().origin + Mission.Current.GetCameraFrame().rotation.u;

                if (Math.Round(Mission.Current.CurrentTime, 0) >= _lastClick + 20)
                {
                    _nbClick = 0;
                }
                _lastClick = (float)Math.Round(Mission.Current.CurrentTime, 0);
                if (_nbClick == 150)
                {
                    troopModel.StanceIndex = 0;
                    troopModel.IdleAction = "act_dance_norse";
                    string musicTrack = "event:/music/musicians/" + SpawnTroopsModel.Instance.SelectedFaction.Name.ToString().ToLower() + "/0" + MBRandom.RandomInt(1, 5);
                    SoundEvent soundEvent = SoundEvent.CreateEvent(SoundEvent.GetEventIdFromString(musicTrack), Mission.Current.Scene);
                    soundEvent.SetPosition(position);
                    soundEvent.Play();
                    DelayedStop(soundEvent, 60000);
                    _nbClick = 0;
                }
                else if (_nbClick == 100)
                {
                    troopModel.StanceIndex = 0;
                    troopModel.IdleAction = "act_death_by_arrow_neck1_cont";
                    string gender = SpawnTroopsModel.Instance.SelectedTroop.IsFemale ? "female" : "male";
                    MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/voice/combat/" + gender + "/01/death"), position);
                }
                else if (_nbClick == 70)
                {
                    troopModel.StanceIndex = 0;
                    troopModel.IdleAction = "act_main_story_conspirator_kneel_down_1";
                    string musicTrack = "event:/ui/cutscenes/story_pledge_support";
                    SoundEvent soundEvent = SoundEvent.CreateEvent(SoundEvent.GetEventIdFromString(musicTrack), Mission.Current.Scene);
                    soundEvent.SetPosition(position);
                    soundEvent.Play();
                    DelayedStop(soundEvent, 30000);
                }
                else if (_nbClick < 50)
                {
                    if (_nbClick % 10 == 0)
                    {
                        int randomEvent = MBRandom.RandomInt(1, 4);
                        string gender = SpawnTroopsModel.Instance.SelectedTroop.IsFemale ? "female" : "male";
                        string voice = "";
                        string action = "";
                        switch (randomEvent)
                        {
                            case 1: action = "act_cheering_low_08"; voice = "yell"; break;
                            case 2: action = "act_bully"; voice = "grunt"; break;
                            case 3: action = "act_cheering_high_04"; voice = "yell"; break;
                        }
                        troopModel.StanceIndex = 0;
                        troopModel.IdleAction = action;
                        MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/voice/combat/" + gender + "/0" + randomEvent + "/" + voice), position);
                    }
                }
            }
        }

        // Stop the sound after set timer
        private async void DelayedStop(SoundEvent eventRef, int milliseconds)
        {
            await Task.Delay(milliseconds);
            eventRef.Stop();
        }

        // Container for the troop count buttons
        private BasicContainer TroopCountButtons()
        {
            buttonsContainer = new BasicContainer(Context);
            buttonsContainer.Init(width: SP.Fixed, height: SP.Fixed, 600, 100,
                                  vAlign: VAlign.Bottom, marginBottom: 10, marginLeft: 75);

            troopCountButtons = new List<ScrollableButtonWidget>();
            float marginLeft = 10;
            int[] troopCount = new int[5] { 1, 10, 50, 100, SpawnTroopsModel.Instance.CustomTroopCount };
            for (int i = 0; i < troopCount.Length; i++)
            {
                // Last button needs to be editable
                if (i == troopCount.Length - 1) troopCountButtons.Add(new ScrollableButtonWidget(Context, true));
                // Others do not
                else troopCountButtons.Add(new ScrollableButtonWidget(Context));
                // Init all buttons
                troopCountButtons[i].InitScrollableButton(brush: Context.GetBrush("FaceGen.Extension.Button"),
                    id: i, troopCount: troopCount[i], soldierIcons: TroopCountToSoldierIcons(troopCount[i]));
                troopCountButtons[i].MarginLeft = marginLeft;
                troopCountButtons[i].EventFire += OnTroopCountSelected;
                buttonsContainer.AddChild(troopCountButtons[i]);
                marginLeft += 110;
            }
            // Make third & fourth buttons text slightly on the right
            troopCountButtons[2].IntTextWidget.MarginLeft = 28;
            troopCountButtons[3].IntTextWidget.MarginLeft = 28;
            // Make last button slightly bigger than default
            troopCountButtons[troopCount.Length - 1].SuggestedWidth = 120;
            // Not proud of this one. It's just to position the last text correctly. Very roundabout way.
            troopCountButtons[troopCount.Length - 1].IntTextWidget.MarginLeft = 0;
            troopCountButtons[troopCount.Length - 1].IntTextWidget.WidthSizePolicy = SP.Fixed;
            troopCountButtons[troopCount.Length - 1].IntTextWidget.SuggestedWidth = 140;
            troopCountButtons[troopCount.Length - 1].IntTextWidget.HorizontalAlignment = HAlign.Left;

            // Load last selected button
            troopCountButtons[SpawnTroopsModel.Instance.TroopCountButtonSelected].IsSelected = true;

            return buttonsContainer;
        }

        // Widget containing formations on the bottom right
        private Widget RightBottomList()
        {
            rightBottomList = new Widget(Context);
            rightBottomList.Init(width: SP.StretchToParent, height: SP.Fixed, suggestedHeight: 200, vAlign: VAlign.Bottom, marginBottom: 150);
            rightBottomList.AddChild(FormationList());
            return rightBottomList;
        }

        // List of formations
        private ListPanel FormationList()
        {
            formationList = new ListPanel(Context);
            formationList.Init(width: SP.StretchToParent, height: SP.Fixed, suggestedHeight: 200);
            for (int i = 0; i < 8; i++)
            {
                OrderTroopItemVM formationVM = new OrderTroopItemVM(Mission.Current.PlayerTeam.GetFormation((FormationClass)i), new Action<OrderTroopItemVM>(ExecuteSelectTransferTroop), new Func<Formation, int>(GetFormationMorale));
                formationVM.IsSelected = false;
                //BasicCharacterObject captain = Mission.Current.PlayerTeam.GetFormation((FormationClass)i).PlayerOwner?.Character;
                //if(captain != null) formationVM.CommanderImageIdentifier = new ImageIdentifierVM(CharacterCode.CreateFrom(captain));

                float width = (float)(Context.GetBrush("Order.Troop.Background").GetLayer("Default").Sprite.Width * 0.8);
                float height = (float)(Context.GetBrush("Order.Troop.Background").GetLayer("Default").Sprite.Height * 0.8);

                FormationCardWidget formationCard = new FormationCardWidget(Context);
                GeneratedWidgetData generatedWidgetData = new GeneratedWidgetData(formationCard);
                generatedWidgetData.Data = formationVM;
                formationCard.AddComponent(generatedWidgetData);
                formationCard.CreateWidgets();
                formationCard.SetIds();
                formationCard.SetAttributes();
                formationCard.SetDataSource(formationVM);
                formationCard.MarginTop = 0;
                formationCard.IsSelectable = true;
                // Force "Default" state otherwise empty formations will appear Disabled / grayed out
                formationCard.SetState("Default");

                TextWidget commander = new TextWidget(Context);
                commander.Init(width: SP.StretchToParent, height: SP.Fixed, suggestedHeight: 30);
                commander.Text = FormationControlModel.Instance.GetControllerOfFormation((FormationClass)i, Mission.Current.PlayerTeam)?.Name ?? "";

                // Formation cards are not clickable natively, so we enclose them in a button
                ToggleStateButtonWidget buttonFormationWidget = new ToggleStateButtonWidget(Context);
                buttonFormationWidget.Init(SP.Fixed, SP.Fixed, width, height);
                buttonFormationWidget.DoNotPassEventsToChildren = true;
                buttonFormationWidget.AllowSwitchOff = false;
                buttonFormationWidget.EventFire += OnFormationSelected;
                buttonFormationWidget.AddChild(formationCard);
                buttonFormationWidget.AddChild(commander);
                formationList.AddChild(buttonFormationWidget);
            }
            // Load last selected formation
            (formationList.GetChild(SpawnTroopsModel.Instance.FormationSelected) as ToggleStateButtonWidget).IsSelected = true;
            (formationList.GetChild(SpawnTroopsModel.Instance.FormationSelected).GetChild(0) as FormationCardWidget).IsSelected = true;
            (formationList.GetChild(SpawnTroopsModel.Instance.FormationSelected).GetChild(0) as FormationCardWidget).SetState("Selected");
            return formationList;
        }

        private Widget TotalTroopCost()
        {
            totalTroopCost = new Widget(Context);
            totalTroopCost.Init(width: SP.Fixed, height: SP.Fixed, suggestedWidth: 200, suggestedHeight: 80,
                                hAlign: HAlign.Left, vAlign: VAlign.Bottom, marginBottom: 20, marginLeft: 140);
            ListPanel lp = new ListPanel(Context);
            lp.Init(SP.CoverChildren, SP.CoverChildren, hAlign: HAlign.Right, vAlign: VAlign.Center, marginRight: 45);

            totalTroopCostText = new TextWidget(Context);
            totalTroopCostText.Init(width: SP.CoverChildren, height: SP.CoverChildren, vAlign: VAlign.Center, marginRight: 5, brush: Context.GetBrush("MPHUD.GoldAmount.Text"));
            totalTroopCostText.PositionYOffset = 3;
            totalTroopCostText.ClipContents = false;
            totalTroopCostText.Brush.FontSize = 40;

            Widget goldIcon = new Widget(Context);
            goldIcon.Init(width: SP.Fixed, height: SP.Fixed, suggestedWidth: 33, suggestedHeight: 30,
                hAlign: HAlign.Right, vAlign: VAlign.Center, marginTop: 8);
            goldIcon.Sprite = Context.SpriteData.GetSprite("General\\Mission\\PersonalKillfeed\\bracelet_icon_shadow");

            lp.AddChild(totalTroopCostText);
            lp.AddChild(goldIcon);

            totalTroopCost.AddChild(lp);

            return totalTroopCost;
        }

        private ButtonWidget SpawnButton()
        {
            spawnButton = new ButtonWidget(Context);
            spawnButton.Init(width: SP.Fixed, height: SP.Fixed, suggestedWidth: 240, suggestedHeight: 80,
                                hAlign: HAlign.Center, vAlign: VAlign.Bottom, marginBottom: 20, brush: Context.GetBrush("ButtonBrush1"));
            spawnButton.DoNotPassEventsToChildren = true;
            spawnButton.EventFire += OnSpawnClick;

            TextWidget txt = new TextWidget(Context);
            txt.Init(SP.StretchToParent, SP.StretchToParent, hAlign: HAlign.Center, vAlign: VAlign.Center, brush: Context.GetBrush("Popup.Button.Text"));
            txt.Brush.FontSize = 30;
            txt.Text = "Recruit !";

            spawnButton.AddChild(txt);
            return spawnButton;
        }

        private Widget TotalGold()
        {
            totalGold = new Widget(Context);
            totalGold.Init(width: SP.Fixed, height: SP.Fixed, suggestedWidth: 200, suggestedHeight: 80,
                                hAlign: HAlign.Right, vAlign: VAlign.Bottom, marginBottom: 20, marginRight: 190);
            ListPanel lp = new ListPanel(Context);
            lp.Init(SP.CoverChildren, SP.CoverChildren, hAlign: HAlign.Right, vAlign: VAlign.Center);

            totalGoldText = new TextWidget(Context);
            totalGoldText.Init(width: SP.CoverChildren, height: SP.CoverChildren, vAlign: VAlign.Center, marginRight: 5, brush: Context.GetBrush("MPHUD.GoldAmount.Text"));
            totalGoldText.PositionYOffset = 3;
            totalGoldText.ClipContents = false;
            totalGoldText.Brush.FontSize = 40;
            totalGoldText.IntText = PvCRepresentative.Main.Gold;

            Widget goldIcon = new Widget(Context);
            goldIcon.Init(width: SP.Fixed, height: SP.Fixed, suggestedWidth: 33, suggestedHeight: 30,
                hAlign: HAlign.Right, vAlign: VAlign.Center, marginTop: 8);
            goldIcon.Sprite = Context.SpriteData.GetSprite("General\\Mission\\PersonalKillfeed\\bracelet_icon_shadow");

            lp.AddChild(totalGoldText);
            lp.AddChild(goldIcon);

            totalGold.AddChild(lp);

            return totalGold;
        }

        private void RefreshGold()
        {
            totalGoldText.IntText = PvCRepresentative.Main.Gold;
            RefreshFormationCards();
        }

        private void OnFormationUpdate(object sender, EventArgs e)
        {
            RefreshFormationCards();
        }

        private void RefreshFormationCards()
        {
            // TODO : find a way to refresh without rebuilding everything ?...
            // Force refresh VM & rebuild cards
            formationList.RemoveAllChildren();

            for (int i = 0; i < 8; i++)
            {
                OrderTroopItemVM formationVM = new OrderTroopItemVM(Mission.Current.PlayerTeam.GetFormation((FormationClass)i), new Action<OrderTroopItemVM>(ExecuteSelectTransferTroop), new Func<Formation, int>(GetFormationMorale));
                formationVM.IsSelected = false;
                //BasicCharacterObject captain = Mission.Current.PlayerTeam.GetFormation((FormationClass)i).PlayerOwner?.Character;
                //if (captain != null) formationVM.CommanderImageIdentifier = new ImageIdentifierVM(CharacterCode.CreateFrom(captain));

                float width = (float)(Context.GetBrush("Order.Troop.Background").GetLayer("Default").Sprite.Width * 0.8);
                float height = (float)(Context.GetBrush("Order.Troop.Background").GetLayer("Default").Sprite.Height * 0.8);

                FormationCardWidget formationCard = new FormationCardWidget(Context);
                GeneratedWidgetData generatedWidgetData = new GeneratedWidgetData(formationCard);
                generatedWidgetData.Data = formationVM;
                formationCard.AddComponent(generatedWidgetData);
                formationCard.CreateWidgets();
                formationCard.SetIds();
                formationCard.SetAttributes();
                formationCard.SetDataSource(formationVM);
                formationCard.MarginTop = 0;
                formationCard.IsSelectable = true;
                // Force "Default" state otherwise empty formations will appear Disabled / grayed out
                formationCard.SetState("Default");

                TextWidget commander = new TextWidget(Context);
                commander.Init(width: SP.StretchToParent, height: SP.Fixed, suggestedHeight: 30);
                commander.Text = FormationControlModel.Instance.GetControllerOfFormation((FormationClass)i, Mission.Current.PlayerTeam)?.Name ?? "";

                // Formation cards are not clickable natively, so we enclose them in a button
                ToggleStateButtonWidget buttonFormationWidget = new ToggleStateButtonWidget(Context);
                buttonFormationWidget.Init(SP.Fixed, SP.Fixed, width, height);
                buttonFormationWidget.DoNotPassEventsToChildren = true;
                buttonFormationWidget.AllowSwitchOff = false;
                buttonFormationWidget.EventFire += OnFormationSelected;
                buttonFormationWidget.AddChild(formationCard);
                buttonFormationWidget.AddChild(commander);
                formationList.AddChild(buttonFormationWidget);
            }
            // Load last selected formation
            (formationList.GetChild(SpawnTroopsModel.Instance.FormationSelected) as ToggleStateButtonWidget).IsSelected = true;
            (formationList.GetChild(SpawnTroopsModel.Instance.FormationSelected).GetChild(0) as FormationCardWidget).IsSelected = true;
            (formationList.GetChild(SpawnTroopsModel.Instance.FormationSelected).GetChild(0) as FormationCardWidget).SetState("Selected");
        }

        private void RefreshCost()
        {
            if (!Config.Instance.UseTroopCost || PvCRepresentative.Main == null) return;
            if (totalTroopCostText == null) mainList.AddChild(TotalTroopCost());

            int newCost = SpawnTroopsModel.Instance.TroopCount * SpawnHelper.GetTroopCost(SpawnTroopsModel.Instance.SelectedTroop, SpawnTroopsModel.Instance.Difficulty);
            totalTroopCostText.IntText = -newCost;
            if (newCost > PvCRepresentative.Main.Gold)
            {
                totalTroopCostText.Brush.FontColor = Colors.Red;
            }
            else
            {
                totalTroopCostText.Brush.FontColor = Colors.White;
            }
        }

        private void RefreshRecruitButton()
        {
            if (PvCRepresentative.Main == null) return;

            bool troopOverLimit = Config.Instance.UseTroopLimit && SpawnTroopsModel.Instance.SelectedTroop.GetExtendedCharacterObject().TroopLeft <= 0;
            bool troopTooCostly = Config.Instance.UseTroopCost && SpawnTroopsModel.Instance.TroopCount * SpawnHelper.GetTroopCost(SpawnTroopsModel.Instance.SelectedTroop, SpawnTroopsModel.Instance.Difficulty) > PvCRepresentative.Main.Gold;
            spawnButton.IsDisabled = !GameNetwork.MyPeer.IsCommander() && (troopOverLimit || troopTooCostly);
        }

        private void RefreshTroopModel(BasicCharacterObject bco = null)
        {
            _unitCharacterVM.FillFrom(bco);
            troopModel.IsFemale = _unitCharacterVM.IsFemale;
            troopModel.BodyProperties = _unitCharacterVM.BodyProperties;
            troopModel.EquipmentCode = _unitCharacterVM.EquipmentCode;
            troopModel.CharStringId = _unitCharacterVM.CharStringId;
            troopModel.StanceIndex = _unitCharacterVM.StanceIndex;
            troopModel.BannerCodeText = _unitCharacterVM.BannerCodeText;
            troopModel.MountCreationKey = _unitCharacterVM.MountCreationKey;
            troopModel.ArmorColor1 = _unitCharacterVM.ArmorColor1;
            troopModel.ArmorColor2 = _unitCharacterVM.ArmorColor2;
            troopModel.Race = _unitCharacterVM.Race;
        }

        // Event when clicking on the spawn button
        private void OnSpawnClick(Widget widget, string eventName, object[] args)
        {
            ButtonWidget button = (ButtonWidget)widget;
            if (eventName == "Click")
            {
                // Get either camera or agent position 
                MatrixFrame _spawnFrame = Mission.Current.GetCameraFrame();
                if (Agent.Main?.Position != null) _spawnFrame = new MatrixFrame(Mat3.Identity, Agent.Main.Position);

                // Play a sound because why not
                Vec3 position = _spawnFrame.origin + _spawnFrame.rotation.u;
                MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/battle_winning"), position);

                // Send a request to spawn to the server
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new RequestSpawnTroop(
                    _spawnFrame,
                    false,
                    SpawnTroopsModel.Instance.SelectedTroop.StringId.ToString(),
                    SpawnTroopsModel.Instance.FormationSelected,
                    SpawnTroopsModel.Instance.TroopCount,
                    SpawnTroopsModel.Instance.Difficulty));
                GameNetwork.EndModuleEventAsClient();
            }
        }

        // Event after troop spawn
        private void OnTroopSpawn(object sender, TroopSpawnedEventArgs e)
        {
            RefreshRecruitButton();
        }

        // Event when clicking on a formation card
        private void OnFormationSelected(Widget widget, string eventName, object[] args)
        {
            ButtonWidget button = (ButtonWidget)widget;
            if (eventName == "Click")
            {
                // Deselect previous formation
                FormationCardWidget previousFormation = formationList.GetChild(SpawnTroopsModel.Instance.FormationSelected).GetChild(0) as FormationCardWidget;
                previousFormation.IsSelected = false;
                previousFormation.SetState("Default");
                // Select new one & update model
                SpawnTroopsModel.Instance.FormationSelected = formationList.GetChildIndex(button);
                (button.GetChild(0) as FormationCardWidget).IsSelected = true;
                (button.GetChild(0) as FormationCardWidget).SetState("Selected");
            }
        }

        // Event when clicking on a count button
        private void OnTroopCountSelected(Widget widget, string eventName, object[] args)
        {
            ScrollableButtonWidget button = (ScrollableButtonWidget)widget;
            if (eventName == "OnTroopCountSelected")
            {
                // If update comes from custom button, store additional variable and refresh soldier icons
                if (button.EditableText)
                {
                    SpawnTroopsModel.Instance.CustomTroopCount = button.IntTextWidget.IntText;
                    button.SoldierIcons = TroopCountToSoldierIcons(button.IntTextWidget.IntText);
                }
                SpawnTroopsModel.Instance.TroopCount = button.IntTextWidget.IntText;
                SpawnTroopsModel.Instance.TroopCountButtonSelected = button.ButtonId;
                RefreshCost();
                RefreshRecruitButton();
            }
        }

        private void OnTroopSelected(object sender, EventArgs e)
        {
            RefreshTroopModel(SpawnTroopsModel.Instance.SelectedTroop);
            RefreshCost();
            RefreshRecruitButton();
        }

        private int GetFormationMorale(Formation formation)
        {
            return (int)MissionGameModels.Current.BattleMoraleModel.GetAverageMorale(formation);
        }

        private void ExecuteSelectTransferTroop(OrderTroopItemVM obj)
        {
            // Do nothing.
        }

        // Returns the number of icons wanted for the specified troop count on the TroopCountButton
        private int TroopCountToSoldierIcons(int troopCount)
        {
            int soldierIcons;
            if (troopCount >= 100) soldierIcons = 5;
            else if (troopCount >= 50) soldierIcons = 3;
            else if (troopCount >= 10) soldierIcons = 2;
            else if (troopCount >= 1) soldierIcons = 1;
            else soldierIcons = 0;
            return soldierIcons;
        }
    }
}