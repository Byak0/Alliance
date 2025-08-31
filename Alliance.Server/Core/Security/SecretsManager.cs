using Microsoft.Extensions.Configuration;

namespace Alliance.Server.Core.Security
{
	public static class SecretsManager
	{
		/// <summary>
		/// Get configuration from User Secrets of this project
		/// </summary>
		private static readonly IConfigurationRoot configuration = new ConfigurationBuilder()
				.AddUserSecrets<SubModule>()
				.Build();

		public static IConfigurationRoot GetSecrets()
		{
			return configuration;
		}
	}
}
