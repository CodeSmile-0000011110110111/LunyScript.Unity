using Luny;
using LunyScript.Diagnostics;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptVariablesController : IScriptDiagnosticsController
	{
		private readonly VisualElement _root;
		private readonly TextField _filterField;
		private readonly Button _btnGlobal;
		private readonly Button _btnInstance;
		private readonly MultiColumnListView _listView;
		private readonly Label _emptyLabel;
		private readonly List<ScriptVariableState> _viewRows = new();

		private ScriptVariableState[] _masterRows;
		private ITable _table;
		private String _filterText = String.Empty;
		private IVisualElementScheduledItem _filterDebounce;
		private Boolean _showGlobal;
		private GameObject _selectedGameObject;

		private static Int32 GetValueTypeOrdinal(ScriptVariableState row) => row.ValueTypeOrdinal;

		internal ScriptVariablesController(VisualElement root)
		{
			_root = root;
			_filterField = root.Q<TextField>("filter-field");
			_btnGlobal = root.Q<Button>("btn-global");
			_btnInstance = root.Q<Button>("btn-instance");
			_listView = root.Q<MultiColumnListView>("variables-list");
			_emptyLabel = root.Q<Label>("empty-label");

			SetupListView();
			UpdateEmptyState();
			UpdateToggleButtons();

			_filterField.RegisterValueChangedCallback(OnFilterChanged);
			_btnGlobal.clicked += OnGlobalClicked;
			_btnInstance.clicked += OnInstanceClicked;
		}

		private void OnGlobalClicked()
		{
			_showGlobal = true;
			UpdateToggleButtons();
			RefreshTable();
		}

		private void OnInstanceClicked()
		{
			_showGlobal = false;
			UpdateToggleButtons();
			RefreshTable();
		}

		private void UpdateToggleButtons()
		{
			_btnGlobal.EnableInClassList("active-toggle", _showGlobal);
			_btnInstance.EnableInClassList("active-toggle", !_showGlobal);
		}

		void IScriptDiagnosticsController.Reset() => Reset();
		void IScriptDiagnosticsController.OnSelectionChanged(GameObject go) => OnSelectionChanged(go);

		internal void Reset()
		{
			UnsubscribeFromTable();
			_table = null;
			_masterRows = null;
			_viewRows.Clear();
			_listView.itemsSource = _viewRows;
			_listView.RefreshItems();
			UpdateEmptyState();
		}

		internal void OnSelectionChanged(GameObject go)
		{
			_selectedGameObject = go;
			_showGlobal = false;
			UpdateToggleButtons();
			RefreshTable();
		}

		private void RefreshTable()
		{
			UnsubscribeFromTable();
			var go = _showGlobal ? null : _selectedGameObject;
			_table = ResolveTable(go);

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

		private ITable ResolveTable(GameObject go)
		{
			var scriptEngine = ScriptEngine.Instance;
			if (scriptEngine == null)
				return null;

			if (go == null || !go.scene.IsValid())
				return scriptEngine.GlobalVariables;

			var context = scriptEngine.GetScriptContext(go.GetInstanceID());
			return context?.LocalVariables;
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

			foreach (var row in _masterRows)
			{
				if (row.HasName(args.Name))
				{
					row.FrameStamp = Time.frameCount;
					break;
				}
			}

			for (var i = 0; i < _viewRows.Count; i++)
			{
				if (!_viewRows[i].HasName(args.Name))
					continue;

				_listView.RefreshItem(i);
				break;
			}
		}

		private void OnFilterChanged(ChangeEvent<String> evt)
		{
			_filterText = evt.newValue ?? String.Empty;
			_filterDebounce?.Pause();
			_filterDebounce = _root.schedule.Execute(RebuildView).StartingIn(200);
		}

		private void RebuildView()
		{
			_viewRows.Clear();

			if (_masterRows != null)
			{
				foreach (var row in _masterRows)
				{
					if (String.IsNullOrEmpty(_filterText) || row.Contains(_filterText))
						_viewRows.Add(row);
				}
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
			{
				_viewRows.Sort((a, b) => ascending ? a.CompareNameTo(b) : b.CompareNameTo(a));
			}
			else if (desc.columnName == "value")
			{
				_viewRows.Sort((a, b) =>
				{
					var typeCompare = GetValueTypeOrdinal(a).CompareTo(GetValueTypeOrdinal(b));
					if (typeCompare != 0)
						return ascending ? typeCompare : -typeCompare;

					var nameCompare = a.CompareNameTo(b);
					return ascending ? nameCompare : -nameCompare;
				});
			}
			else if (desc.columnName == "timestamp")
			{
				_viewRows.Sort((b, a) => ascending
					? a.FrameStamp.CompareTo(b.FrameStamp)
					: b.FrameStamp.CompareTo(a.FrameStamp));
			}
		}

		private void SetupListView()
		{
			_listView.columnSortingChanged += OnColumnSortingChanged;

			var nameCol = _listView.columns["name"];
			nameCol.makeCell = () => new TextField { isReadOnly = true, style = { flexGrow = 1 } };
			nameCol.bindCell = (element, index) =>
			{
				var row = _viewRows[index];
				((TextField)element).SetValueWithoutNotify(row.Name);
				element.EnableInClassList("constant-row", row.IsConstant);
			};

			var valueCol = _listView.columns["value"];
			valueCol.makeCell = () => new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row } };
			valueCol.bindCell = BindValueCell;
			valueCol.unbindCell = (element, _) => element.Clear();

			var stampCol = _listView.columns["timestamp"];
			stampCol.makeCell = () => new IntegerField { isReadOnly = true, style = { flexGrow = 1 } };
			stampCol.bindCell = (element, index) =>
			{
				var row = _viewRows[index];
				((IntegerField)element).SetValueWithoutNotify(row.FrameStamp);
				element.EnableInClassList("constant-row", row.IsConstant);
			};
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
					toggle.RegisterValueChangedCallback(evt => varHandle.Variable = evt.newValue);
					valueElement = toggle;
					break;
				case Variable.ValueType.Number:
					var df = new DoubleField { style = { flexGrow = 1 } };
					df.SetValueWithoutNotify(variable.AsDouble());
					df.SetEnabled(!row.IsConstant);
					df.RegisterValueChangedCallback(evt => varHandle.Variable = evt.newValue);
					valueElement = df;
					break;
				default: // String
					var tf = new TextField { style = { flexGrow = 1 } };
					tf.SetValueWithoutNotify(variable.AsString());
					tf.SetEnabled(!row.IsConstant);
					tf.RegisterValueChangedCallback(evt => varHandle.Variable = evt.newValue);
					valueElement = tf;
					break;
			}

			container.Add(valueElement);
			container.EnableInClassList("constant-row", row.IsConstant);
		}

		private void OnColumnSortingChanged()
		{
			ApplySort();
			_listView.RefreshItems();
		}

		private void UpdateEmptyState()
		{
			var hasTable = _table != null;
			_listView.style.display = hasTable ? DisplayStyle.Flex : DisplayStyle.None;
			_emptyLabel.style.display = hasTable ? DisplayStyle.None : DisplayStyle.Flex;
		}

		private void UnsubscribeFromTable()
		{
			if (_table != null)
				_table.OnVariableChanged -= OnVariableChanged;
		}
	}
}
