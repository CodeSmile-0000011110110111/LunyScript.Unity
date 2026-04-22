using LunyScript.Unity;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace LunyScript.UnityEditor
{
	[CustomEditor(typeof(LunyScriptVariables))]
	internal sealed partial class LunyScriptVariablesEditor : Editor
	{
		private static readonly String k_UxmlPath =
			"Packages/de.codesmile.lunyscript/LunyScript.Unity/UI/Documents/ScriptVariablesInspector.uxml";

		private VisualElement _root;

		private void Awake() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

		private void OnDisable() => OnVariablesDisable();

		private void OnDestroy() => EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			ScriptEngine.OnScriptEngineInitialized -= OnScriptEngineInitialized;
			if (state == PlayModeStateChange.EnteredPlayMode)
			{
				if (ScriptEngine.Instance == null)
					ScriptEngine.OnScriptEngineInitialized += OnScriptEngineInitialized;
				else
					RefreshVariables();
			}
		}

		private void OnScriptEngineInitialized(IScriptEngine obj)
		{
			ScriptEngine.OnScriptEngineInitialized -= OnScriptEngineInitialized;
			EditorApplication.delayCall += () => RefreshVariables();
		}

		public override VisualElement CreateInspectorGUI()
		{
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
			_root = uxml != null ? uxml.Instantiate() : new VisualElement();

			CreateVariablesGUI(_root);
			CreateReferencesGUI(_root);

			// Edit-mode: detect external changes (e.g. Reset()) via serialized object tracking
			_root.TrackSerializedObjectValue(serializedObject, OnSerializedObjectChanged);

			return _root;
		}

		private void OnSerializedObjectChanged(SerializedObject so)
		{
			if (Application.isPlaying)
				return;

			OnVariablesSerializedObjectChanged(so);
			OnReferencesSerializedObjectChanged(so);
		}

		private void EnsureCellInputSelectsRow(MultiColumnListView listView, VisualElement element, Int32 index) =>
			element.RegisterCallback<PointerDownEvent>(evt =>
			{
				if (listView.selectedIndex != index)
					listView.SetSelection(index);
			}, TrickleDown.TrickleDown);
	}
}
