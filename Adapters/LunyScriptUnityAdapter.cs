using Luny.Unity;
using LunyScript.Diagnostics;
using System;
using UnityEditor;
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

#if UNITY_EDITOR
		// this ensures proper "disabled domain reload" behaviour
		[InitializeOnLoadMethod]
		private static void Init()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

			// clear once after domain reload
			EnsureStaticFieldsAreNull();
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.ExitingEditMode)
				EnsureStaticFieldsAreNull();
		}

		private static void EnsureStaticFieldsAreNull() => ScriptDiagnosticsObserver.ResetStaticFields();
#endif
	}
}
