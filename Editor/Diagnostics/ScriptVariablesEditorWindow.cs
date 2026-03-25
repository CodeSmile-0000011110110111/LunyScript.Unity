using LunyScript.Diagnostics;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptVariablesEditorWindow : EditorWindow
	{
		private const String s_UxmlPath = "Packages/de.codesmile.lunyscript/LunyScript.Unity/UI/ScriptVariablesWindow.uxml";
		private ScriptVariablesController _controller;

		[MenuItem("Window/LunyScript/Script Variables")]
		private static void ShowWindow() => GetWindow<ScriptVariablesEditorWindow>("LunyScript Variables");

		private void CreateGUI()
		{
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);
			if (uxml == null)
				throw new MissingReferenceException($"Failed to load UI Document: {s_UxmlPath}");

			uxml.CloneTree(rootVisualElement);
			CreateController();
			UpdateControllerTargetObject();
		}

		private void OnEnable() => UpdateControllerTargetObject();

		private void OnDisable() => ResetController();

		private void OnDestroy()
		{
			ScriptDiagnosticsObserver.OnDiagnosticsStartup -= OnDiagnosticsStartup;
			ScriptDiagnosticsObserver.OnDiagnosticsShutdown -= OnDiagnosticsShutdown;
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			Selection.selectionChanged -= UpdateControllerTargetObject;

			_controller = null;
		}

		private void CreateController()
		{
			if (ScriptDiagnosticsObserver.Instance == null)
				ScriptDiagnosticsObserver.OnDiagnosticsStartup += OnDiagnosticsStartup;
			else
			{
				ScriptDiagnosticsObserver.OnDiagnosticsStartup -= OnDiagnosticsStartup;
				ScriptDiagnosticsObserver.OnDiagnosticsShutdown += OnDiagnosticsShutdown;
				EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
				Selection.selectionChanged += UpdateControllerTargetObject;

				_controller = new ScriptVariablesController(rootVisualElement);
			}
		}

		private void OnDiagnosticsStartup(ScriptDiagnosticsObserver _)
		{
			CreateController();
			UpdateControllerTargetObject();
		}

		private void OnDiagnosticsShutdown(ScriptDiagnosticsObserver _)
		{
			ScriptDiagnosticsObserver.OnDiagnosticsShutdown -= OnDiagnosticsShutdown;
			ResetController();
		}

		private void UpdateControllerTargetObject() => _controller?.OnSelectionChanged(Selection.activeGameObject);
		private void ResetController() => _controller?.Reset();

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			switch (state)
			{
				case PlayModeStateChange.EnteredPlayMode:
					UpdateControllerTargetObject();
					break;
				case PlayModeStateChange.ExitingPlayMode:
					ResetController();
					break;
			}
		}
	}
}
