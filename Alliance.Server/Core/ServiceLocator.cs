using Microsoft.Extensions.DependencyInjection;
using System;

namespace Alliance.Server.Core
{
	public static class ServiceLocator
	{
		private static IServiceProvider _serviceProvider;

		public static void Initialize(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		public static T GetService<T>() where T : class
		{
			if (_serviceProvider == null)
				throw new InvalidOperationException("ServiceLocator not initialized");

			return _serviceProvider.GetService<T>();
		}
	}
}
