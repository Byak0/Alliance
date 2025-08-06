using Microsoft.Extensions.Configuration;

namespace Alliance.Server.Core.Database
{
	public static class DbConfig
	{
		/// <summary>
		/// Get configuration from User Secrets of this project
		/// </summary>
		private static readonly IConfigurationRoot configuration = new ConfigurationBuilder()
				.AddUserSecrets<SubModule>()
				.Build();

		public static string DbConnectionString
		{
			get
			{
				return configuration.GetConnectionString("DefaultConnection");
			}
		}
	}
}
