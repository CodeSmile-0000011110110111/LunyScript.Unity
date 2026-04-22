using Luny;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LunyScript.Unity
{
	/// <summary>
	/// Serializable representation of a single engine reference for Inspector editing.
	/// </summary>
	[Serializable]
	public sealed class InspectorReference
	{
		public String Name;
		public EngineReferenceType RefType;

		// catch-all for UnityEngine.Object subtypes
		public Object RefValue;

		// non-UnityEngine.Object reference types
		public AnimationCurve CurveValue;

		// Value types — serialized explicitly
		public Color ColorValue;
		public Vector2 Vector2Value;
		public Vector3 Vector3Value;
	}
}
