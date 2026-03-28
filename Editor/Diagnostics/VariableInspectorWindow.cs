using LunyScript.Unity.UI;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptVariablesWindow : ScriptDiagnosticsWindow
	{
		private const String s_UxmlPath = "Packages/de.codesmile.lunyscript/LunyScript.Unity/UI/Documents/ScriptVariablesWindow.uxml";
		private const String WindowTitle = "Variable Inspector";

		protected override String UxmlPath => s_UxmlPath;

		[MenuItem("Window/" + nameof(LunyScript) + "/" + WindowTitle)]
		private static void ShowWindow() => GetWindow<ScriptVariablesWindow>(WindowTitle + " [" + nameof(LunyScript) + "]");

		protected override IScriptDiagnosticsController CreateControllerInstance(VisualElement root) =>
			new ScriptVariablesWindowController(root);
	}
}
