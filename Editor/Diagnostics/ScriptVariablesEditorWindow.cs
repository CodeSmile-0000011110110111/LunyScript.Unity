using Luny;
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
			LunyLogger.LogWarning("CreateGUI", this);

			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);
			if (uxml == null)
				throw new MissingReferenceException($"Failed to load UI Document: {s_UxmlPath}");

			uxml.CloneTree(rootVisualElement);
			_controller = new ScriptVariablesController(rootVisualElement);
			_controller?.OnEnable();
		}

		private void Awake() => EditorApplication.playModeStateChanged += OnPlayModeStateChange;

		private void OnEnable()
		{
			LunyLogger.LogWarning("OnEnable", this);
			_controller?.OnEnable();
		}

		private void OnDisable()
		{
			LunyLogger.LogWarning("OnDisable", this);
			_controller?.OnDisable();
		}

		private void OnDestroy()
		{
			LunyLogger.LogWarning("OnDestroy", this);
			_controller?.Dispose();
			_controller = null;

			EditorApplication.playModeStateChanged -= OnPlayModeStateChange;
		}

		private void OnPlayModeStateChange(PlayModeStateChange state)
		{
			switch (state)
			{
				case PlayModeStateChange.EnteredPlayMode:
					_controller?.OnEnable();
					break;
				case PlayModeStateChange.ExitingPlayMode:
					_controller?.OnDisable();
					break;
			}
		}
	}
}
