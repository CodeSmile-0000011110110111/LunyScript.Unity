using Luny.Engine.Bridge;
using UnityEngine;

namespace LunyScript.Unity.EventRelays
{
	internal abstract class MonoBehaviourEventRelay : MonoBehaviour
	{
		protected ScriptRuntimeContext _runtimeContext;
		protected LunyObject _lunyObject;

		internal void Initialize(ScriptRuntimeContext runtimeContext)
		{
			_lunyObject = (LunyObject)runtimeContext.LunyObject;
			_lunyObject.OnDestroy += OnLunyObjectDestroy;
		}

		private void OnLunyObjectDestroy() => _lunyObject = null;
	}
}
