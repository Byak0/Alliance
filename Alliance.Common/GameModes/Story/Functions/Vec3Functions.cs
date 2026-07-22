using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Functions
{
	/// <summary>Returns the world position of an agent.</summary>
	[Serializable]
	[PhrasePreview("position of {Agent}")]
	[PhraseTemplate("position of {Agent}")]
	public class GetAgentPositionFunction : Function
	{
		public override Type ReturnType => typeof(Vec3);

		[ConfigProperty(label: "Agent", tooltip: "Agent to get the position of.")]
		public ValueSource<Agent> Agent = new VariableValue<Agent>();

		public GetAgentPositionFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Agent agent = Agent?.Resolve(ctx, globals);
			return agent?.Position ?? Vec3.Zero;
		}
	}

	/// <summary>Returns the resolved center of a zone.</summary>
	[Serializable]
	[PhrasePreview("center of {Zone}")]
	[PhraseTemplate("center of {Zone}")]
	public class GetZoneCenterFunction : Function
	{
		public override Type ReturnType => typeof(Vec3);

		[ConfigProperty(label: "Zone", tooltip: "Zone to get the center of.")]
		public ValueSource<Zone> Zone = new LiteralValue<Zone>(new Zone());

		public GetZoneCenterFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Zone zone = Zone?.Resolve(ctx, globals);
			return zone?.ResolveCenter(ctx, globals) ?? Vec3.Zero;
		}
	}

	/// <summary>Adds two positions together (useful for offsetting).</summary>
	[Serializable]
	[PhrasePreview("{Left} + {Right}")]
	[PhraseTemplate("{Left} plus {Right}")]
	public class AddVec3Function : Function
	{
		public override Type ReturnType => typeof(Vec3);

		[ConfigProperty(label: "Left")]
		public ValueSource<Vec3> Left = new VariableValue<Vec3>();
		[ConfigProperty(label: "Right")]
		public ValueSource<Vec3> Right = new VariableValue<Vec3>();

		public AddVec3Function() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Vec3 a = Left?.Resolve(ctx, globals) ?? Vec3.Zero;
			Vec3 b = Right?.Resolve(ctx, globals) ?? Vec3.Zero;
			return a + b;
		}
	}

	/// <summary>Subtracts one position from another.</summary>
	[Serializable]
	[PhrasePreview("{Left} - {Right}")]
	[PhraseTemplate("{Left} minus {Right}")]
	public class SubtractVec3Function : Function
	{
		public override Type ReturnType => typeof(Vec3);

		[ConfigProperty(label: "Left")]
		public ValueSource<Vec3> Left = new VariableValue<Vec3>();
		[ConfigProperty(label: "Right")]
		public ValueSource<Vec3> Right = new VariableValue<Vec3>();

		public SubtractVec3Function() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Vec3 a = Left?.Resolve(ctx, globals) ?? Vec3.Zero;
			Vec3 b = Right?.Resolve(ctx, globals) ?? Vec3.Zero;
			return a - b;
		}
	}

	/// <summary>Scales a position by a float factor.</summary>
	[Serializable]
	[PhrasePreview("{Value} * {Factor}")]
	[PhraseTemplate("{Value} times {Factor}")]
	public class ScaleVec3Function : Function
	{
		public override Type ReturnType => typeof(Vec3);

		[ConfigProperty(label: "Value")]
		public ValueSource<Vec3> Value = new VariableValue<Vec3>();
		[ConfigProperty(label: "Factor")]
		public ValueSource<float> Factor = new LiteralValue<float>(1f);

		public ScaleVec3Function() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Vec3 v = Value?.Resolve(ctx, globals) ?? Vec3.Zero;
			float f = Factor?.Resolve(ctx, globals) ?? 1f;
			return v * f;
		}
	}

	/// <summary>Constructs a position from X, Y, Z components.</summary>
	[Serializable]
	[PhrasePreview("position({X}, {Y}, {Z})")]
	[PhraseTemplate("position ({X}, {Y}, {Z})")]
	public class MakePositionFunction : Function
	{
		public override Type ReturnType => typeof(Vec3);

		[ConfigProperty(label: "X")]
		public ValueSource<float> X = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Y")]
		public ValueSource<float> Y = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Z")]
		public ValueSource<float> Z = new LiteralValue<float>(0f);

		public MakePositionFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			float x = AddFloatFunction.Resolve(X, ctx, globals);
			float y = AddFloatFunction.Resolve(Y, ctx, globals);
			float z = AddFloatFunction.Resolve(Z, ctx, globals);
			return new Vec3(x, y, z);
		}
	}

	/// <summary>Returns the X component of a position.</summary>
	[Serializable]
	[PhrasePreview("X of {Value}")]
	[PhraseTemplate("X of {Value}")]
	public class PositionXFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Value")]
		public ValueSource<Vec3> Value = new VariableValue<Vec3>();

		public PositionXFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Vec3 v = Value?.Resolve(ctx, globals) ?? Vec3.Zero;
			return v.x;
		}
	}

	/// <summary>Returns the Y component of a position.</summary>
	[Serializable]
	[PhrasePreview("Y of {Value}")]
	[PhraseTemplate("Y of {Value}")]
	public class PositionYFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Value")]
		public ValueSource<Vec3> Value = new VariableValue<Vec3>();

		public PositionYFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Vec3 v = Value?.Resolve(ctx, globals) ?? Vec3.Zero;
			return v.y;
		}
	}

	/// <summary>Returns the Z component of a position.</summary>
	[Serializable]
	[PhrasePreview("Z of {Value}")]
	[PhraseTemplate("Z of {Value}")]
	public class PositionZFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Value")]
		public ValueSource<Vec3> Value = new VariableValue<Vec3>();

		public PositionZFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Vec3 v = Value?.Resolve(ctx, globals) ?? Vec3.Zero;
			return v.z;
		}
	}

	/// <summary>Returns the normalized direction from Source to Target.</summary>
	[Serializable]
	[PhrasePreview("direction from {Source} to {Target}")]
	[PhraseTemplate("direction from {Source} to {Target}")]
	public class DirectionBetweenPositionsFunction : Function
	{
		public override Type ReturnType => typeof(Vec3);

		[ConfigProperty(label: "Source")]
		public ValueSource<Vec3> Source = new VariableValue<Vec3>();
		[ConfigProperty(label: "Target")]
		public ValueSource<Vec3> Target = new VariableValue<Vec3>();

		public DirectionBetweenPositionsFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Vec3 a = Source?.Resolve(ctx, globals) ?? Vec3.Zero;
			Vec3 b = Target?.Resolve(ctx, globals) ?? Vec3.Zero;
			Vec3 dir = b - a;
			return dir.LengthSquared > 0 ? dir.NormalizedCopy() : Vec3.Zero;
		}
	}

	/// <summary>Returns the distance between two positions (float).</summary>
	[Serializable]
	[PhrasePreview("distance from {Source} to {Target}")]
	[PhraseTemplate("distance from {Source} to {Target}")]
	public class DistanceBetweenPositionsFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Source")]
		public ValueSource<Vec3> Source = new VariableValue<Vec3>();
		[ConfigProperty(label: "Target")]
		public ValueSource<Vec3> Target = new VariableValue<Vec3>();

		public DistanceBetweenPositionsFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Vec3 a = Source?.Resolve(ctx, globals) ?? Vec3.Zero;
			Vec3 b = Target?.Resolve(ctx, globals) ?? Vec3.Zero;
			return a.Distance(b);
		}
	}

	/// <summary>Linearly interpolates between two positions by a float factor (0 = Source, 1 = Target).</summary>
	[Serializable]
	[PhrasePreview("lerp from {Source} to {Target} by {Alpha}")]
	[PhraseTemplate("lerp from {Source} to {Target} by {Alpha}")]
	public class LerpPositionFunction : Function
	{
		public override Type ReturnType => typeof(Vec3);

		[ConfigProperty(label: "Source")]
		public ValueSource<Vec3> Source = new VariableValue<Vec3>();
		[ConfigProperty(label: "Target")]
		public ValueSource<Vec3> Target = new VariableValue<Vec3>();
		[ConfigProperty(label: "Alpha", tooltip: "Interpolation factor (0 = Source, 1 = Target).")]
		public ValueSource<float> Alpha = new LiteralValue<float>(0.5f);

		public LerpPositionFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Vec3 a = Source?.Resolve(ctx, globals) ?? Vec3.Zero;
			Vec3 b = Target?.Resolve(ctx, globals) ?? Vec3.Zero;
			float t = AddFloatFunction.Resolve(Alpha, ctx, globals);
			return Vec3.Lerp(a, b, Math.Min(Math.Max(t, 0f), 1f));
		}
	}
}
