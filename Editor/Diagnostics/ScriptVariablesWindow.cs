using LunyScript.Unity.UI;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptVariablesWindow : ScriptDiagnosticsWindow
	{
		private const String s_UxmlPath = "Packages/de.codesmile.lunyscript/LunyScript.Unity/UI/Documents/ScriptVariablesWindow.uxml";

		[MenuItem("Window/LunyScript/Script Variables")]
		private static void ShowWindow() => GetWindow<ScriptVariablesWindow>("LunyScript Variables");

		protected override String UxmlPath => s_UxmlPath;

		protected override IScriptDiagnosticsController CreateControllerInstance(VisualElement root) =>
			new ScriptVariablesWindowController(root);
	}
}
