using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptBlocksEditorWindow : ScriptDiagnosticsEditorWindow
	{
		private const String s_UxmlPath = "Packages/de.codesmile.lunyscript/LunyScript.Unity/UI/ScriptBlocksWindow.uxml";
		private ScriptBlocksController _blocksController;

		[MenuItem("Window/LunyScript/Script Blocks")]
		private static void ShowWindow() => GetWindow<ScriptBlocksEditorWindow>("LunyScript Blocks");

		protected override String UxmlPath => s_UxmlPath;

		protected override IScriptDiagnosticsController CreateControllerInstance(VisualElement root)
		{
			_blocksController = new ScriptBlocksController(root);
			return _blocksController;
		}

		private void Update() => _blocksController?.OnEditorUpdate();

		protected override void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			base.OnPlayModeStateChanged(state);

			switch (state)
			{
				case PlayModeStateChange.EnteredEditMode:
					ResetController();
					break;
			}
		}
	}
}
