using Luny;
using System;

namespace LunyScript.Unity
{
	/// <summary>
	/// Serializable representation of a single local variable for Inspector editing.
	/// </summary>
	[Serializable]
	public sealed class InspectorVariable
	{
		public enum VariableType
		{
			Number = Variable.ValueType.Number,
			Boolean = Variable.ValueType.Boolean,
			String = Variable.ValueType.String,
		}

		public String Name;
		public VariableType VarType;
		public Boolean BoolValue;
		public Double NumberValue;
		public String TextValue;

		public Variable ToVariable() => VarType switch
		{
			VariableType.Boolean => Variable.Named(BoolValue, Name),
			VariableType.Number => Variable.Named(NumberValue, Name),
			VariableType.String => Variable.Named(TextValue, Name),
			var _ => Variable.Named($"unhandled type: {VarType}", Name),
		};
	}
}
