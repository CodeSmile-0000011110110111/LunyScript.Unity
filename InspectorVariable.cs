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

			UnityObject = 100,
			GameObject = 101,
			ScriptableObject = 102,
			Prefab = 103,

			Component = 110,
			Transform = 111,
			Rigidbody = 112,

			Material = 150,
			Mesh = 151,
		}

		public String Name;
		public Variable.ValueType VarValueType;
		public VariableType VarType;
		public Boolean BoolValue;
		public Double NumberValue;
		public String TextValue;
		public Object ReferenceValue;

		public Variable ToVariable() => VarValueType switch
		{
			Variable.ValueType.Boolean => Variable.Named(BoolValue, Name),
			Variable.ValueType.Number => Variable.Named(NumberValue, Name),
			Variable.ValueType.String => Variable.Named(TextValue, Name),
			Variable.ValueType.Object => Variable.Named((object)null, Name),
			//Variable.ValueType.Object => Variable.Named((Object)UnityObject, Name), // must cast, or else the Boolean override gets called!
			var _ => throw new ArgumentOutOfRangeException(nameof(VarValueType), $"unhandled variable type: {VarValueType}"),
		};
	}
}
