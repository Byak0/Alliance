using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace Alliance.Client.Extensions.BrushPicker.Widgets
{
    public class BrushPickerWidget : BrushWidget
    {
        public ScrollablePanel ScrollablePanel;
        public ScrollbarWidget ScrollbarWidget;
        public Widget ScrollbarContainer;
        public Widget ClipRect;
        public Widget InnerPanel;
        public List<ButtonWidget> BrushWidgets;

        public BrushPickerWidget(UIContext context) : base(context)
        {
            BrushPicker.Instance.LoadBrush(context);
            BrushPicker.Instance.Serialize();

            WidthSizePolicy = SizePolicy.Fixed;
            HeightSizePolicy = SizePolicy.Fixed;
            SuggestedWidth = 1800;
            SuggestedHeight = 900;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
            Brush = Context.GetBrush("Frame1Brush");

            ScrollablePanel = new ScrollablePanel(context);
            ScrollablePanel.HeightSizePolicy = SizePolicy.StretchToParent;
            ScrollablePanel.WidthSizePolicy = SizePolicy.StretchToParent;
            ScrollablePanel.AutoHideScrollBars = true;
            AddChild(ScrollablePanel);

            ClipRect = new Widget(Context);
            ClipRect.WidthSizePolicy = SizePolicy.StretchToParent;
            ClipRect.HeightSizePolicy = SizePolicy.StretchToParent;
            ClipRect.ClipContents = true;

            ScrollablePanel.AddChild(ClipRect);
            ScrollablePanel.ClipRect = ClipRect;

            ScrollbarWidget = new ScrollbarWidget(Context);
            ScrollbarWidget.WidthSizePolicy = SizePolicy.Fixed;
            ScrollbarWidget.SuggestedWidth = 8;
            ScrollbarWidget.HeightSizePolicy = SizePolicy.StretchToParent;
            ScrollbarWidget.HorizontalAlignment = HorizontalAlignment.Left;
            ScrollbarWidget.UpdateChildrenStates = true;
            ScrollbarWidget.MarginTop = 6;
            ScrollbarWidget.MarginBottom = 15;
            ScrollbarWidget.MarginLeft = -15;
            ScrollbarWidget.AlignmentAxis = AlignmentAxis.Vertical;
            ScrollbarWidget.MaxValue = 100;
            ScrollbarWidget.MinValue = 0;

            Widget backgroundColorSprite = new Widget(Context);
            backgroundColorSprite.WidthSizePolicy = SizePolicy.Fixed;
            backgroundColorSprite.HeightSizePolicy = SizePolicy.StretchToParent;
            backgroundColorSprite.SuggestedWidth = 4;
            backgroundColorSprite.HorizontalAlignment = HorizontalAlignment.Center;
            backgroundColorSprite.Sprite = Context.SpriteData.GetSprite("lobby_slider_bed_9");
            backgroundColorSprite.AlphaFactor = 0.2f;
            ScrollbarWidget.AddChild(backgroundColorSprite);

            ImageWidget handle = new ImageWidget(Context);
            handle.WidthSizePolicy = SizePolicy.Fixed;
            handle.MinHeight = 50;
            handle.SuggestedWidth = 8;
            handle.HorizontalAlignment = HorizontalAlignment.Center;
            handle.Brush = Context.GetBrush("MPLobby.CustomServer.ScrollHandle");
            ScrollbarWidget.AddChild(handle);

            ScrollbarWidget.Handle = handle;

            AddChild(ScrollbarWidget);
            ScrollablePanel.VerticalScrollbar = ScrollbarWidget;

            InnerPanel = new Widget(Context);
            InnerPanel.WidthSizePolicy = SizePolicy.StretchToParent;
            InnerPanel.HeightSizePolicy = SizePolicy.CoverChildren;
            ScrollablePanel.InnerPanel = InnerPanel;
            ClipRect.AddChild(InnerPanel);

            int marginLeft = 0;
            int marginTop = 0;
            int i = 0;
            foreach (Brush brush in context.BrushFactory.Brushes)
            {
                Widget brushWidget = new Widget(context);
                brushWidget.WidthSizePolicy = SizePolicy.Fixed;
                brushWidget.HeightSizePolicy = SizePolicy.Fixed;
                brushWidget.SuggestedWidth = 100;
                brushWidget.SuggestedHeight = 100;
                brushWidget.MarginLeft = 10 + marginLeft;
                brushWidget.MarginTop = 10 + marginTop;

                ButtonWidget buttonWidget = new ButtonWidget(context);
                buttonWidget.WidthSizePolicy = SizePolicy.Fixed;
                buttonWidget.HeightSizePolicy = SizePolicy.Fixed;
                buttonWidget.SuggestedWidth = brush.DefaultLayer?.OverridenWidth > 0 ? brush.DefaultLayer.OverridenWidth : 80;
                buttonWidget.SuggestedHeight = brush.DefaultLayer?.OverridenHeight > 0 ? brush.DefaultLayer.OverridenHeight : 80;
                buttonWidget.Brush = brush;
                brushWidget.AddChild(buttonWidget);
                TextWidget textWidget = new TextWidget(context);
                textWidget.WidthSizePolicy = SizePolicy.CoverChildren;
                textWidget.Text = i.ToString();
                textWidget.Brush = Context.GetBrush("Order.Troop.CountText");
                textWidget.Brush.Color = TaleWorlds.Library.Color.Black;
                brushWidget.AddChild(textWidget);
                InnerPanel.AddChild(brushWidget);

                i++;
                marginLeft += 100;
                if (marginLeft == 1800)
                {
                    marginLeft = 0;
                    marginTop += 100;
                }
            }
        }
    }
}