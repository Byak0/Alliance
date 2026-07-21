using System;
using System.Xml.Serialization;

namespace Alliance.Common.GameModes.Story.Functions
{
	/// <summary>
	/// Base for pure computed-value functions composed inside <see cref="ValueSource{T}"/> expression trees.
	/// <para>
	/// Three "callable" kinds share the same authoring shape (fields + phrase attributes):
	/// <list type="bullet">
	/// <item><see cref="Actions.ActionBase"/> = void function (top-level side effect, lifecycle <c>Register</c>).</item>
	/// <item><see cref="Conditions.Condition"/> = bool function (top-level predicate).</item>
	/// <item><see cref="Function"/> = typed-value function (composed inside an expression; no side effects).</item>
	/// </list>
	/// </para>
	/// <para>
	/// Non-generic on purpose: the serializer discovers concrete subclasses with <c>t.IsSubclassOf</c>
	/// (see <c>ScenarioSerializer.GetSerializableDerivedTypes</c>), which does not work for open generics.
	/// </para>
	/// <para>
	/// <b>When to add a field as <c>ValueSource&lt;T&gt;</c> vs a plain typed field:</b> only fields whose value
	/// is resolved at runtime (damage amount, spawn count, who/zone, dynamic message...) become expressions.
	/// Structural identifiers (map id, variable name, character id, enum type name) stay plain typed fields.
	/// </para>
	/// </summary>
	[Serializable]
	public abstract class Function
	{
		/// <summary>
		/// Type of value returned by the function. 
		/// A slot ValueSource&lt;T&gt; only offers functions whose ReturnType is assignable to T.
		/// </summary>
		[XmlIgnore]
		public abstract Type ReturnType { get; }

		/// <summary>
		/// Computes the value. Reads its own <c>[ConfigProperty]</c> fields (typically <c>ValueSource&lt;X&gt;</c>
		/// parameters), resolves them through <c>.Resolve(ctx, globals)</c>, then returns the boxed result.
		/// </summary>
		public abstract object Evaluate(TriggerContext ctx, VariableStore globals);
	}
}
