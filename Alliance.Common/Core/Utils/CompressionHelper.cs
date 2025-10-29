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
		public static readonly CompressionInfo.Integer ConfigFieldCountCompressionInfo = new(0, ConfigManager.Instance.ConfigFields.Count, true);
		public static readonly CompressionInfo.Integer DefaultIntValueCompressionInfo = new(-1, 20000, true);
		public static readonly CompressionInfo.Float DefaultFloatValueCompressionInfo = new(0f, 10, 0.01f);
		public static readonly CompressionInfo.Integer LanguageCompressionInfo = new(0, LocalizationHelper.GetAvailableLanguages().Count - 1);
		public static readonly CompressionInfo.Integer AgentDataTypeCompressionInfo = new(0, (int)AgentDataType.All);
		public static readonly CompressionInfo.Integer AnimationCompressionInfo = new(-1, 10000, true);
		public static readonly int StringMaxLength = 512;
	}
}
