using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using HAlign = TaleWorlds.GauntletUI.HorizontalAlignment;
using SP = TaleWorlds.GauntletUI.SizePolicy;
using VAlign = TaleWorlds.GauntletUI.VerticalAlignment;

namespace Alliance.Client.Extensions.TroopSpawner.Widgets
{
    public class PvCSliderWidget : ListPanel
    {
        private NavigationTargetSwitcher nts;
        private NavigationAutoScrollWidget nasw;
        private SliderWidget slider;
        private Widget canvas;
        private Widget filler;
        private Widget fill;
        private Widget frame;
        private ImageWidget handle;
        private RichTextWidget valueWidget;

        public PvCSliderWidget(UIContext context, int min, int max, int value, Action<PropertyOwnerObject, string, float> onPropertyChanged) : base(context)
        {
            this.Init(width: SP.CoverChildren, height: SP.CoverChildren, hAlign: HAlign.Left, vAlign: VAlign.Center);

            nts = new NavigationTargetSwitcher(context);
            AddChild(nts);
            nasw = new NavigationAutoScrollWidget(context);
            AddChild(nasw);
            slider = new SliderWidget(context);
            canvas = new Widget(context);
            slider.AddChild(canvas);
            filler = new Widget(context);
            fill = new Widget(context);
            filler.AddChild(fill);
            slider.AddChild(filler);
            frame = new Widget(context);
            slider.AddChild(frame);
            handle = new ImageWidget(context);
            slider.AddChild(handle);
            AddChild(slider);
            valueWidget = new RichTextWidget(context);
            AddChild(valueWidget);

            nts.FromTarget = this;
            nts.ToTarget = slider;

            nasw.TrackedWidget = slider;
            nasw.ScrollYOffset = 90;

            slider.Init(width: SP.Fixed, height: SP.Fixed, 250, 42, vAlign: VAlign.Center);
            slider.DoNotUpdateHandleSize = true;
            slider.Filler = filler;
            slider.Handle = handle;
            slider.MinValueInt = min;
            slider.MaxValueInt = max;
            slider.ValueInt = value;
            slider.IsDiscrete = true;
            slider.DiscreteIncrementInterval = 1;
            slider.UpdateChildrenStates = true;

            canvas.Init(width: SP.Fixed, height: SP.Fixed, 265, 38, hAlign: HAlign.Center, vAlign: VAlign.Center);
            canvas.Sprite = Context.SpriteData.GetSprite("SPGeneral\\SPOptions\\standart_slider_canvas");
            canvas.DoNotAcceptEvents = true;

            filler.Init(width: SP.Fixed, height: SP.Fixed, 255, 35, vAlign: VAlign.Center);
            filler.Sprite = Context.SpriteData.GetSprite("SPGeneral\\SPOptions\\standart_slider_fill");
            filler.ClipContents = true;
            filler.DoNotAcceptEvents = true;

            fill.Init(width: SP.Fixed, height: SP.Fixed, 255, 35, hAlign: HAlign.Left, vAlign: VAlign.Center);
            fill.Sprite = Context.SpriteData.GetSprite("SPGeneral\\SPOptions\\standart_slider_fill");

            frame.Init(width: SP.Fixed, height: SP.Fixed, 300, 65, hAlign: HAlign.Center, vAlign: VAlign.Center);
            frame.Sprite = Context.SpriteData.GetSprite("SPGeneral\\SPOptions\\standart_slider_frame");
            frame.DoNotAcceptEvents = true;

            handle.Init(width: SP.Fixed, height: SP.Fixed, 14, 38, hAlign: HAlign.Left, vAlign: VAlign.Center);
            handle.Brush = Context.GetBrush("SPOptions.Slider.Handle");

            valueWidget.Init(width: SP.CoverChildren, height: SP.Fixed, 60, 30, marginLeft: 30, marginTop: 15);
            valueWidget.Brush = Context.GetBrush("SPOptions.Slider.Value.Text");
            slider.UpdateValueOnRelease = true;
            slider.floatPropertyChanged += UpdateTextWidget;
            slider.floatPropertyChanged += onPropertyChanged;
            UpdateTextWidget(null, "ValueInt", 1);
        }

        private void UpdateTextWidget(PropertyOwnerObject obj, string propertyName, float value)
        {
            switch (value)
            {
                case 0:
                    valueWidget.Text = "Easy"; break;
                case 1:
                    valueWidget.Text = "Normal"; break;
                case 2:
                    valueWidget.Text = "Hard"; break;
                case 3:
                    valueWidget.Text = "Very hard"; break;
                case 4:
                    valueWidget.Text = "Bannerlord"; break;
            }
        }
    }
}
