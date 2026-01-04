/*
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LunyScript.Unity.Tests
{
	public class LifecycleTests
	{
		private IVariables _globalVars;

		[SetUp]
		public void Setup()
		{
			Debug.LogWarning("SETUP");
			SceneManager.LoadScene(0);

			_globalVars = LunyScriptEngine.Instance.GlobalVariables;
		}

		[TearDown]
		public void Teardown() {}

		[UnityTest]
		public IEnumerator Assert_Runs_WhenCreated()
		{
			Debug.LogWarning("TEST START");
			yield return null;
			Debug.LogWarning("yield 1");
			yield return null;
			Debug.LogWarning("yield 2");
			yield return null;
			Debug.LogWarning("yield 3");

			var vars = LunyScriptEngine.Instance.GlobalVariables;
			Assert.That(vars[nameof(Assert_Runs_WhenCreated)].Boolean, Is.True);
		}
	}
}
*/


