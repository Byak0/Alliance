using Alliance.Common.Extensions;
using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.Audio.NetworkMessages.FromServer;
using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.Audio.Handlers
{
	public class SoundClientHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SyncNativeSoundLocalized>(HandleSyncSoundLocalized);
			reg.Register<SyncNativeSound>(HandleSyncSound);
			reg.Register<SyncAudioLocalized>(HandleSyncAudioLocalized);
			reg.Register<SyncAudio>(HandleSyncAudio);
			reg.Register<SyncMusic>(HandleSyncMusic);
			reg.Register<SyncMusicLocalized>(HandleSyncMusicLocalized);
		}

		private void HandleSyncMusic(SyncMusic message)
		{
			try
			{
				Log($"Alliance - Playing music {message.SoundIndex} with volume {message.Volume} from {message.StartingPoint}", LogLevel.Debug);
				AudioPlayer.Instance.PlayMainMusic(message.SoundIndex, message.StartingPoint, message.Volume);
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to play music {message.SoundIndex}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}

		private void HandleSyncMusicLocalized(SyncMusicLocalized message)
		{
			try
			{
				Log($"Alliance - Playing music {message.SoundIndex} at {message.Position} with volume {message.Volume}", LogLevel.Debug);
				AudioPlayer.Instance.PlayLocalizedMusic(message.SoundIndex, message.Volume, message.Position, message.MaxHearingDistance, message.StartingPoint, message.MuteMainMusic);
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to play music {message.SoundIndex}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}

		private void HandleSyncAudio(SyncAudio message)
		{
			try
			{
				Log($"Alliance - Playing audio {message.SoundIndex} with volume {message.Volume}", LogLevel.Debug);
				AudioPlayer.Instance.Play(message.SoundIndex, message.Volume, message.Stackable);
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to play audio {message.SoundIndex}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}

		private void HandleSyncAudioLocalized(SyncAudioLocalized message)
		{
			try
			{
				Log($"Alliance - Playing audio {message.SoundIndex} at {message.SoundOrigin} with volume {message.Volume}", LogLevel.Debug);
				AudioPlayer.Instance.Play(message.SoundIndex, message.Volume, message.Stackable, message.MaxHearingDistance, message.SoundOrigin);
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to play audio {message.SoundIndex}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}

		public void HandleSyncSoundLocalized(SyncNativeSoundLocalized message)
		{
			try
			{
				Log($"Alliance - Playing sound {message.SoundIndex} at {message.Position} for {message.SoundDuration}", LogLevel.Debug);
				NativeAudioPlayer.Instance.PlaySound(message.SoundIndex, message.Position, message.SoundDuration);
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to play sound {message.SoundIndex}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}

		public void HandleSyncSound(SyncNativeSound message)
		{
			try
			{
				Log($"Alliance - Playing sound {message.SoundIndex} for {message.SoundDuration}", LogLevel.Debug);
				NativeAudioPlayer.Instance.PlaySound(message.SoundIndex, Vec3.Invalid, message.SoundDuration);
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to play sound {message.SoundIndex}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}
	}
}
