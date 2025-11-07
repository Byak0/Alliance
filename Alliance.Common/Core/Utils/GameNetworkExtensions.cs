using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlayerServices;

namespace Alliance.Common.Core.Utils
{
	public static class GameNetworkExtensions
	{
		public static void WritePlayerIdToPacket(PlayerId playerId)
		{
			// Compress PlayerId into 9 bytes
			byte[] bytes = new byte[9];
			byte part1 = ((byte)playerId.Part1);
			byte part2 = ((byte)playerId.Part2);
			byte part3 = ((byte)playerId.Part3);
			byte header = (byte)((part1 << 4) | (part2 << 2) | part3);
			bytes[0] = header;
			byte[] part4 = BitConverter.GetBytes(playerId.Part4);
			if (!BitConverter.IsLittleEndian) Array.Reverse(part4);
			Array.Copy(part4, 0, bytes, 1, 8);

			GameNetworkMessage.WriteByteArrayToPacket(bytes, 0, 9);
		}

		public static PlayerId ReadPlayerIdFromPacket(ref bool bufferReadValid)
		{
			// Decompress PlayerId from 9 bytes
			byte[] buffer = new byte[9];
			GameNetworkMessage.ReadByteArrayFromPacket(buffer, 0, 9, ref bufferReadValid);
			if (!bufferReadValid) return default;

			byte header = buffer[0];
			ulong part4 = BitConverter.ToUInt64(buffer, 1);
			if (!BitConverter.IsLittleEndian) Array.Reverse(buffer);
			ulong part1 = (ulong)((header >> 4) & 0x0F);
			ulong part2 = (ulong)((header >> 2) & 0x03);
			ulong part3 = (ulong)(header & 0x03);

			return new PlayerId(part1, part2, part3, part4);
		}

		public static void WriteDateTimeToPacket(DateTime date)
		{
			if (date == DateTime.MinValue)
			{
				GameNetworkMessage.WriteBoolToPacket(false); // no date
				return;
			}

			GameNetworkMessage.WriteBoolToPacket(true);
			int yearOffset = date.Year - 2000;
			if (yearOffset < 0) yearOffset = 0; // clamp

			uint dateUint = (uint)((yearOffset << 25)
				| (date.Month << 21)
				| (date.Day << 16)
				| (date.Hour << 11)
				| (date.Minute << 5));

			GameNetworkMessage.WriteUintToPacket(dateUint, CompressionBasic.GUIDCompressionInfo);
		}

		public static DateTime ReadDateTimeFromPacket(ref bool bufferReadValid)
		{
			bool hasDate = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			if (!hasDate) return DateTime.MinValue;

			uint dateUint = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
			int minute = (int)((dateUint >> 5) & 0x3F);
			int hour = (int)((dateUint >> 11) & 0x1F);
			int day = (int)((dateUint >> 16) & 0x1F);
			int month = (int)((dateUint >> 21) & 0x0F);
			int year = (int)(((dateUint >> 25) & 0x7F) + 2000);

			return new DateTime(year, month, day, hour, minute, 0);
		}
	}
}
