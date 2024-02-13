namespace Alliance.Common.Core.Configuration.Utilities
{
    /// <summary>
    /// Various compression informations used to optimize NetworkMessage usage.
    /// </summary>
    public static class CompressionHelper
    {
        public static readonly TaleWorlds.MountAndBlade.CompressionInfo.Integer ConfigFieldCountCompressionInfo = new(0, ConfigManager.Instance.ConfigFields.Count, true);
        public static readonly TaleWorlds.MountAndBlade.CompressionInfo.Integer DefaultIntValueCompressionInfo = new(-1, 20000, true);
        public static readonly TaleWorlds.MountAndBlade.CompressionInfo.Float DefaultFloatValueCompressionInfo = new(0f, 10, 0.01f);
    }
}
