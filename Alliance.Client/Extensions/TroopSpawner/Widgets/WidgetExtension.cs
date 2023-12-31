﻿using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using HAlign = TaleWorlds.GauntletUI.HorizontalAlignment;
using SP = TaleWorlds.GauntletUI.SizePolicy;
using VAlign = TaleWorlds.GauntletUI.VerticalAlignment;

namespace Alliance.Client.Extensions.TroopSpawner.Widgets
{
    public static class WidgetExtension
    {
        public static void Init(this Widget widget, SP? width = null, SP? height = null, float? suggestedWidth = null, float? suggestedHeight = null, HAlign? hAlign = null, VAlign? vAlign = null, float? marginTop = null, float? marginBottom = null, float? marginLeft = null, float? marginRight = null, Brush brush = null)
        {
            if (width != null) widget.WidthSizePolicy = (SP)width;
            if (height != null) widget.HeightSizePolicy = (SP)height;
            if (suggestedWidth != null) widget.SuggestedWidth = (float)suggestedWidth;
            if (suggestedHeight != null) widget.SuggestedHeight = (float)suggestedHeight;
            if (hAlign != null) widget.HorizontalAlignment = (HAlign)hAlign;
            if (vAlign != null) widget.VerticalAlignment = (VAlign)vAlign;
            if (marginTop != null) widget.MarginTop = (float)marginTop;
            if (marginBottom != null) widget.MarginBottom = (float)marginBottom;
            if (marginLeft != null) widget.MarginLeft = (float)marginLeft;
            if (marginRight != null) widget.MarginRight = (float)marginRight;
            if (brush != null) ((BrushWidget)widget).Brush = brush;
        }
    }
}