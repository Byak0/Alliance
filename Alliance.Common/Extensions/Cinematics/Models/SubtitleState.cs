using TaleWorlds.GauntletUI;
using TaleWorlds.TwoDimension;

namespace Alliance.Common.Extensions.Cinematics.Models
{
	/// <summary>One active subtitle sample. <see cref="Source"/> is the originating keyframe - a stable
	/// identity hosts use to recycle UI items across frames (the list passed to the sink is reused).</summary>
	public struct SubtitleState
	{
		public object Source;
		public string Text;
		public float Alpha;
		public int FontSize;
		public string FontColor;
		public string Font;
		public TextHorizontalAlignment HAlign;
		public VerticalAlignment VAlign;
	}
}
