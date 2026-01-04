using LunyScript.Tests;
using System;
using UnityEngine;
using Object = System.Object;

namespace LunyScript.Unity.Tests
{
	public sealed class ObjectLifecycleTestController : MonoBehaviour
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

		private void Awake() =>
			// LunyLogger.LogWarning($"AWAKE: Frame {Time.frameCount}", this);
			LunyScriptEngine.Instance.GlobalVariables.OnVariableChanged += OnVariableChanged;

		// private void OnEnable() => LunyLogger.LogWarning($"ONENABLE: Frame {Time.frameCount}", this);
		// private void Start() => LunyLogger.LogWarning($"START: Frame {Time.frameCount}", this);
		// private void Update() => LunyLogger.LogWarning($"UPDATE: Frame {Time.frameCount}", this);

		private void OnDestroy()
		{
			var scriptEngine = LunyScriptEngine.Instance;
			if (scriptEngine != null)
				scriptEngine.GlobalVariables.OnVariableChanged -= OnVariableChanged;
		}

		private void OnVariableChanged(Object sender, VariableChangedEventArgs changedVar)
		{
			//Debug.Log($"{e} ({sender})", this);

			var pass = changedVar.Variable.Boolean();
			if (changedVar.Name == nameof(Assert_Runs_WhenCreated))
				Assert_Runs_WhenCreated_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_WhenDestroyed))
				Assert_Runs_WhenDestroyed_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_WhenEnabled))
				Assert_Runs_WhenEnabled_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_WhenDisabled))
				Assert_Runs_WhenDisabled_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_WhenReady))
				Assert_Runs_WhenReady_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_EveryFixedStep))
				Assert_Runs_EveryFixedStep_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_EveryFrame))
				Assert_Runs_EveryFrame_Passed = pass;
			else if (changedVar.Name == nameof(Assert_Runs_EveryFrameEnds))
				Assert_Runs_EveryFrameEnds_Passed = pass;
			else
				throw new ArgumentOutOfRangeException(nameof(changedVar.Name));
		}
	}
}
