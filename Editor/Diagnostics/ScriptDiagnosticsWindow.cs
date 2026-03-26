using LunyScript.Diagnostics;
using LunyScript.Unity.UI;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal abstract class ScriptDiagnosticsWindow : EditorWindow
	{
		private IScriptDiagnosticsController _controller;

		protected abstract String UxmlPath { get; }

		private void CreateGUI()
		{
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
			if (uxml == null)
				throw new MissingReferenceException($"Failed to load UI Document: {UxmlPath}");

			uxml.CloneTree(rootVisualElement);
			CreateController();
		}

		private void OnEnable()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			Selection.selectionChanged += UpdateController;
			UpdateController();
		}

		private void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			Selection.selectionChanged -= UpdateController;
			ResetController();
		}

		private void OnDestroy()
		{
			ScriptDiagnosticsObserver.OnDiagnosticsStartup -= OnDiagnosticsStartup;
			ScriptDiagnosticsObserver.OnDiagnosticsShutdown -= OnDiagnosticsShutdown;

			_controller = null;
		}

		protected abstract IScriptDiagnosticsController CreateControllerInstance(VisualElement root);

		private void CreateController()
		{
			ScriptDiagnosticsObserver.OnDiagnosticsStartup -= OnDiagnosticsStartup;
			if (ScriptDiagnosticsObserver.Instance == null)
				ScriptDiagnosticsObserver.OnDiagnosticsStartup += OnDiagnosticsStartup;
			else
				ScriptDiagnosticsObserver.OnDiagnosticsShutdown += OnDiagnosticsShutdown;

			_controller = CreateControllerInstance(rootVisualElement);
			UpdateController();
		}

		private void OnDiagnosticsStartup(ScriptDiagnosticsObserver _) => CreateController();

		private void OnDiagnosticsShutdown(ScriptDiagnosticsObserver _)
		{
			ScriptDiagnosticsObserver.OnDiagnosticsShutdown -= OnDiagnosticsShutdown;
			ResetController();
		}

		protected void UpdateController() => _controller?.SetTargetObject(Selection.activeGameObject);
		protected void ResetController() => _controller?.Reset();

		protected virtual void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			switch (state)
			{
				case PlayModeStateChange.ExitingPlayMode:
				case PlayModeStateChange.EnteredEditMode:
					ResetController();
					break;
			}

			UpdateController();
		}
	}
}
