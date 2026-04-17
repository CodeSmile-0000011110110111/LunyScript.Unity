using Luny;
using LunyScript.Diagnostics;
using LunyScript.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace LunyScript.UnityEditor
{
	[CustomEditor(typeof(LunyScriptVariables))]
	internal sealed class LunyScriptVariablesEditor : Editor
	{
		private static readonly String k_UxmlPath =
			"Packages/de.codesmile.lunyscript/LunyScript.Unity/UI/Documents/ScriptVariablesInspector.uxml";

		private readonly List<ScriptVariableState> _viewRows = new();

		private MultiColumnListView _listView;
		private Button _btnAdd;
		private Button _btnRemove;
		private VisualElement _root;

		private ScriptVariableState[] _masterRows;
		private String _focusedRowName;
		private ITable _table;

		private static Int32 GetValueTypeOrdinal(ScriptVariableState row) => row.ValueTypeOrdinal;

		private void Awake() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

		private void OnDisable()
		{
			UnsubscribeFromTable();
			_focusedRowName = null;
		}

		private void OnDestroy() => EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			ScriptEngine.OnScriptEngineInitialized -= OnScriptEngineInitialized;
			if (state == PlayModeStateChange.EnteredPlayMode)
			{
				if (ScriptEngine.Instance == null)
					ScriptEngine.OnScriptEngineInitialized += OnScriptEngineInitialized;
				else
					Refresh();
			}
		}

		private void OnScriptEngineInitialized(IScriptEngine obj)
		{
			ScriptEngine.OnScriptEngineInitialized -= OnScriptEngineInitialized;
			EditorApplication.delayCall += () => Refresh();
		}

		public override VisualElement CreateInspectorGUI()
		{
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
			_root = uxml != null ? uxml.Instantiate() : new VisualElement();

			_listView = _root.Q<MultiColumnListView>("variables-list");
			_btnAdd = _root.Q<Button>("btn-add");
			_btnRemove = _root.Q<Button>("btn-remove");

			SetupListView();
			Refresh();
			UpdateEmptyState();

			_btnAdd.clicked += OnAddClicked;
			_btnRemove.clicked += OnRemoveClicked;

			// Play-mode: refresh every 500ms to pick up script-defined variables
			_root.schedule.Execute(OnEditorUpdate).Every(500);

			// Edit-mode: detect external changes (e.g. Reset()) via serialized object tracking
			_root.TrackSerializedObjectValue(serializedObject, OnSerializedObjectChanged);

			return _root;
		}

		private void OnEditorUpdate()
		{
			// if (Application.isPlaying)
			// 	Refresh();
		}

		private void OnSerializedObjectChanged(SerializedObject so)
		{
			if (Application.isPlaying)
				return;

			var varsProp = so.FindProperty("_variables");
			if (varsProp != null && varsProp.arraySize != _viewRows.Count)
				Refresh();
		}

		private void Refresh()
		{
			UnsubscribeFromTable();

			var component = (LunyScriptVariables)target;
			if (!Application.isPlaying)
				component.ResetTable();
			_table = component.Table;

			Debug.Log($"Table: {_table} Target: {component.name} ({component.GetEntityId()}) on {component.gameObject.name} ({component.gameObject.GetEntityId()})");

			if (_table != null)
			{
				_masterRows = BuildMasterRows(_table);
				_table.OnVariableChanged += OnVariableChanged;
			}
			else
				_masterRows = null;

			RebuildView();
			UpdateEmptyState();
		}

		private ScriptVariableState[] BuildMasterRows(ITable table)
		{
			var keys = new List<String>();
			foreach (var kvp in table)
				keys.Add(kvp.Key);

			var rows = new ScriptVariableState[keys.Count];
			for (var i = 0; i < keys.Count; i++)
				rows[i] = new ScriptVariableState(table.GetHandle(keys[i]));
			return rows;
		}

		private void OnVariableChanged(Object sender, VariableChangedArgs args)
		{
			if (_masterRows == null)
				return;

			// Skip refresh while the user is actively editing this field — prevents focus loss
			if (args.Name == _focusedRowName)
				return;

			for (var i = 0; i < _viewRows.Count; i++)
			{
				if (!_viewRows[i].HasName(args.Name))
					continue;

				_listView.RefreshItem(i);
				break;
			}
		}

		private void RebuildView()
		{
			//Debug.LogWarning(nameof(RebuildView));

			_viewRows.Clear();

			if (_masterRows != null)
			{
				foreach (var row in _masterRows)
					_viewRows.Add(row);
			}

			ApplySort();
			_listView.itemsSource = _viewRows;
			_listView.RefreshItems();
		}

		private void ApplySort()
		{
			var sortDescs = _listView.sortColumnDescriptions;
			if (sortDescs == null || sortDescs.Count == 0)
				return;

			var desc = sortDescs[0];
			var ascending = desc.direction == SortDirection.Ascending;

			if (desc.columnName == "name")
				_viewRows.Sort((a, b) => ascending ? a.CompareNameTo(b) : b.CompareNameTo(a));
			else if (desc.columnName == "value" || desc.columnName == "type")
			{
				_viewRows.Sort((a, b) =>
				{
					var typeCompare = GetValueTypeOrdinal(a).CompareTo(GetValueTypeOrdinal(b));
					if (typeCompare != 0)
						return ascending ? typeCompare : -typeCompare;

					return ascending ? a.CompareNameTo(b) : b.CompareNameTo(a);
				});
			}
		}

		private void SetupListView()
		{
			_listView.columnSortingChanged += OnColumnSortingChanged;

			var nameCol = _listView.columns["name"];
			nameCol.makeCell = MakeNameCell;
			nameCol.bindCell = BindNameCell;
			nameCol.unbindCell = UnbindNameCell;

			var valueCol = _listView.columns["value"];
			valueCol.makeCell = () => new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row } };
			valueCol.bindCell = BindValueCell;
			valueCol.unbindCell = (element, _) => element.Clear();

			var typeCol = _listView.columns["type"];
			typeCol.makeCell = () => new VisualElement { style = { flexGrow = 1 } };
			typeCol.bindCell = BindTypeCell;
			typeCol.unbindCell = (element, _) => element.Clear();
		}

		private VisualElement MakeNameCell() => new TextField { style = { flexGrow = 1 } };

		private void BindNameCell(VisualElement element, Int32 index)
		{
			var row = _viewRows[index];
			var tf = (TextField)element;
			tf.SetValueWithoutNotify(row.Name);
			tf.isReadOnly = Application.isPlaying;
			element.EnableInClassList("constant-row", row.IsConstant);
			tf.userData = index;

			if (!Application.isPlaying)
				tf.RegisterValueChangedCallback(OnNameFieldChanged);

			EnsureCellInputSelectsRow(element, index);
		}

		private void UnbindNameCell(VisualElement element, Int32 index)
		{
			var tf = (TextField)element;
			tf.UnregisterValueChangedCallback(OnNameFieldChanged);
		}

		private void OnNameFieldChanged(ChangeEvent<String> evt)
		{
			if (Application.isPlaying)
				return;

			var tf = (TextField)evt.target;
			if (tf?.userData is not Int32 index || index < 0 || index >= _viewRows.Count)
				return;

			var row = _viewRows[index];
			var so = serializedObject;
			so.Update();
			var varsProp = so.FindProperty("_variables");
			for (var i = 0; i < varsProp.arraySize; i++)
			{
				var elem = varsProp.GetArrayElementAtIndex(i);
				if (elem.FindPropertyRelative("Name").stringValue != row.Name)
					continue;

				elem.FindPropertyRelative("Name").stringValue = evt.newValue;
				break;
			}
			so.ApplyModifiedProperties();
			Refresh();
		}

		private void BindValueCell(VisualElement container, Int32 index)
		{
			container.Clear();

			var row = _viewRows[index];
			if (!row.TryGetVarHandle(out var varHandle))
				return;

			var variable = varHandle.Variable;
			VisualElement valueElement;

			switch (variable.Type)
			{
				case Variable.ValueType.Boolean:
					var toggle = new Toggle { style = { flexGrow = 1 } };
					toggle.SetValueWithoutNotify(variable.AsBoolean());
					toggle.SetEnabled(!row.IsConstant);
					toggle.RegisterValueChangedCallback(evt =>
					{
						varHandle.Variable = evt.newValue;
						SyncToInspectorVariable(row.Name, varHandle.Variable);
					});
					valueElement = toggle;
					break;
				case Variable.ValueType.Number:
					var df = new DoubleField { style = { flexGrow = 1 } };
					df.SetValueWithoutNotify(variable.AsDouble());
					df.SetEnabled(!row.IsConstant);
					df.RegisterCallback<FocusInEvent>(_ => _focusedRowName = row.Name);
					df.RegisterCallback<FocusOutEvent>(_ =>
					{
						_focusedRowName = null;
						SyncToInspectorVariable(row.Name, varHandle.Variable);
					});
					df.RegisterValueChangedCallback(evt => varHandle.Variable = evt.newValue);
					valueElement = df;
					break;
				default: // String
					var tf = new TextField { style = { flexGrow = 1 } };
					tf.SetValueWithoutNotify(variable.AsString());
					tf.SetEnabled(!row.IsConstant);
					tf.RegisterCallback<FocusInEvent>(_ => _focusedRowName = row.Name);
					tf.RegisterCallback<FocusOutEvent>(_ =>
					{
						_focusedRowName = null;
						SyncToInspectorVariable(row.Name, varHandle.Variable);
					});
					tf.RegisterValueChangedCallback(evt =>
					{
						Debug.Log(
							$"[DEBUG_LOG] TextField value changed — name={row.Name}, newValue={evt.newValue}, focusController={tf.focusController != null}");
						varHandle.Variable = evt.newValue;
					});
					valueElement = tf;
					break;
			}

			container.Add(valueElement);
			container.EnableInClassList("constant-row", row.IsConstant);

			EnsureCellInputSelectsRow(container, index);
		}

		private void BindTypeCell(VisualElement container, Int32 index)
		{
			container.Clear();

			var row = _viewRows[index];
			if (!row.TryGetVarHandle(out var varHandle))
				return;

			if (Application.isPlaying)
			{
				var label = new Label(varHandle.Variable.Type.ToString()) { style = { flexGrow = 1 } };
				label.EnableInClassList("constant-row", row.IsConstant);
				container.Add(label);
				return;
			}

			var enumField = new EnumField(varHandle.Variable.Type) { style = { flexGrow = 1 } };
			enumField.SetEnabled(!row.IsConstant);
			enumField.RegisterValueChangedCallback(evt =>
			{
				var newType = (Variable.ValueType)evt.newValue;
				var so = serializedObject;
				so.Update();
				var varsProp = so.FindProperty("_variables");
				for (var i = 0; i < varsProp.arraySize; i++)
				{
					var elem = varsProp.GetArrayElementAtIndex(i);
					if (elem.FindPropertyRelative("Name").stringValue != row.Name)
						continue;

					elem.FindPropertyRelative("Type").enumValueIndex = (Int32)newType;
					elem.FindPropertyRelative("BoolValue").boolValue = false;
					elem.FindPropertyRelative("NumberValue").doubleValue = 0.0;
					elem.FindPropertyRelative("TextValue").stringValue = String.Empty;
					break;
				}
				so.ApplyModifiedProperties();
				Refresh();
			});
			container.Add(enumField);

			EnsureCellInputSelectsRow(container, index);
		}

		private void SyncToInspectorVariable(String name, Variable variable)
		{
			if (Application.isPlaying)
				return;

			var so = serializedObject;
			so.Update();
			var varsProp = so.FindProperty("_variables");
			for (var i = 0; i < varsProp.arraySize; i++)
			{
				var elem = varsProp.GetArrayElementAtIndex(i);
				if (elem.FindPropertyRelative("Name").stringValue != name)
					continue;

				switch (variable.Type)
				{
					case Variable.ValueType.Boolean:
						elem.FindPropertyRelative("BoolValue").boolValue = variable.AsBoolean();
						break;
					case Variable.ValueType.Number:
						elem.FindPropertyRelative("NumberValue").doubleValue = variable.AsDouble();
						break;
					default:
						elem.FindPropertyRelative("TextValue").stringValue = variable.AsString();
						break;
				}
				break;
			}
			so.ApplyModifiedProperties();
		}

		private void OnAddClicked()
		{
			var so = serializedObject;
			so.Update();
			var varsProp = so.FindProperty("_variables");
			varsProp.InsertArrayElementAtIndex(varsProp.arraySize);
			var newElem = varsProp.GetArrayElementAtIndex(varsProp.arraySize - 1);
			newElem.FindPropertyRelative("Name").stringValue = "newVar";
			newElem.FindPropertyRelative("Type").enumValueIndex = (Int32)Variable.ValueType.Number;
			newElem.FindPropertyRelative("BoolValue").boolValue = false;
			newElem.FindPropertyRelative("NumberValue").doubleValue = 0.0;
			newElem.FindPropertyRelative("TextValue").stringValue = String.Empty;
			so.ApplyModifiedProperties();
			Refresh();

			_listView.ClearSelection();
			_listView.SetSelection(_viewRows.Count - 1);
			_listView.ScrollToItem(_viewRows.Count - 1);
		}

		private void OnRemoveClicked()
		{
			var selectedItemsCount = _listView.selectedIndices.Count();
			if (selectedItemsCount == 0)
				return;

			var so = serializedObject;
			so.Update();
			var varsProp = so.FindProperty("_variables");
			var newSelectionIndex = -1;

			foreach (var index in _listView.selectedIndices)
			{
				if (newSelectionIndex < 0)
					newSelectionIndex = index;

				var selected = _viewRows[index];
				for (var i = 0; i < varsProp.arraySize; i++)
				{
					if (varsProp.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue != selected.Name)
						continue;

					varsProp.DeleteArrayElementAtIndex(i);
					break;
				}
			}

			so.ApplyModifiedProperties();
			_listView.ClearSelection();
			Refresh();

			if (newSelectionIndex >= _viewRows.Count)
				newSelectionIndex = _viewRows.Count - selectedItemsCount;
			if (newSelectionIndex >= 0)
			{
				_listView.SetSelection(newSelectionIndex);
				_listView.ScrollToItem(newSelectionIndex);
			}
		}

		private void OnColumnSortingChanged()
		{
			ApplySort();
			_listView.RefreshItems();
		}

		private void UpdateEmptyState()
		{
			var emptyLabel = _root.Q<Label>("empty-label");
			if (emptyLabel == null)
				return;

			var isEmpty = _viewRows.Count == 0;
			emptyLabel.style.display = isEmpty ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private void UnsubscribeFromTable()
		{
			if (_table != null)
				_table.OnVariableChanged -= OnVariableChanged;
		}

		private void EnsureCellInputSelectsRow(VisualElement element, Int32 index) => element.RegisterCallback<PointerDownEvent>(evt =>
		{
			if (_listView.selectedIndex != index)
				_listView.SetSelection(index);
		}, TrickleDown.TrickleDown);
	}
}
