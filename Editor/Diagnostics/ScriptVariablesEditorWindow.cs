using UnityEditor;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptVariablesEditorWindow : EditorWindow
	{
		private ScriptVariablesController _controller;

		[MenuItem("Window/LunyScript/Script Variables")]
		private static void ShowWindow() => GetWindow<ScriptVariablesEditorWindow>("Script Variables");

		private void CreateGUI()
		{
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
				"Packages/de.codesmile.lunyscript/LunyScript.Unity/UI/ScriptVariablesWindow.uxml");

			if (uxml != null)
				uxml.CloneTree(rootVisualElement);

			_controller = new ScriptVariablesController(rootVisualElement);
		}

		private void OnDestroy()
		{
			_controller?.Dispose();
			_controller = null;
		}
	}
}
