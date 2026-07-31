using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncObjectiveProgressMessage : GameNetworkMessage
	{
		private const int TagNull = 0;
		private const int TagInt = 1;
		private const int TagFloat = 2;
		private const int TagString = 3;

		public struct ElementData
		{
			public byte Type;
			public string Text;
			public object[] Values;
			public float Current;
			public float Min;
			public float Max;
			public float EndTime;
		}

		public struct ObjectiveData
		{
			public bool IsCompleted;
			public ElementData[] Elements;
		}

		public ObjectiveData[] Objectives { get; private set; }

		public SyncObjectiveProgressMessage() { }

		public SyncObjectiveProgressMessage(ObjectiveData[] objectives)
		{
			Objectives = objectives;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(Objectives?.Length ?? 0, new CompressionInfo.Integer(0, 50, true));

			foreach (var obj in Objectives ?? Array.Empty<ObjectiveData>())
			{
				WriteBoolToPacket(obj.IsCompleted);
				WriteIntToPacket(obj.Elements?.Length ?? 0, new CompressionInfo.Integer(0, 10, true));

				foreach (var el in obj.Elements ?? Array.Empty<ElementData>())
				{
					WriteIntToPacket(el.Type, new CompressionInfo.Integer(0, 2, true));

					if (el.Type == 0) // TextElement
					{
						WriteStringToPacket(el.Text ?? "");
						WriteIntToPacket(el.Values?.Length ?? 0, new CompressionInfo.Integer(0, 10, true));
						if (el.Values != null)
						{
							foreach (var val in el.Values)
								WriteValue(val);
						}
					}
					else if (el.Type == 1) // BarElement
					{
						WriteStringToPacket(el.Text ?? "");
						WriteFloatToPacket(el.Current, new CompressionInfo.Float(0f, 100000f, 2));
						WriteFloatToPacket(el.Min, new CompressionInfo.Float(0f, 100000f, 2));
						WriteFloatToPacket(el.Max, new CompressionInfo.Float(0f, 100000f, 2));
					}
					else if (el.Type == 2) // TimerElement
					{
						WriteStringToPacket(el.Text ?? "");
						WriteFloatToPacket(el.EndTime, new CompressionInfo.Float(0f, 100000f, 2));
					}
				}
			}
		}

		protected override bool OnRead()
		{
			bool valid = true;
			int objCount = ReadIntFromPacket(new CompressionInfo.Integer(0, 50, true), ref valid);
			if (!valid) return false;

			Objectives = new ObjectiveData[objCount];

			for (int i = 0; i < objCount; i++)
			{
				Objectives[i].IsCompleted = ReadBoolFromPacket(ref valid);
				if (!valid) return false;

				int elCount = ReadIntFromPacket(new CompressionInfo.Integer(0, 10, true), ref valid);
				if (!valid) return false;

				var elements = new ElementData[elCount];
				for (int j = 0; j < elCount; j++)
				{
					elements[j].Type = (byte)ReadIntFromPacket(new CompressionInfo.Integer(0, 2, true), ref valid);
					if (!valid) return false;

					if (elements[j].Type == 0)
					{
						elements[j].Text = ReadStringFromPacket(ref valid);
						int valCount = ReadIntFromPacket(new CompressionInfo.Integer(0, 10, true), ref valid);
						if (!valid) return false;
						var vals = new object[valCount];
						for (int k = 0; k < valCount; k++)
						{
							vals[k] = ReadValue(ref valid);
							if (!valid) return false;
						}
						elements[j].Values = vals;
					}
					else if (elements[j].Type == 1)
					{
						elements[j].Text = ReadStringFromPacket(ref valid);
						elements[j].Current = ReadFloatFromPacket(new CompressionInfo.Float(0f, 100000f, 2), ref valid);
						elements[j].Min = ReadFloatFromPacket(new CompressionInfo.Float(0f, 100000f, 2), ref valid);
						elements[j].Max = ReadFloatFromPacket(new CompressionInfo.Float(0f, 100000f, 2), ref valid);
						if (!valid) return false;
					}
					else if (elements[j].Type == 2)
					{
						elements[j].Text = ReadStringFromPacket(ref valid);
						elements[j].EndTime = ReadFloatFromPacket(new CompressionInfo.Float(0f, 100000f, 2), ref valid);
						if (!valid) return false;
					}
				}
				Objectives[i].Elements = elements;
			}
			return valid;
		}

		private void WriteValue(object value)
		{
			if (value == null)
			{
				WriteIntToPacket(TagNull, new CompressionInfo.Integer(0, 3, true));
			}
			else if (value is int i)
			{
				WriteIntToPacket(TagInt, new CompressionInfo.Integer(0, 3, true));
				WriteIntToPacket(i, new CompressionInfo.Integer(int.MinValue, int.MaxValue, true));
			}
			else if (value is float f)
			{
				WriteIntToPacket(TagFloat, new CompressionInfo.Integer(0, 3, true));
				WriteFloatToPacket(f, new CompressionInfo.Float(float.MinValue, float.MaxValue, 2));
			}
			else
			{
				WriteIntToPacket(TagString, new CompressionInfo.Integer(0, 3, true));
				WriteStringToPacket(value.ToString());
			}
		}

		private object ReadValue(ref bool valid)
		{
			int tag = ReadIntFromPacket(new CompressionInfo.Integer(0, 3, true), ref valid);
			if (!valid) return null;
			switch (tag)
			{
				case TagNull: return null;
				case TagInt: return ReadIntFromPacket(new CompressionInfo.Integer(int.MinValue, int.MaxValue, true), ref valid);
				case TagFloat: return ReadFloatFromPacket(new CompressionInfo.Float(float.MinValue, float.MaxValue, 2), ref valid);
				case TagString: return ReadStringFromPacket(ref valid);
				default: valid = false; return null;
			}
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;

		protected override string OnGetLogFormat() => "SyncObjectiveProgressMessage";
	}
}
