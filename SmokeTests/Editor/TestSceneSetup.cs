using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LunyScript.UnityEditor.SmokeTests
{
	//[PrebuildSetup(typeof(LifecycleTests))]
	internal  sealed class TestSceneSetup : IPrebuildSetup, IPostBuildCleanup
	{
		private const String TestScenePath = "Packages/de.codesmile.lunyscript/LunyScript.Unity/Tests/ObjectLifecycleTests.unity";

		private EditorBuildSettingsScene[] _buildScenes;

		public void Cleanup() => EditorBuildSettings.scenes = _buildScenes;

		public void Setup()
		{
			Debug.LogWarning("TEST SCENE SETUP");
			_buildScenes = EditorBuildSettings.scenes;
			EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene { path = TestScenePath } };
		}
	}

	// This runs globally for all PlayMode/Player tests
}
