using System;

namespace Alliance.Server.Core.Security
{
	public static class SecretsManager
	{
		public static readonly string DB_CONNECTION_STRING = Environment.GetEnvironmentVariable("ConnectionStrings");
		public static readonly string ZEVENT_AMOUNT_GET_API = Environment.GetEnvironmentVariable("ZeventAmountGetEndpoint");
	}
}
