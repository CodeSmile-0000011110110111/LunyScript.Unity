using Luny;
using Luny.Engine;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LunyScript.Unity
{
	[RequireComponent(typeof(LunyScriptVariables))]
	public class LunyScriptBehaviour : MonoBehaviour
	{
		/*// TODO: proper key-value Inspector editor
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
		}*/
	}

	// TODO: this belongs in LunyScript or Luny layer, with reference to UnityGameObject removed
}
