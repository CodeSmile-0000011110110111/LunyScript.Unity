using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;

namespace LunyScript.UnityEditor.SmokeTests
{
	internal  class GlobalTestSceneSetup : ITestPlayerBuildModifier
	{
		public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
		{
			Debug.LogWarning("TEST SCENE ModifyOptions");

			return playerOptions;
		}
	}
}
