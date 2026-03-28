using LunyScript.Unity.UI;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptBlocksWindow : ScriptDiagnosticsWindow
	{
		private const String s_UxmlPath = "Packages/de.codesmile.lunyscript/LunyScript.Unity/UI/Documents/ScriptBlocksWindow.uxml";
		private const String WindowTitle = "Block Inspector";

		private ScriptBlocksWindowController _blocksController;

		protected override String UxmlPath => s_UxmlPath;

		[MenuItem("Window/" + nameof(LunyScript) + "/" + WindowTitle)]
		private static void ShowWindow() => GetWindow<ScriptBlocksWindow>(WindowTitle + " [" + nameof(LunyScript) + "]");

		private void Update() => _blocksController?.OnEditorUpdate();

		protected override IScriptDiagnosticsController CreateControllerInstance(VisualElement root) =>
			_blocksController = new ScriptBlocksWindowController(root);
	}
}
