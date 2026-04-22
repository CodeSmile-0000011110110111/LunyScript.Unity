using Luny;
using Luny.Unity.Bridge;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunyScript.Unity
{
	/// <summary>
	/// Holds serialized local variables for a LunyScript on this object.
	/// In edit-mode: owns the Table for Inspector preview.
	/// In play-mode: builds the Table and hands it to ScriptRuntimeContext before Build() runs.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class LunyScriptVariables : MonoBehaviour
	{
		[SerializeField] private List<InspectorVariable> _variables = new();
		[SerializeField] private List<InspectorReference> _references = new();

		private Table _table;

		/// <summary>
		/// The live Table — valid in edit-mode and after Awake in play-mode.
		/// </summary>
		internal ITable Table => _table ??= BuildTable();

		/// <summary>
		/// Read-only access to the serialized variable descriptors (for Inspector rendering).
		/// </summary>
		internal IReadOnlyList<InspectorVariable> Variables => _variables;

		/// <summary>
		/// Read-only access to the serialized reference descriptors (for Inspector rendering).
		/// </summary>
		internal IReadOnlyList<InspectorReference> References => _references;

		private static String EnsureUniqueName(InspectorVariable v, Table table)
		{
			var renameCount = 0;
			var uniqueName = v.Name;
			while (table.Has(uniqueName))
			{
				uniqueName = $"{v.Name} ({++renameCount})";
			}

			if (v.Name != uniqueName)
				v.Name = uniqueName;

			return uniqueName;
		}

		private void Awake()
		{
			ResetTable(); // ensure table gets rebuilt in playmode
			RegisterScriptInstantiated();
		}

		private void OnDestroy() => UnregisterScriptInstantiated();

		private void OnScriptInstantiated(ScriptBuildContext buildContext, ScriptRuntimeContext ctx)
		{
			if (ctx.LunyGameObject.NativeObjectId != (Int64)gameObject.GetEntityId())
				return;

			LunyLogger.LogWarning($"Script instantiated: {ctx}", this);
			UnregisterScriptInstantiated();

			var table = TryGetTable();
			if (table != null)
				ctx.SetLocalVariables(table);

			var refs = BuildEngineReferences();
			if (refs != null)
				buildContext.EngineReferences = refs;
		}

		private void RegisterScriptInstantiated()
		{
			var scriptEngine = (IScriptEngineInternal)ScriptEngine.Instance;
			scriptEngine.OnScriptInstantiated += OnScriptInstantiated;
		}

		private void UnregisterScriptInstantiated()
		{
			var scriptEngine = (IScriptEngineInternal)ScriptEngine.Instance;
			if (scriptEngine != null)
				scriptEngine.OnScriptInstantiated -= OnScriptInstantiated;
		}

		internal void ResetTable() => _table = null;

		private Table BuildTable()
		{
			var table = new Table();
			foreach (var v in _variables)
			{
				if (String.IsNullOrEmpty(v.Name))
					continue;

				var uniqueName = EnsureUniqueName(v, table);
				table.DefineVariable(uniqueName, v.ToVariable());
			}

			return table;
		}

		/// <summary>
		/// Returns the Table built from serialized variables, retaining the reference for live Inspector editing.
		/// Returns null if no variables have been defined.
		/// </summary>
		internal Table TryGetTable()
		{
			if (_variables.Count == 0)
				return null;

			_table ??= BuildTable();
			return _table;
		}

		private EngineReferences BuildEngineReferences()
		{
			if (_references.Count == 0)
				return null;

			var refs = new EngineReferences(
				gameObjectFactory: obj => UnityGameObject.ToLunyObject(obj),
				componentFactory: obj => throw new NotImplementedException("Component factory")
			);

			foreach (var r in _references)
			{
				if (String.IsNullOrEmpty(r.Name))
					continue;

				switch (r.RefType)
				{
					case EngineReferenceType.Color:
						refs.Add(r.Name, r.ColorValue, r.RefType);
						break;
					case EngineReferenceType.AnimationCurve:
						refs.Add(r.Name, r.CurveValue, r.RefType);
						break;
					case EngineReferenceType.Vector3:
						refs.Add(r.Name, r.Vector3Value, r.RefType);
						break;
					default:
						if (r.RefValue != null)
							refs.Add(r.Name, r.RefValue, r.RefType);
						break;
				}
			}

			return refs;
		}
	}
}
