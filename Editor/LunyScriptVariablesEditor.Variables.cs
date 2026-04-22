using Luny;
using LunyScript.Diagnostics;
using LunyScript.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace LunyScript.UnityEditor
{
	internal sealed partial class LunyScriptVariablesEditor
	{
		private readonly List<ScriptVariableState> _viewRows = new();

		private MultiColumnListView _variablesListView;
		private Button _btnAdd;
		private Button _btnRemove;

		private ScriptVariableState[] _masterRows;
		private String _focusedRowName;
		private ITable _table;

		private static Int32 GetValueTypeOrdinal(ScriptVariableState row) => row.ValueTypeOrdinal;

		private void OnVariablesDisable()
		{
			UnsubscribeFromTable();
			_focusedRowName = null;
		}

		private void CreateVariablesGUI(VisualElement root)
		{
			_variablesListView = root.Q<MultiColumnListView>("variables-list");
			_btnAdd = root.Q<Button>("btn-add");
			_btnRemove = root.Q<Button>("btn-remove");

			SetupVariablesListView();
			RefreshVariables();
			UpdateVariablesEmptyState();

			_btnAdd.clicked += OnAddClicked;
			_btnRemove.clicked += OnRemoveClicked;
		}

		private void OnVariablesSerializedObjectChanged(SerializedObject so)
		{
			var varsProp = so.FindProperty("_variables");
			if (varsProp != null && varsProp.arraySize != _viewRows.Count)
				RefreshVariables();
		}

		private void RefreshVariables()
		{
			UnsubscribeFromTable();

			var component = (LunyScriptVariables)target;
			if (!Application.isPlaying)
				component.ResetTable();
			_table = component.Table;

			if (_table != null)
			{
				_masterRows = BuildMasterRows(_table);
				_table.OnVariableChanged += OnVariableChanged;
			}
			else
				_masterRows = null;

			RebuildVariablesView();
			UpdateVariablesEmptyState();
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

			if (args.Name == _focusedRowName)
				return;

			for (var i = 0; i < _viewRows.Count; i++)
			{
				if (!_viewRows[i].HasName(args.Name))
					continue;

				_variablesListView.RefreshItem(i);
				break;
			}
		}

		private void RebuildVariablesView()
		{
			_viewRows.Clear();

			if (_masterRows != null)
			{
				foreach (var row in _masterRows)
					_viewRows.Add(row);
			}

			ApplyVariablesSort();
			_variablesListView.itemsSource = _viewRows;
			_variablesListView.RefreshItems();
		}

		private void ApplyVariablesSort()
		{
			var sortDescs = _variablesListView.sortColumnDescriptions;
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

		private void SetupVariablesListView()
		{
			_variablesListView.columnSortingChanged += OnVariablesColumnSortingChanged;

			var nameCol = _variablesListView.columns["name"];
			nameCol.makeCell = MakeNameCell;
			nameCol.bindCell = BindNameCell;
			nameCol.unbindCell = UnbindNameCell;

			var valueCol = _variablesListView.columns["value"];
			valueCol.makeCell = () => new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row } };
			valueCol.bindCell = BindValueCell;
			valueCol.unbindCell = (element, _) => element.Clear();

			var typeCol = _variablesListView.columns["type"];
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

			EnsureCellInputSelectsRow(_variablesListView, element, index);
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
				if (elem.FindPropertyRelative(nameof(InspectorVariable.Name)).stringValue != row.Name)
					continue;

				elem.FindPropertyRelative(nameof(InspectorVariable.Name)).stringValue = evt.newValue;
				break;
			}
			so.ApplyModifiedProperties();
			RefreshVariables();
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
				case Variable.ValueType.String:
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
						varHandle.Variable = evt.newValue;
					});
					valueElement = tf;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(variable.Type), $"unhandled variable type: {variable.Type}");
			}

			container.Add(valueElement);
			container.EnableInClassList("constant-row", row.IsConstant);

			EnsureCellInputSelectsRow(_variablesListView, container, index);
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

			var varType = (InspectorVariable.VariableType)varHandle.Variable.Type;
			var enumField = new EnumField(varType) { style = { flexGrow = 1 } };
			enumField.SetEnabled(!row.IsConstant);
			enumField.RegisterValueChangedCallback(evt =>
			{
				var newType = (InspectorVariable.VariableType)evt.newValue;
				var so = serializedObject;
				so.Update();
				var varsProp = so.FindProperty("_variables");
				for (var i = 0; i < varsProp.arraySize; i++)
				{
					var elem = varsProp.GetArrayElementAtIndex(i);
					if (elem.FindPropertyRelative(nameof(InspectorVariable.Name)).stringValue != row.Name)
						continue;

					Debug.Log($"set var type: {newType} ({(Int32)newType})");
					elem.FindPropertyRelative(nameof(InspectorVariable.VarType)).enumValueIndex = (Int32)newType;
					elem.FindPropertyRelative(nameof(InspectorVariable.BoolValue)).boolValue = false;
					elem.FindPropertyRelative(nameof(InspectorVariable.NumberValue)).doubleValue = 0.0;
					elem.FindPropertyRelative(nameof(InspectorVariable.TextValue)).stringValue = String.Empty;
					break;
				}
				so.ApplyModifiedProperties();
				RefreshVariables();
			});
			container.Add(enumField);

			EnsureCellInputSelectsRow(_variablesListView, container, index);
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
				if (elem.FindPropertyRelative(nameof(InspectorVariable.Name)).stringValue != name)
					continue;

				switch (variable.Type)
				{
					case Variable.ValueType.Boolean:
						elem.FindPropertyRelative(nameof(InspectorVariable.BoolValue)).boolValue = variable.AsBoolean();
						break;
					case Variable.ValueType.Number:
						elem.FindPropertyRelative(nameof(InspectorVariable.NumberValue)).doubleValue = variable.AsDouble();
						break;
					case Variable.ValueType.String:
						elem.FindPropertyRelative(nameof(InspectorVariable.TextValue)).stringValue = variable.AsString();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(variable.Type), $"unhandled variable type: {variable.Type}");
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
			newElem.FindPropertyRelative(nameof(InspectorVariable.Name)).stringValue = "New Var";
			newElem.FindPropertyRelative(nameof(InspectorVariable.VarType)).enumValueIndex = (Int32)Variable.ValueType.Number;
			newElem.FindPropertyRelative(nameof(InspectorVariable.BoolValue)).boolValue = false;
			newElem.FindPropertyRelative(nameof(InspectorVariable.NumberValue)).doubleValue = 0.0;
			newElem.FindPropertyRelative(nameof(InspectorVariable.TextValue)).stringValue = String.Empty;
			so.ApplyModifiedProperties();
			RefreshVariables();

			_variablesListView.ClearSelection();
			_variablesListView.SetSelection(_viewRows.Count - 1);
			_variablesListView.ScrollToItem(_viewRows.Count - 1);
		}

		private void OnRemoveClicked()
		{
			var selectedItemsCount = _variablesListView.selectedIndices.Count();
			if (selectedItemsCount == 0)
				return;

			var so = serializedObject;
			so.Update();
			var varsProp = so.FindProperty("_variables");
			var newSelectionIndex = -1;

			foreach (var index in _variablesListView.selectedIndices)
			{
				if (newSelectionIndex < 0)
					newSelectionIndex = index;

				var selected = _viewRows[index];
				for (var i = 0; i < varsProp.arraySize; i++)
				{
					if (varsProp.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(InspectorVariable.Name)).stringValue != selected.Name)
						continue;

					varsProp.DeleteArrayElementAtIndex(i);
					break;
				}
			}

			so.ApplyModifiedProperties();
			_variablesListView.ClearSelection();
			RefreshVariables();

			if (newSelectionIndex >= _viewRows.Count)
				newSelectionIndex = _viewRows.Count - selectedItemsCount;
			if (newSelectionIndex >= 0)
			{
				_variablesListView.SetSelection(newSelectionIndex);
				_variablesListView.ScrollToItem(newSelectionIndex);
			}
		}

		private void OnVariablesColumnSortingChanged()
		{
			ApplyVariablesSort();
			_variablesListView.RefreshItems();
		}

		private void UpdateVariablesEmptyState()
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
	}
}
