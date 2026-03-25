using LunyScript.Diagnostics;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptBlocksEditorWindow : EditorWindow
	{
		private const String s_UxmlPath = "Packages/de.codesmile.lunyscript/LunyScript.Unity/UI/ScriptBlocksWindow.uxml";
		private ScriptBlocksController _controller;

		[MenuItem("Window/LunyScript/Script Blocks")]
		private static void ShowWindow() => GetWindow<ScriptBlocksEditorWindow>("LunyScript Blocks");

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

		private void Update() => _controller?.OnEditorUpdate();

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

				_controller = new ScriptBlocksController(rootVisualElement);
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
				case PlayModeStateChange.EnteredEditMode:
					ResetController();
					break;
			}
		}
	}
}
