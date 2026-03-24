using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Utilities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Core.Configuration
{
	/// <summary>
	/// Initializer for configuration. Keep watch over config file to detect real-time changes.
	/// </summary>
	public class ConfigInitializer
	{
		private const int MIN_TIME_BETWEEN_UPDATES_TICKS = 10_000_000; // 1 second = 10 000 000 ticks
		private const int FILE_ACCESS_MAX_RETRIES = 5;
		private const int FILE_ACCESS_DELAY_MS = 100;
		private const int LOAD_CONFIG_MAX_RETRIES = 3;
		private const int WATCHER_COOLDOWN_MS = 100;

		private static DateTime _lastRead = DateTime.MinValue;
		private static FileSystemWatcher _configWatcher;

		/// <summary>
		/// Initialize the config from file and start watching for changes.
		/// </summary>
		public static async void Init()
		{
			Log($"Initializing config from file {SubModule.ConfigFilePath}");

			await LoadAndApplyConfigAsync(isInitialization: true);

			// Watch changes to the config file
			_configWatcher = SerializeHelper.CreateFileWatcher(SubModule.ConfigFilePath, OnConfigFileChanged);

			Log("Config initialized successfully");
		}

		/// <summary>
		/// Load config from file, validate it, and apply to Config.Instance (async).
		/// Saves corrected config if needed.
		/// </summary>
		private static async Task LoadAndApplyConfigAsync(bool isInitialization)
		{
			ConfigLoadResult result = await TryLoadConfigAsync();

			if (!result.IsSuccessful)
			{
				await HandleLoadFailureAsync(result, isInitialization);
				return;
			}

			// Validate and apply the loaded config
			bool needsCorrection = ConfigManager.Instance.ValidateConfigInstance(result.Config);

			if (isInitialization)
			{
				ConfigManager.Instance.ApplyModOptions(result.Config);
			}
			else
			{
				ConfigManager.Instance.UpdateConfigFromDeserialized(result.Config, result.Config.SyncConfig);
			}

			// Save if corrections were made
			if (needsCorrection)
			{
				Log("Config had invalid values that were corrected - saving", LogLevel.Warning);
				await SaveConfigAsync();
			}
		}

		/// <summary>
		/// Handle config load failure by resetting to defaults (async).
		/// </summary>
		private static async Task HandleLoadFailureAsync(ConfigLoadResult result, bool isInitialization)
		{
			string reason = result.IsTemporaryError
				? "file inaccessible after retries"
				: "invalid XML syntax";

			if (isInitialization)
			{
				Log($"Config initialization failed ({reason}) - creating default config", LogLevel.Error);
				ConfigManager.Instance.ApplyModOptions(new Config());
			}
			else
			{
				Log($"Config reload failed ({reason}) - keeping current values", LogLevel.Warning);
			}

			await SaveConfigAsync();
		}

		/// <summary>
		/// Save current config to file with watcher protection (async).
		/// </summary>
		private static async Task SaveConfigAsync()
		{
			try
			{
				// Temporarily disable watcher to prevent self-triggering
				if (_configWatcher != null)
				{
					_configWatcher.EnableRaisingEvents = false;
				}

				if (SerializeHelper.SaveClassToFile(SubModule.ConfigFilePath, Config.Instance))
				{
					Log($"Config saved to {SubModule.ConfigFilePath}", LogLevel.Information);
				}
				else
				{
					Log("Failed to save config file", LogLevel.Error);
				}

				// Wait for filesystem to complete write
				await Task.Delay(WATCHER_COOLDOWN_MS);
			}
			catch (Exception ex)
			{
				Log($"Error saving config: {ex.Message}", LogLevel.Error);
			}
			finally
			{
				// Re-enable watcher
				if (_configWatcher != null)
				{
					_configWatcher.EnableRaisingEvents = true;
				}
			}
		}

		/// <summary>
		/// Called when the config file changes externally.
		/// </summary>
		private static async void OnConfigFileChanged(object source, FileSystemEventArgs e)
		{
			try
			{
				// Throttle updates - prevent too frequent changes
				DateTime lastWriteTime = File.GetLastWriteTime(SubModule.ConfigFilePath);
				if (lastWriteTime.Ticks - _lastRead.Ticks < MIN_TIME_BETWEEN_UPDATES_TICKS)
				{
					return;
				}

				// Wait for file to be ready (not locked by editor)
				if (!await WaitForFileReadyAsync(SubModule.ConfigFilePath))
				{
					Log("Config file changed but not accessible - skipping update", LogLevel.Warning);
					return;
				}

				// Load and apply the changed config
				await LoadAndApplyConfigAsync(isInitialization: false);

				_lastRead = lastWriteTime;
			}
			catch (Exception ex)
			{
				Log($"Unexpected error updating config: {ex.Message}", LogLevel.Error);
			}
		}

		/// <summary>
		/// Try to load config from file with retry logic (async).
		/// </summary>
		private static async Task<ConfigLoadResult> TryLoadConfigAsync()
		{
			for (int attempt = 1; attempt <= LOAD_CONFIG_MAX_RETRIES; attempt++)
			{
				try
				{
					Config loadedConfig = SerializeHelper.LoadClassFromFile(
						SubModule.ConfigFilePath,
						new Config());

					return ConfigLoadResult.Success(loadedConfig);
				}
				catch (Exception ex)
				{
					bool isTemporaryError = IsTemporaryXmlError(ex);

					// Retry on temporary errors
					if (isTemporaryError && attempt < LOAD_CONFIG_MAX_RETRIES)
					{
						Log($"Temporary XML error (attempt {attempt}/{LOAD_CONFIG_MAX_RETRIES}) - retrying...", LogLevel.Debug);
						await Task.Delay(FILE_ACCESS_DELAY_MS * attempt);
						continue;
					}

					// Failed permanently
					return ConfigLoadResult.Failure(isTemporaryError, ex.Message);
				}
			}

			return ConfigLoadResult.Failure(isTemporaryError: false, "Max retries exceeded");
		}

		/// <summary>
		/// Check if exception is a temporary file access error.
		/// </summary>
		private static bool IsTemporaryXmlError(Exception ex)
		{
			return ex.Message.Contains("(0, 0)") ||
			       ex.Message.Contains("Root element is missing") ||
			       ex.Message.Contains("Unexpected end of file");
		}

		/// <summary>
		/// Wait for file to be accessible with exponential backoff (async, non-blocking).
		/// </summary>
		private static async Task<bool> WaitForFileReadyAsync(string filePath)
		{
			for (int i = 0; i < FILE_ACCESS_MAX_RETRIES; i++)
			{
				if (IsFileAccessible(filePath))
				{
					return true;
				}

				await Task.Delay(FILE_ACCESS_DELAY_MS * (i + 1));
			}

			return false;
		}

		/// <summary>
		/// Check if file is accessible and not empty.
		/// </summary>
		private static bool IsFileAccessible(string filePath)
		{
			try
			{
				using FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				return stream.Length > 0;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Result of a config load attempt.
		/// </summary>
		private class ConfigLoadResult
		{
			public bool IsSuccessful { get; private set; }
			public Config Config { get; private set; }
			public bool IsTemporaryError { get; private set; }
			public string ErrorMessage { get; private set; }

			public static ConfigLoadResult Success(Config config)
			{
				return new ConfigLoadResult
				{
					IsSuccessful = true,
					Config = config
				};
			}

			public static ConfigLoadResult Failure(bool isTemporaryError, string errorMessage)
			{
				return new ConfigLoadResult
				{
					IsSuccessful = false,
					IsTemporaryError = isTemporaryError,
					ErrorMessage = errorMessage
				};
			}
		}
	}
}
