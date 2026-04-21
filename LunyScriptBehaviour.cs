using Luny.Engine.Bridge;
using Luny.Unity.Bridge;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LunyScript.Unity
{
	[RequireComponent(typeof(LunyScriptVariables))]
	public class LunyScriptBehaviour : MonoBehaviour
	{
		// TODO: proper key-value Inspector editor
		[SerializeField] private String[] _referenceKeys = Array.Empty<String>();
		[SerializeField] private Object[] _referenceValues = Array.Empty<Object>();

		// TODO: call from where? Somewhere from ScriptBuilder before calling Initialize/Build
		// perhaps via ScriptRuntimeContext or its LunyObject
		// alternative: use a SerializedDictionary
		internal Boolean TryGetEngineReferences(out EngineReferences refs)
		{
			refs = null;

			if (_referenceKeys == null || _referenceKeys.Length == 0)
				return false;

			refs = new EngineReferences();
			for (var i = 0; i < _referenceKeys.Length; i++)
			{
				var key = _referenceKeys[i];
				var value = _referenceValues[i];
				var nativeId = value != null ? value.GetEntityId() : 0l;
				var isSceneRef = value switch
				{
					GameObject go => go.scene.IsValid(),
					Component comp => comp.gameObject.scene.IsValid(),
					var _ => false,
				};

				refs.Add(key, value, nativeId, isSceneRef);
			}
			return true;
		}
	}

	// TODO: this belongs in LunyScript or Luny layer
	public sealed class EngineReferences
	{
		private Dictionary<String, EngineReference> _references = new();

		internal void Add(String key, Object engineRef, Int64 nativeId, Boolean isSceneReference) => _references.Add(key, new EngineReference
		{
			Name = key,
			Value = engineRef,
			NativeId = nativeId,
			IsSceneReference = isSceneReference,
		});

		public Boolean TryGet(String name, out ILunyGameObject obj)
		{
			obj = null;
			if (!_references.TryGetValue(name, out var value))
				return false;

			// TODO: LunyObject should provide a static conversion method, but C# 9 doesn't support static overrides
			// LunyObject has TryGetCached with registry lookup, this should be utilized
			//
			// could register the object with Luny registry here but should be avoided in case the object is never actually used
			// though that's probably only a minor optimization
			obj = UnityGameObject.ToLunyObject(value);
			return true;
		}
	}

	public record EngineReference
	{
		public String Name;
		public Object Value;
		public Int64 NativeId;
		public Boolean IsSceneReference;

		// TODO: this should provide getters for LunyObject
	}
}
