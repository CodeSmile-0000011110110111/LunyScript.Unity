using Luny.Engine.Bridge;
using UnityEngine;

namespace LunyScript.Unity.EventRelays
{
	internal abstract class MonoBehaviourEventRelay : MonoBehaviour
	{
		protected ScriptRuntimeContext _runtimeContext;
		protected LunyGameObject _lunyGameObject;

		internal void Initialize(ScriptRuntimeContext runtimeContext)
		{
			_lunyGameObject = (LunyGameObject)runtimeContext.LunyGameObject;
			_lunyGameObject.OnDestroyed += OnLunyGameObjectDestroy;
		}

		private void OnLunyGameObjectDestroy() => _lunyGameObject = null;
	}
}
