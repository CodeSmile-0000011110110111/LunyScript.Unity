using Luny;
using LunyScript.SmokeTests;
using LunyScript.SmokeTests.Lifecycle;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace LunyScript.Unity.SmokeTests
{
	public sealed class LifecycleTestController : MonoBehaviour
	{
		[Header("Tests")]
		public Boolean Assert_Runs_WhenCreated_Passed;
		public Boolean Assert_Runs_WhenDestroyed_Passed;
		public Boolean Assert_Runs_WhenEnabled_Passed;
		public Boolean Assert_Runs_WhenDisabled_Passed;
		public Boolean Assert_Runs_WhenReady_Passed;
		public Boolean Assert_Runs_EveryFixedStep_Passed;
		public Boolean Assert_Runs_EveryFrame_Passed;
		public Boolean Assert_Runs_EveryFrameEnds_Passed;

		[Header("Debug")]
		public Boolean ReloadScene;

		private void OnValidate()
		{
			if (ReloadScene && gameObject.activeInHierarchy && enabled)
			{
				ReloadScene = false;
				StartCoroutine(ReloadSceneNextFrame());
			}
		}

		private IEnumerator ReloadSceneNextFrame()
		{
			yield return null;

			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}

		private void OnVariableChanged(Object sender, VariableChangedArgs changedVar)
		{
			if (changedVar.Name.StartsWith("Time."))
				return;

			LunyLogger.LogInfo($"{changedVar}", this);

			var pass = changedVar.Current.AsBoolean();
			if (changedVar.Name == nameof(Assert_Runs_OnCreated))
				Assert_Runs_WhenCreated_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_OnDestroyed))
				Assert_Runs_WhenDestroyed_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_OnEnabled))
				Assert_Runs_WhenEnabled_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_OnDisabled))
				Assert_Runs_WhenDisabled_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_OnReady))
				Assert_Runs_WhenReady_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_OnHeartbeat))
				Assert_Runs_EveryFixedStep_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_OnFrameUpdate))
				Assert_Runs_EveryFrame_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_OnFrameLateUpdate))
				Assert_Runs_EveryFrameEnds_Passed = pass;
			else
				LunyLogger.LogWarning($"unhandled {changedVar}", this);
		}

#pragma warning disable 0114 // hides inherited member
		private void Awake()
		{
			// LunyLogger.LogWarning($"AWAKE: Frame {Time.frameCount}", this);
			var globalVars = ScriptEngine.Instance.GlobalVariables;
			globalVars.RemoveAll();
			globalVars.OnVariableChanged += OnVariableChanged;
		}

		// private void OnEnable() => LunyLogger.LogWarning($"ONENABLE: Frame {Time.frameCount}", this);
		// private void Start() => LunyLogger.LogWarning($"START: Frame {Time.frameCount}", this);
		// private void Update() => LunyLogger.LogWarning($"UPDATE: Frame {Time.frameCount}", this);

		private void OnDestroy()
		{
			var scriptEngine = ScriptEngine.Instance;
			if (scriptEngine != null)
				scriptEngine.GlobalVariables.OnVariableChanged -= OnVariableChanged;
		}
#pragma warning restore 0114 // hides inherited member
	}
}
