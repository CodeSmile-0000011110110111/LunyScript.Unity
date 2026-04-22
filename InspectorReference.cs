using Luny;
using System;
using UnityEngine;

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

		// UnityEngine.Object — covers all Object subtypes
		public UnityEngine.Object RefValue;

		// Value types — serialized explicitly
		public Color ColorValue;
		public AnimationCurve CurveValue;
		public Vector3 Vector3Value;
	}
}
