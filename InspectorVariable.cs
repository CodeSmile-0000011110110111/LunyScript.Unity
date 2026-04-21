using Luny;
using System;
using Object = System.Object;

namespace LunyScript.Unity
{
	/// <summary>
	/// Serializable representation of a single local variable for Inspector editing.
	/// </summary>
	[Serializable]
	public sealed class InspectorVariable
	{
		public String Name;
		public Variable.ValueType Type;
		public Boolean BoolValue;
		public Double NumberValue;
		public String TextValue;

		/*
		public enum UnityTopLevelReferenceType
		{
			None,
			Asset,
			Prefab,
			GameObject,
			Component,
		}
		*/

		public enum UnityReferenceType
		{
			None,

			// objects/assets
			Object, // catch-all
			GameObject,
			Prefab,

			// components
			Component, // catch-all
			Rigidbody,
			Transform,

			// assets
			Material,
			Mesh,

		}

		//public UnityReferenceType ReferenceType;
		//public UnityEngine.Object UnityObject;

		public Variable ToVariable() => Type switch
		{
			Variable.ValueType.Boolean => Variable.Named(BoolValue, Name),
			Variable.ValueType.Number => Variable.Named(NumberValue, Name),
			Variable.ValueType.String => Variable.Named(TextValue, Name),
			Variable.ValueType.Object => Variable.Named((object)null, Name),
			//Variable.ValueType.Object => Variable.Named((Object)UnityObject, Name), // must cast, or else the Boolean override gets called!
			var _ => throw new ArgumentOutOfRangeException(nameof(Type), $"unhandled variable type: {Type}"),
		};
	}
}
