using Alliance.Common.Extensions.Cinematics.Models.Tracks;

namespace Alliance.Common.Extensions.Cinematics.Models
{
	/// <summary>One active subtitle sample. <see cref="Source"/> is the originating keyframe - a stable
	/// identity hosts use to recycle UI items across frames (the list passed to the sink is reused).
	/// NOTE: must stay free of client-only references (GauntletUI/TwoDimension). Referencing those from
	/// code compiled into the server build loads TaleWorlds.GauntletUI on the dedicated server, which
	/// breaks TaleWorlds.Diamond.MessageJsonConverter (GetTypes on assemblies missing client DLLs)
	/// and kills the master-server login. Map to UI enums in client-only code instead.</summary>
	public struct SubtitleState
	{
		public object Source;
		public string Text;
		public float Alpha;
		public int FontSize;
		public string FontColor;
		public string Font;
		public SubtitleHPosition HAlign;
		public SubtitleVPosition VAlign;
	}
}
