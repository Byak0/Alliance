using Alliance.Common.Core.Utils;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Core.Configuration.NetworkMessages.FromServer
{
	/// <summary>
	/// NetworkMessage to synchronize single config field between server and clients.    
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncConfigField : GameNetworkMessage
	{
		public int FieldIndex { get; private set; }
		public object FieldValue { get; private set; }
		public FieldInfo FieldInfo { get => ConfigManager.Instance.ConfigFields[FieldIndex]; }

		public SyncConfigField() { }

		public SyncConfigField(int fieldIndex, object fieldValue)
		{
			FieldIndex = fieldIndex;
			FieldValue = fieldValue;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(FieldIndex, new CompressionInfo.Integer(0, ConfigManager.Instance.ConfigFields.Count, true));
			MultiplayerOptionsSerializer.WriteModOption(FieldInfo, FieldValue);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;

			FieldIndex = ReadIntFromPacket(new CompressionInfo.Integer(0, ConfigManager.Instance.ConfigFields.Count, true), ref bufferReadValid);
			FieldValue = MultiplayerOptionsSerializer.ReadModOption(FieldInfo, ref bufferReadValid);

			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Sync mod configuration " + ConfigManager.Instance.ConfigFields[FieldIndex].Name + " => " + FieldValue;
		}
	}
}