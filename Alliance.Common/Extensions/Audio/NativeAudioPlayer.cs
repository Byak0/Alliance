using Alliance.Common.Extensions.Audio.NetworkMessages.FromServer;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.Audio
{
	/// <summary>
	/// Audio player for native sounds. Sucks a bit. 
	/// Prefer using AudioPlayer for custom sounds.
	/// </summary>
	public class NativeAudioPlayer
	{
		private static NativeAudioPlayer _instance;

		public static NativeAudioPlayer Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new NativeAudioPlayer();
				}
				return _instance;
			}
		}

		public NativeAudioPlayer()
		{
		}

		/// <summary>
		/// Play sound at position with specified duration.
		/// </summary>
		/// <param name="synchronize">Set to true to synchronize with all clients</param>
		public void PlaySound(int soundIndex, Vec3 position, int soundDuration = -1, bool synchronize = false)
		{
			SoundEvent eventRef = SoundEvent.CreateEvent(soundIndex, Mission.Current?.Scene);

			// If position is invalid, use the agent's position or the camera's position
			if (position.IsValid)
			{
				eventRef.SetPosition(position);
			}
			else if (Agent.Main != null)
			{
				eventRef.SetPosition(Agent.Main.Position);
			}
			else
			{
				eventRef.SetPosition(Mission.Current.GetCameraFrame().origin);
			}

			eventRef.Play();

			if (soundDuration != -1) DelayedStop(eventRef, soundDuration * 1000);

			if (synchronize)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				if (position.IsValid)
				{
					GameNetwork.WriteMessage(new SyncNativeSoundLocalized(soundIndex, soundDuration, position));
				}
				else
				{
					GameNetwork.WriteMessage(new SyncNativeSound(soundIndex, soundDuration));
				}
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}

		public void PlaySound(string soundName, int soundDuration = -1, bool synchronize = false)
		{
			Vec3 position = Agent.Main?.Position ?? Vec3.Invalid;
			Log($"Alliance - Playing sound {soundName} at {position} for {soundDuration}s", LogLevel.Debug);
			PlaySound(SoundEvent.GetEventIdFromString(soundName), position, soundDuration, synchronize);
		}

		public void PlaySoundLocalized(string soundName, Vec3 position, int soundDuration = -1, bool synchronize = false)
		{
			Log($"Alliance - Playing localized sound {soundName} at {position} for {soundDuration}s", LogLevel.Debug);
			PlaySound(SoundEvent.GetEventIdFromString(soundName), position, soundDuration, synchronize);
		}

		// Delayed stop to prevent ambient sounds from looping
		private async void DelayedStop(SoundEvent eventRef, int soundDuration)
		{
			await Task.Delay(soundDuration);
			eventRef.Stop();
		}
	}
}
