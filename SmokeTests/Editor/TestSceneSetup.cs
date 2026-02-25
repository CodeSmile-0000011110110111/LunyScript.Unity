using System;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.TestTools;

namespace SmokeTests.Editor
{
	//[PrebuildSetup(typeof(LifecycleTests))]
	public sealed class TestSceneSetup : IPrebuildSetup, IPostBuildCleanup
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
	public class GlobalTestSceneSetup : ITestPlayerBuildModifier
	{
		public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
		{
			Debug.LogWarning("TEST SCENE ModifyOptions");

			return playerOptions;
		}
	}
}
