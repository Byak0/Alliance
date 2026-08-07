using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using TaleWorlds.Library;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// The "literal" origin for a <c>ValueSource&lt;MatrixFrame&gt;</c> slot. Holds a serializable
	/// <see cref="FrameValue"/> (position + rotation_euler + scale, native format) that is composed into a
	/// live <see cref="MatrixFrame"/> at runtime. Edited visually through the "Place on map" ghost preview.
	/// </summary>
	[Serializable]
	[PhrasePreview("at {Frame}")]
	[PhraseTemplate("at {Frame}")]
	public class FrameLiteralValue : ValueSource<MatrixFrame>
	{
		[ConfigProperty(label: "Frame", tooltip: "Position, rotation and scale of the entity.")]
		public FrameValue Frame = new FrameValue();

		public FrameLiteralValue() { }

		public FrameLiteralValue(FrameValue frame) { Frame = frame; }

		public override MatrixFrame Resolve(VariableStore context, VariableStore globals)
			=> Frame?.ToFrame() ?? MatrixFrame.Identity;
	}
}
