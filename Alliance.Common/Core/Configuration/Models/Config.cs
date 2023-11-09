namespace Alliance.Common.Core.Configuration.Models
{
    public sealed class Config : DefaultConfig
    {
        // Singleton
        private static Config instance = new();

        public static Config Instance
        {
            get
            {
                return instance;
            }
            set
            {
                instance = value;
            }
        }

        static Config()
        {
        }
    }
}
