using Alliance.Server.Core.Security;
using Microsoft.Extensions.Configuration;

namespace Alliance.Server.Core.Database
{
	public static class DbConfig
	{
		public static string DbConnectionString
		{
			get
			{
				return SecretsManager.GetSecrets().GetConnectionString("DefaultConnection");
			}
		}
	}
}
