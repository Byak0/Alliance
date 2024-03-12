using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using HAlign = TaleWorlds.GauntletUI.HorizontalAlignment;
using SP = TaleWorlds.GauntletUI.SizePolicy;
using VAlign = TaleWorlds.GauntletUI.VerticalAlignment;

namespace Alliance.Client.Extensions.TroopSpawner.Widgets
{
    /// <summary>
    /// Slider widget to select difficulty (from easy to bannerlord).
    /// </summary>
    public class DifficultySliderWidget : ListPanel
    {
        private int _difficultyValue;

        private NavigationTargetSwitcher _nts;
        private NavigationAutoScrollWidget _nasw;
        private SliderWidget _slider;
        private Widget _canvas;
        private Widget _filler;
        private Widget _fill;
        private Widget _frame;
        private ImageWidget _handle;
        private RichTextWidget _valueWidget;

        [Editor(false)]
        public int DifficultyValue
        {
            get
            {
                return _difficultyValue;
            }
            set
            {
                if (_difficultyValue != value)
                {
                    _difficultyValue = value;
                    _slider.ValueInt = value;
                    OnPropertyChanged(value, "DifficultyValue");
                }
            }
        }

        public DifficultySliderWidget(UIContext context) : base(context)
        {
            this.Init(width: SP.CoverChildren, height: SP.CoverChildren, hAlign: HAlign.Left, vAlign: VAlign.Center);

            _nts = new NavigationTargetSwitcher(context);
            AddChild(_nts);
            _nasw = new NavigationAutoScrollWidget(context);
            AddChild(_nasw);
            _slider = new SliderWidget(context);
            _canvas = new Widget(context);
            _slider.AddChild(_canvas);
            _filler = new Widget(context);
            _fill = new Widget(context);
            _filler.AddChild(_fill);
            _slider.AddChild(_filler);
            _frame = new Widget(context);
            _slider.AddChild(_frame);
            _handle = new ImageWidget(context);
            _slider.AddChild(_handle);
            AddChild(_slider);
            _valueWidget = new RichTextWidget(context);
            AddChild(_valueWidget);

            _nts.FromTarget = this;
            _nts.ToTarget = _slider;

            _nasw.TrackedWidget = _slider;
            _nasw.ScrollYOffset = 90;

            _slider.Init(width: SP.Fixed, height: SP.Fixed, 250, 42, vAlign: VAlign.Center);
            _slider.DoNotUpdateHandleSize = true;
            _slider.Filler = _filler;
            _slider.Handle = _handle;
            _slider.MinValueInt = 0;
            _slider.MaxValueInt = 4;
            _slider.ValueInt = DifficultyValue;
            _slider.IsDiscrete = true;
            _slider.DiscreteIncrementInterval = 1;
            _slider.UpdateChildrenStates = true;

            _canvas.Init(width: SP.Fixed, height: SP.Fixed, 265, 38, hAlign: HAlign.Center, vAlign: VAlign.Center);
            _canvas.Sprite = Context.SpriteData.GetSprite("SPGeneral\\SPOptions\\standart_slider_canvas");
            _canvas.DoNotAcceptEvents = true;

            _filler.Init(width: SP.Fixed, height: SP.Fixed, 255, 35, vAlign: VAlign.Center);
            _filler.Sprite = Context.SpriteData.GetSprite("SPGeneral\\SPOptions\\standart_slider_fill");
            _filler.ClipContents = true;
            _filler.DoNotAcceptEvents = true;

            _fill.Init(width: SP.Fixed, height: SP.Fixed, 255, 35, hAlign: HAlign.Left, vAlign: VAlign.Center);
            _fill.Sprite = Context.SpriteData.GetSprite("SPGeneral\\SPOptions\\standart_slider_fill");

            _frame.Init(width: SP.Fixed, height: SP.Fixed, 300, 65, hAlign: HAlign.Center, vAlign: VAlign.Center);
            _frame.Sprite = Context.SpriteData.GetSprite("SPGeneral\\SPOptions\\standart_slider_frame");
            _frame.DoNotAcceptEvents = true;

            _handle.Init(width: SP.Fixed, height: SP.Fixed, 14, 38, hAlign: HAlign.Left, vAlign: VAlign.Center);
            _handle.Brush = Context.GetBrush("SPOptions.Slider.Handle");

            _valueWidget.Init(width: SP.CoverChildren, height: SP.Fixed, 60, 30, marginLeft: 30, marginTop: 15);
            _valueWidget.Brush = Context.GetBrush("SPOptions.Slider.Value.Text");
            _slider.UpdateValueOnRelease = true;
            _slider.intPropertyChanged += UpdateDifficulty;
            UpdateDifficulty(null, "ValueInt", DifficultyValue);
        }

        private void UpdateDifficulty(PropertyOwnerObject obj, string propertyName, int value)
        {
            DifficultyValue = value;
            switch (value)
            {
                case 0:
                    _valueWidget.Text = "Easy"; break;
                case 1:
                    _valueWidget.Text = "Normal"; break;
                case 2:
                    _valueWidget.Text = "Hard"; break;
                case 3:
                    _valueWidget.Text = "Very hard"; break;
                case 4:
                    _valueWidget.Text = "Bannerlord"; break;
            }
        }
    }
}
