using Luny;
using LunyScript.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LunyScript.UnityEditor
{
	internal sealed partial class LunyScriptVariablesEditor
	{
		private readonly List<InspectorReference> _refViewRows = new();

		private MultiColumnListView _referencesListView;
		private Button _btnRefAdd;
		private Button _btnRefRemove;

		private static Type GetObjectTypeForRefType(EngineReferenceType refType) => refType switch
		{
			EngineReferenceType.GameObject => typeof(GameObject),
			EngineReferenceType.ScriptableObject => typeof(ScriptableObject),

			EngineReferenceType.Transform => typeof(Transform),
			EngineReferenceType.Rigidbody => typeof(Rigidbody),
			EngineReferenceType.Material => typeof(Material),
			EngineReferenceType.Mesh => typeof(Mesh),
			EngineReferenceType.AudioClip => typeof(AudioClip),
			var _ => typeof(Object),
		};

		private void CreateReferencesGUI(VisualElement root)
		{
			_referencesListView = root.Q<MultiColumnListView>("references-list");
			_btnRefAdd = root.Q<Button>("btn-ref-add");
			_btnRefRemove = root.Q<Button>("btn-ref-remove");

			SetupReferencesListView();
			RefreshReferences();

			_btnRefAdd.clicked += OnRefAddClicked;
			_btnRefRemove.clicked += OnRefRemoveClicked;
		}

		private void OnReferencesSerializedObjectChanged(SerializedObject so)
		{
			var refsProp = so.FindProperty("_references");
			if (refsProp != null && refsProp.arraySize != _refViewRows.Count)
				RefreshReferences();
		}

		private void RefreshReferences()
		{
			_refViewRows.Clear();

			var component = (LunyScriptVariables)target;
			foreach (var r in component.References)
				_refViewRows.Add(r);

			_referencesListView.itemsSource = _refViewRows;
			_referencesListView.RefreshItems();
		}

		private void SetupReferencesListView()
		{
			var nameCol = _referencesListView.columns["ref-name"];
			nameCol.makeCell = () => new TextField { style = { flexGrow = 1 } };
			nameCol.bindCell = BindRefNameCell;
			nameCol.unbindCell = (element, _) => ((TextField)element).UnregisterValueChangedCallback(OnRefNameFieldChanged);

			var valueCol = _referencesListView.columns["ref-value"];
			valueCol.makeCell = () => new VisualElement { style = { flexGrow = 1 } };
			valueCol.bindCell = BindRefValueCell;
			valueCol.unbindCell = (element, _) => element.Clear();

			var typeCol = _referencesListView.columns["ref-type"];
			typeCol.makeCell = () => new VisualElement { style = { flexGrow = 1 } };
			typeCol.bindCell = BindRefTypeCell;
			typeCol.unbindCell = (element, _) => element.Clear();
		}

		private void BindRefNameCell(VisualElement element, Int32 index)
		{
			var tf = (TextField)element;
			tf.SetValueWithoutNotify(_refViewRows[index].Name ?? String.Empty);
			tf.isReadOnly = Application.isPlaying;
			tf.userData = index;

			if (!Application.isPlaying)
				tf.RegisterValueChangedCallback(OnRefNameFieldChanged);

			EnsureCellInputSelectsRow(_referencesListView, element, index);
		}

		private void OnRefNameFieldChanged(ChangeEvent<String> evt)
		{
			if (Application.isPlaying)
				return;

			var tf = (TextField)evt.target;
			if (tf?.userData is not Int32 index || index < 0 || index >= _refViewRows.Count)
				return;

			var so = serializedObject;
			so.Update();
			var refsProp = so.FindProperty("_references");
			var elem = refsProp.GetArrayElementAtIndex(index);
			elem.FindPropertyRelative(nameof(InspectorReference.Name)).stringValue = evt.newValue;
			so.ApplyModifiedProperties();
		}

		private void BindRefValueCell(VisualElement container, Int32 index)
		{
			container.Clear();

			var row = _refViewRows[index];
			VisualElement valueElement;

			switch (row.RefType)
			{
				case EngineReferenceType.Color:
					var cf = new ColorField { style = { flexGrow = 1 } };
					cf.SetValueWithoutNotify(row.ColorValue);
					cf.SetEnabled(!Application.isPlaying);
					cf.RegisterValueChangedCallback(evt => SetRefColorValue(index, evt.newValue));
					valueElement = cf;
					break;
				case EngineReferenceType.AnimationCurve:
					var curvef = new CurveField { style = { flexGrow = 1 } };
					curvef.SetValueWithoutNotify(row.CurveValue);
					curvef.SetEnabled(!Application.isPlaying);
					curvef.RegisterValueChangedCallback(evt => SetRefCurveValue(index, evt.newValue));
					valueElement = curvef;
					break;
				case EngineReferenceType.Vector3:
					var v3f = new Vector3Field { style = { flexGrow = 1 } };
					v3f.SetValueWithoutNotify(row.Vector3Value);
					v3f.SetEnabled(!Application.isPlaying);
					v3f.RegisterValueChangedCallback(evt => SetRefVector3Value(index, evt.newValue));
					valueElement = v3f;
					break;
				case EngineReferenceType.Rigidbody:
				case EngineReferenceType.Transform:
				case EngineReferenceType.Material:
				case EngineReferenceType.AudioClip:
				case EngineReferenceType.Component:
				case EngineReferenceType.GameObject:
				case EngineReferenceType.ScriptableObject:
				case EngineReferenceType.Object:
					var objField = new ObjectField { style = { flexGrow = 1 } };
					objField.objectType = GetObjectTypeForRefType(row.RefType);
					objField.allowSceneObjects = true;
					objField.SetValueWithoutNotify(row.RefValue);
					objField.SetEnabled(!Application.isPlaying);
					objField.RegisterValueChangedCallback(evt => SetRefObjectValue(index, evt.newValue));
					valueElement = objField;
					break;
				default:
					valueElement = new Label($"unhandled type: {row.RefType}");
					break;
			}

			container.Add(valueElement);
			EnsureCellInputSelectsRow(_referencesListView, container, index);
		}

		private void BindRefTypeCell(VisualElement container, Int32 index)
		{
			container.Clear();

			var row = _refViewRows[index];

			if (Application.isPlaying)
			{
				container.Add(new Label(row.RefType.ToString()) { style = { flexGrow = 1 } });
				return;
			}

			var enumField = new EnumField(row.RefType) { style = { flexGrow = 1 } };
			enumField.RegisterValueChangedCallback(evt =>
			{
				var newType = (EngineReferenceType)evt.newValue;
				Debug.LogWarning(newType);
				var so = serializedObject;
				so.Update();
				var refsProp = so.FindProperty("_references");
				var elem = refsProp.GetArrayElementAtIndex(index);
				elem.FindPropertyRelative(nameof(InspectorReference.RefType)).enumValueIndex = (Int32)newType;
				elem.FindPropertyRelative(nameof(InspectorReference.RefValue)).objectReferenceValue = null;
				so.ApplyModifiedProperties();
				RefreshReferences();
			});
			container.Add(enumField);

			EnsureCellInputSelectsRow(_referencesListView, container, index);
		}

		private void SetRefObjectValue(Int32 index, Object value)
		{
			var so = serializedObject;
			so.Update();
			var elem = so.FindProperty("_references").GetArrayElementAtIndex(index);
			elem.FindPropertyRelative(nameof(InspectorReference.RefValue)).objectReferenceValue = value;

			// Auto-fill name from object name if empty
			var nameProp = elem.FindPropertyRelative(nameof(InspectorReference.Name));
			if (String.IsNullOrEmpty(nameProp.stringValue) && value != null)
				nameProp.stringValue = value.name;

			so.ApplyModifiedProperties();
			RefreshReferences();
		}

		private void SetRefColorValue(Int32 index, Color value)
		{
			var so = serializedObject;
			so.Update();
			so.FindProperty("_references")
				.GetArrayElementAtIndex(index)
				.FindPropertyRelative(nameof(InspectorReference.ColorValue))
				.colorValue = value;
			so.ApplyModifiedProperties();
		}

		private void SetRefCurveValue(Int32 index, AnimationCurve value)
		{
			var so = serializedObject;
			so.Update();
			so.FindProperty("_references")
				.GetArrayElementAtIndex(index)
				.FindPropertyRelative(nameof(InspectorReference.CurveValue))
				.animationCurveValue = value;
			so.ApplyModifiedProperties();
		}

		private void SetRefVector3Value(Int32 index, Vector3 value)
		{
			var so = serializedObject;
			so.Update();
			so.FindProperty("_references")
				.GetArrayElementAtIndex(index)
				.FindPropertyRelative(nameof(InspectorReference.Vector3Value))
				.vector3Value = value;
			so.ApplyModifiedProperties();
		}

		private void OnRefAddClicked()
		{
			var so = serializedObject;
			so.Update();
			var refsProp = so.FindProperty("_references");
			refsProp.InsertArrayElementAtIndex(refsProp.arraySize);
			var newElem = refsProp.GetArrayElementAtIndex(refsProp.arraySize - 1);
			newElem.FindPropertyRelative(nameof(InspectorReference.Name)).stringValue = String.Empty;
			newElem.FindPropertyRelative(nameof(InspectorReference.RefType)).enumValueIndex = (Int32)EngineReferenceType.Object;
			newElem.FindPropertyRelative(nameof(InspectorReference.RefValue)).objectReferenceValue = null;
			so.ApplyModifiedProperties();
			RefreshReferences();

			_referencesListView.ClearSelection();
			_referencesListView.SetSelection(_refViewRows.Count - 1);
			_referencesListView.ScrollToItem(_refViewRows.Count - 1);
		}

		private void OnRefRemoveClicked()
		{
			var selectedCount = _referencesListView.selectedIndices.Count();
			if (selectedCount == 0)
				return;

			var so = serializedObject;
			so.Update();
			var refsProp = so.FindProperty("_references");
			var newSelectionIndex = -1;

			foreach (var index in _referencesListView.selectedIndices.OrderByDescending(i => i))
			{
				if (newSelectionIndex < 0)
					newSelectionIndex = index;
				refsProp.DeleteArrayElementAtIndex(index);
			}

			so.ApplyModifiedProperties();
			_referencesListView.ClearSelection();
			RefreshReferences();

			if (newSelectionIndex >= _refViewRows.Count)
				newSelectionIndex = _refViewRows.Count - 1;
			if (newSelectionIndex >= 0)
			{
				_referencesListView.SetSelection(newSelectionIndex);
				_referencesListView.ScrollToItem(newSelectionIndex);
			}
		}
	}
}
