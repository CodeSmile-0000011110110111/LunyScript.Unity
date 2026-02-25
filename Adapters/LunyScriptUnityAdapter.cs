using Luny.Unity.Engine;
using System;
using UnityEngine;

namespace LunyScript.Unity.Adapters
{
	[DefaultExecutionOrder(Int32.MinValue + 1)] // Run before all other scripts but after main Unity engine adapter
	[AddComponentMenu("GameObject/")] // Do not list in "Add Component" menu
	[DisallowMultipleComponent]
	internal sealed class LunyScriptUnityAdapter : MonoBehaviour
	{
		private LunyScriptMonoBehaviourEventRelayInstaller _eventRelayInstaller;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void OnBeforeSceneLoad()
		{
			var engineAdapter = (LunyEngineUnityAdapter)LunyEngineUnityAdapter.Instance;
			engineAdapter.gameObject.AddComponent<LunyScriptUnityAdapter>();
		}

		private void Awake()
		{
			_eventRelayInstaller = new LunyScriptMonoBehaviourEventRelayInstaller();
			_eventRelayInstaller.Initialize();
		}

		private void OnDestroy()
		{
			_eventRelayInstaller.Shutdown();
			_eventRelayInstaller = null;
		}
	}
}
