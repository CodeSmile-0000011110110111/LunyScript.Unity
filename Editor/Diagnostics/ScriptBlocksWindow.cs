using LunyScript.Unity.UI;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptBlocksWindow : ScriptDiagnosticsWindow
	{
		private const String s_UxmlPath = "Packages/de.codesmile.lunyscript/LunyScript.Unity/UI/Documents/ScriptBlocksWindow.uxml";
		private ScriptBlocksWindowController _blocksController;

		protected override String UxmlPath => s_UxmlPath;

		[MenuItem("Window/LunyScript/Script Blocks")]
		private static void ShowWindow() => GetWindow<ScriptBlocksWindow>("LunyScript Blocks");

		private void Update() => _blocksController?.OnEditorUpdate();

		protected override IScriptDiagnosticsController CreateControllerInstance(VisualElement root) =>
			_blocksController = new ScriptBlocksWindowController(root);
	}
}
