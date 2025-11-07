using Alliance.Common.Core.Configuration;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.GameModes.Story.Utilities;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Core.Utils
{
	/// <summary>
	/// Various compression informations used to optimize NetworkMessage usage.
	/// </summary>
	public static class CompressionHelper
	{
		// Compression bits / max value for integer (power of 2 minus 1) :
		// Bits : 1  2  3  4  5  6   7   8   9   10   11   12   13    14    15    16
		// Max  : 1  3  7 15 31 63 127 255 511 1023 2047 4095 8191 16383 32767 65535
		public static readonly CompressionInfo.Integer ConfigFieldCountCompressionInfo = new(0, ConfigManager.Instance.ConfigFields.Count, true);
		public static readonly CompressionInfo.Integer DefaultIntValueCompressionInfo = new(-1, 15);
		public static readonly CompressionInfo.Integer IntValueCompressionInfoMax255 = new(-1, 8);
		public static readonly CompressionInfo.Float DefaultFloatValueCompressionInfo = new(0f, 10, 0.01f);
		public static readonly CompressionInfo.Integer LanguageCompressionInfo = new(0, LocalizationHelper.GetAvailableLanguages().Count - 1);
		public static readonly CompressionInfo.Integer AgentDataTypeCompressionInfo = new(0, (int)AgentDataType.All);
		public static readonly CompressionInfo.Integer AnimationCompressionInfo = new(-1, 10000, true);
		public static readonly int StringMaxLength = 512;
	}
}
