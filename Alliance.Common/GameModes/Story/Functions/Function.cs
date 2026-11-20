using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Xml.Serialization;

namespace Alliance.Common.GameModes.Story.Functions
{
	/// <summary>
	/// Base for pure computed-value functions composed inside <see cref="ValueSource{T}"/> expression trees.	
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
		/// parameters), resolves them through <c>.Resolve(context)</c>, then returns the boxed result.
		/// </summary>
		public abstract object Evaluate(VariableStore context);
	}
}
