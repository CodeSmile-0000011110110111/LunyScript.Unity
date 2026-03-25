using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptVariablesEditorWindow : ScriptDiagnosticsEditorWindow
	{
		private const String s_UxmlPath = "Packages/de.codesmile.lunyscript/LunyScript.Unity/UI/ScriptVariablesWindow.uxml";

		[MenuItem("Window/LunyScript/Script Variables")]
		private static void ShowWindow() => GetWindow<ScriptVariablesEditorWindow>("LunyScript Variables");

		protected override String UxmlPath => s_UxmlPath;

		protected override IScriptDiagnosticsController CreateControllerInstance(VisualElement root) =>
			new ScriptVariablesController(root);
	}
}
