using Luny;
using LunyScript.Unity;
using System;
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
		private MultiColumnListView _referencesListView;
		private Button _btnRefAdd;
		private Button _btnRefRemove;

		private Int32 _refCount;

		private static Type GetObjectTypeForRefType(EngineReferenceType refType) => refType switch
		{
			EngineReferenceType.GameObject => typeof(GameObject),
			EngineReferenceType.ScriptableObject => typeof(ScriptableObject),
			EngineReferenceType.Component => typeof(Component),

			EngineReferenceType.Transform => typeof(Transform),
			EngineReferenceType.Rigidbody => typeof(Rigidbody),
			EngineReferenceType.Material => typeof(Material),
			EngineReferenceType.Mesh => typeof(Mesh),
			EngineReferenceType.AudioClip => typeof(AudioClip),
			var _ => typeof(Object),
		};

		private SerializedProperty GetRefsProp() => serializedObject.FindProperty("_references");

		private SerializedProperty GetRefElemProp(Int32 index) => GetRefsProp().GetArrayElementAtIndex(index);

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
			if (refsProp != null && refsProp.arraySize != _refCount)
				RefreshReferences();
		}

		private void RefreshReferences()
		{
			serializedObject.Update();
			_refCount = GetRefsProp().arraySize;

			_referencesListView.itemsSource = Enumerable.Range(0, _refCount).ToList();
			_referencesListView.RefreshItems();
		}

		private void SetupReferencesListView()
		{
			var nameCol = _referencesListView.columns["ref-name"];
			nameCol.makeCell = () => new TextField { style = { flexGrow = 1 } };
			nameCol.bindCell = BindRefNameCell;
			nameCol.unbindCell = (element, _) => ((TextField)element).Unbind();

			var valueCol = _referencesListView.columns["ref-value"];
			valueCol.makeCell = () => new VisualElement { style = { flexGrow = 1 } };
			valueCol.bindCell = BindRefValueCell;
			valueCol.unbindCell = (element, _) =>
			{
				element.Q<BindableElement>()?.Unbind();
				element.Clear();
			};

			var typeCol = _referencesListView.columns["ref-type"];
			typeCol.makeCell = () => new VisualElement { style = { flexGrow = 1 } };
			typeCol.bindCell = BindRefTypeCell;
			typeCol.unbindCell = (element, _) =>
			{
				element.Q<BindableElement>()?.Unbind();
				element.Clear();
			};
		}

		private void BindRefNameCell(VisualElement element, Int32 index)
		{
			var tf = (TextField)element;
			tf.isReadOnly = Application.isPlaying;

			var nameProp = GetRefElemProp(index).FindPropertyRelative(nameof(InspectorReference.Name));
			if (Application.isPlaying)
				tf.SetValueWithoutNotify(nameProp.stringValue ?? String.Empty);
			else
				tf.BindProperty(nameProp);

			EnsureCellInputSelectsRow(_referencesListView, element, index);
		}

		private void BindRefValueCell(VisualElement container, Int32 index)
		{
			container.Clear();

			var elem = GetRefElemProp(index);
			var refTypeProp = elem.FindPropertyRelative(nameof(InspectorReference.RefType));
			var refType = (EngineReferenceType)refTypeProp.intValue;

			VisualElement valueElement;

			switch (refType)
			{
				case EngineReferenceType.Color:
					var cf = new ColorField { style = { flexGrow = 1 } };
					cf.BindProperty(elem.FindPropertyRelative(nameof(InspectorReference.ColorValue)));
					valueElement = cf;
					break;
				case EngineReferenceType.AnimationCurve:
					var curvef = new CurveField { style = { flexGrow = 1 } };
					curvef.BindProperty(elem.FindPropertyRelative(nameof(InspectorReference.CurveValue)));
					valueElement = curvef;
					break;
				case EngineReferenceType.Vector2:
					var v2f = new Vector2Field { style = { flexGrow = 1 } };
					v2f.BindProperty(elem.FindPropertyRelative(nameof(InspectorReference.Vector2Value)));
					valueElement = v2f;
					break;
				case EngineReferenceType.Vector3:
					var v3f = new Vector3Field { style = { flexGrow = 1 } };
					v3f.BindProperty(elem.FindPropertyRelative(nameof(InspectorReference.Vector3Value)));
					valueElement = v3f;
					break;
				default:
					var objField = new ObjectField { style = { flexGrow = 1 } };
					objField.objectType = GetObjectTypeForRefType(refType);
					objField.allowSceneObjects = refType == EngineReferenceType.GameObject ||
					                             refType == EngineReferenceType.Component;
					objField.BindProperty(elem.FindPropertyRelative(nameof(InspectorReference.RefValue)));
					objField.RegisterValueChangedCallback(evt => AutoFillRefName(index, evt.previousValue, evt.newValue));
					valueElement = objField;
					break;
			}

			valueElement.SetEnabled(!Application.isPlaying);
			container.Add(valueElement);
			EnsureCellInputSelectsRow(_referencesListView, container, index);
		}

		private void AutoFillRefName(Int32 index, Object prevValue, Object newValue)
		{
			if (Application.isPlaying)
				return;

			var so = serializedObject;
			so.Update();
			var nameProp = GetRefElemProp(index).FindPropertyRelative(nameof(InspectorReference.Name));

			// Auto-fill name only when the name field is empty
			var currentName = nameProp.stringValue;
			if (String.IsNullOrEmpty(currentName) || prevValue != null && prevValue.name.Equals(currentName))
			{
				if (newValue != null)
					nameProp.stringValue = newValue.name;
			}

			so.ApplyModifiedProperties();
		}

		private void BindRefTypeCell(VisualElement container, Int32 index)
		{
			container.Clear();

			var refTypeProp = GetRefElemProp(index).FindPropertyRelative(nameof(InspectorReference.RefType));
			var refType = (EngineReferenceType)refTypeProp.intValue;

			if (Application.isPlaying)
			{
				container.Add(new Label(refType.ToString()) { style = { flexGrow = 1 } });
				return;
			}

			var enumField = new EnumField(refType) { style = { flexGrow = 1 } };
			enumField.RegisterValueChangedCallback(evt =>
			{
				var newType = (EngineReferenceType)evt.newValue;
				var so = serializedObject;
				so.Update();
				var elem = GetRefElemProp(index);
				elem.FindPropertyRelative(nameof(InspectorReference.RefType)).intValue = (Int32)newType;
				elem.FindPropertyRelative(nameof(InspectorReference.RefValue)).objectReferenceValue = null;
				so.ApplyModifiedProperties();
				RefreshReferences();
			});
			container.Add(enumField);

			EnsureCellInputSelectsRow(_referencesListView, container, index);
		}

		private void OnRefAddClicked()
		{
			var so = serializedObject;
			so.Update();
			var refsProp = GetRefsProp();
			refsProp.InsertArrayElementAtIndex(refsProp.arraySize);
			var newElem = refsProp.GetArrayElementAtIndex(refsProp.arraySize - 1);
			newElem.FindPropertyRelative(nameof(InspectorReference.Name)).stringValue = String.Empty;
			newElem.FindPropertyRelative(nameof(InspectorReference.RefType)).intValue = (Int32)EngineReferenceType.Object;
			newElem.FindPropertyRelative(nameof(InspectorReference.RefValue)).objectReferenceValue = null;
			so.ApplyModifiedProperties();
			RefreshReferences();

			_referencesListView.ClearSelection();
			_referencesListView.SetSelection(_refCount - 1);
			_referencesListView.ScrollToItem(_refCount - 1);
		}

		private void OnRefRemoveClicked()
		{
			var selectedCount = _referencesListView.selectedIndices.Count();
			if (selectedCount == 0)
				return;

			var so = serializedObject;
			so.Update();
			var refsProp = GetRefsProp();
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

			if (newSelectionIndex >= _refCount)
				newSelectionIndex = _refCount - 1;
			if (newSelectionIndex >= 0)
			{
				_referencesListView.SetSelection(newSelectionIndex);
				_referencesListView.ScrollToItem(newSelectionIndex);
			}
		}
	}
}
