using System;
using UnityEngine;
using Object = System.Object;

namespace LunyScript.Unity.Tests
{
	[DefaultExecutionOrder(-1000)]
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

		private void Awake() => LunyScriptEngine.Instance.GlobalVariables.OnVariableChanged += OnVariableChanged;

		private void OnVariableChanged(Object sender, VariableChangedEventArgs e)
		{
			Debug.Log($"{nameof(ObjectLifecycleTestController)}: {e}");

			var pass = e.Variable.Boolean();
			if (e.Name == nameof(Assert_Runs_WhenCreated))
				Assert_Runs_WhenCreated_Passed = pass;
			else if (e.Name == nameof(Assert_Runs_WhenDestroyed))
				Assert_Runs_WhenDestroyed_Passed = pass;
			else if (e.Name == nameof(Assert_Runs_WhenEnabled))
				Assert_Runs_WhenEnabled_Passed = pass;
			else if (e.Name == nameof(Assert_Runs_WhenDisabled))
				Assert_Runs_WhenDisabled_Passed = pass;
			else if (e.Name == nameof(Assert_Runs_WhenReady))
				Assert_Runs_WhenReady_Passed = pass;
			else if (e.Name == nameof(Assert_Runs_EveryFixedStep))
				Assert_Runs_EveryFixedStep_Passed = pass;
			else if (e.Name == nameof(Assert_Runs_EveryFrame))
				Assert_Runs_EveryFrame_Passed = pass;
			else if (e.Name == nameof(Assert_Runs_EveryFrameEnds))
				Assert_Runs_EveryFrameEnds_Passed = pass;
			else
				throw new ArgumentOutOfRangeException(nameof(e.Name));
		}
	}
}
