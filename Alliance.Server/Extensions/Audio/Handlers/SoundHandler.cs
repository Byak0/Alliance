using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.Audio.NetworkMessages.FromClient;
using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.Audio.Handlers
{
	public class SoundHandler : IHandlerRegister
	{
		public SoundHandler()
		{
		}

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<NativeSoundRequest>(HandleSoundRequest);
			reg.Register<AudioRequest>(HandleAudioRequest);
			reg.Register<MusicRequest>(HandleMusicRequest);
		}

		public bool HandleMusicRequest(NetworkCommunicator peer, MusicRequest message)
		{
			try
			{
				if (!peer.IsAdmin())
				{
					Log($"ATTENTION : {peer.UserName} sent a MusicRequest despite not being admin !", LogLevel.Error);
					return false;
				}
				Log($"Alliance - {peer.UserName} is requesting to play music {message.SoundIndex}.", LogLevel.Information);
				AudioPlayer.Instance.PlayMainMusic(message.SoundIndex, synchronize: true);
				return true;
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to play music {message.SoundIndex} as requested by {peer.UserName}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
			return false;
		}

		public bool HandleAudioRequest(NetworkCommunicator peer, AudioRequest message)
		{
			try
			{
				if (!peer.IsAdmin())
				{
					Log($"ATTENTION : {peer.UserName} sent an AudioRequest despite not being admin !", LogLevel.Error);
					return false;
				}
				Log($"Alliance - {peer.UserName} is requesting to play audio {message.SoundIndex}.", LogLevel.Information);
				AudioPlayer.Instance.Play(message.SoundIndex, 1f, synchronize: true);
				return true;
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to play audio {message.SoundIndex} as requested by {peer.UserName}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
			return false;
		}

		public bool HandleSoundRequest(NetworkCommunicator peer, NativeSoundRequest message)
		{
			try
			{
				if (!peer.IsAdmin())
				{
					Log($"ATTENTION : {peer.UserName} sent a NativeSoundRequest despite not being admin !", LogLevel.Error);
					return false;
				}
				Log($"Alliance - {peer.UserName} is requesting to play native sound {message.SoundIndex} for {message.SoundDuration} seconds.", LogLevel.Information);
				NativeAudioPlayer.Instance.PlaySound(message.SoundIndex, Vec3.Invalid, message.SoundDuration, true);
				return true;
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to play sound {message.SoundIndex} for {message.SoundDuration} seconds as requested by {peer.UserName}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
			return false;
		}
	}
}
