using Luny;
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

		private Table _table;
		private WeakReference<ScriptRuntimeContext> _runtimeContextRef;

		/// <summary>
		/// The live Table — valid in edit-mode and after Awake in play-mode.
		/// </summary>
		internal ITable Table => _table ??= BuildTable();

		/// <summary>
		/// Read-only access to the serialized variable descriptors (for Inspector rendering).
		/// </summary>
		internal IReadOnlyList<InspectorVariable> Variables => _variables;

		private void Awake()
		{
			_table = null; // ensure table gets rebuilt in playmode
			RegisterScriptInstantiated();
		}

		private void OnDestroy() => UnregisterScriptInstantiated();

		private void OnScriptInstantiated(ScriptRuntimeContext ctx)
		{
			UnregisterScriptInstantiated();

			if (ctx is ScriptRuntimeContext runtimeContext)
			{
				_runtimeContextRef = new WeakReference<ScriptRuntimeContext>(runtimeContext);

				var table = TryGetTable();
				if (table != null)
					runtimeContext.SetLocalVariables(table);
			}

			RegisterScriptBuilt();
		}
		private void OnScriptBuilt(ScriptRuntimeContext ctx)
		{
			UnregisterScriptBuilt();


			LunyLogger.LogInfo($"Table after build: {_table}", ctx.LunyObject);
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

		private void RegisterScriptBuilt()
		{
			var scriptEngine = (IScriptEngineInternal)ScriptEngine.Instance;
			scriptEngine.OnScriptBuilt += OnScriptBuilt;
		}

		private void UnregisterScriptBuilt()
		{
			var scriptEngine = (IScriptEngineInternal)ScriptEngine.Instance;
			if (scriptEngine != null)
				scriptEngine.OnScriptBuilt -= OnScriptBuilt;
		}

		private Table BuildTable()
		{
			var table = new Table();
			foreach (var v in _variables)
			{
				if (!String.IsNullOrEmpty(v.Name))
				{
					if (!table.Has(v.Name))
					{
						var varHandle = table.DefineVariable(v.Name, v.ToVariable());
						v.SetVarHandle(varHandle);
					}
					else
					{
						LunyLogger.LogWarning($"Variable named '{v.Name}' already exists");
					}
				}
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

		/// <summary>
		/// Adds a variable to the serialized list (edit-mode use).
		/// </summary>
		internal void AddVariable(InspectorVariable variable)
		{
			_variables.Add(variable);
			_table = null;
		}

		/// <summary>
		/// Removes a variable by name from the serialized list (edit-mode use).
		/// </summary>
		internal Boolean RemoveVariable(String name)
		{
			var index = _variables.FindIndex(v => v.Name == name);
			if (index < 0)
				return false;

			_variables.RemoveAt(index);
			_table = null;
			return true;
		}
	}
}
