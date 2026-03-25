using LunyScript.Diagnostics;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal interface IScriptDiagnosticsController
	{
		void Reset();
		void OnSelectionChanged(GameObject go);
	}

	internal abstract class ScriptDiagnosticsEditorWindow : EditorWindow
	{
		private IScriptDiagnosticsController _controller;

		protected abstract String UxmlPath { get; }
		protected abstract IScriptDiagnosticsController CreateControllerInstance(VisualElement root);

		private void CreateGUI()
		{
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
			if (uxml == null)
				throw new MissingReferenceException($"Failed to load UI Document: {UxmlPath}");

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

				_controller = CreateControllerInstance(rootVisualElement);
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

		protected void UpdateControllerTargetObject() => _controller?.OnSelectionChanged(Selection.activeGameObject);
		protected void ResetController() => _controller?.Reset();

		protected virtual void OnPlayModeStateChanged(PlayModeStateChange state)
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
