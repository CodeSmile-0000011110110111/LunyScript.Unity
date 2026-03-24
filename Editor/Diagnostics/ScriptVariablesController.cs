using Luny;
using LunyScript;
using LunyScript.Diagnostics;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptVariablesController : IDisposable
	{
		private readonly VisualElement _root;
		private readonly TextField _filterField;
		private readonly MultiColumnListView _listView;
		private readonly Label _emptyLabel;

		private ScriptVariableState[] _masterRows;
		private readonly List<ScriptVariableState> _viewRows = new();
		private ITable _table;
		private String _filterText = String.Empty;
		private IVisualElementScheduledItem _filterDebounce;

		internal ScriptVariablesController(VisualElement root)
		{
			_root = root;
			_filterField = root.Q<TextField>("filter-field");
			_listView = root.Q<MultiColumnListView>("variables-list");
			_emptyLabel = root.Q<Label>("empty-label");

			SetupListView();
			_filterField.RegisterValueChangedCallback(OnFilterChanged);

			ScriptDiagnosticsObserver.OnDiagnosticsStartup += OnDiagnosticsStartup;
			ScriptDiagnosticsObserver.OnDiagnosticsShutdown += OnDiagnosticsShutdown;
			Selection.selectionChanged += OnSelectionChanged;

			if (ScriptDiagnosticsObserver.Instance != null)
				OnSelectionChanged();

			UpdateEmptyState();
		}

		public void Dispose()
		{
			ScriptDiagnosticsObserver.OnDiagnosticsStartup -= OnDiagnosticsStartup;
			ScriptDiagnosticsObserver.OnDiagnosticsShutdown -= OnDiagnosticsShutdown;
			Selection.selectionChanged -= OnSelectionChanged;
			UnsubscribeFromTable();
		}

		private void OnDiagnosticsStartup(ScriptDiagnosticsObserver _) => OnSelectionChanged();

		private void OnDiagnosticsShutdown(ScriptDiagnosticsObserver _)
		{
			UnsubscribeFromTable();
			_table = null;
			_masterRows = null;
			_viewRows.Clear();
			_listView.itemsSource = _viewRows;
			_listView.RefreshItems();
			UpdateEmptyState();
		}

		private void OnSelectionChanged()
		{
			UnsubscribeFromTable();
			_table = ResolveTable();

			if (_table != null)
			{
				_masterRows = BuildMasterRows(_table);
				_table.OnVariableChanged += OnVariableChanged;
			}
			else
			{
				_masterRows = null;
			}

			RebuildView();
			UpdateEmptyState();
		}

		private ITable ResolveTable()
		{
			if (ScriptEngine.Instance == null)
				return null;

			var go = Selection.activeGameObject;
			if (go == null || !go.scene.IsValid())
				return null;

			var context = ScriptEngine.Instance.GetScriptContext(go.GetInstanceID());
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

		private void OnVariableChanged(object sender, VariableChangedArgs args)
		{
			if (_masterRows == null)
				return;

			foreach (var row in _masterRows)
			{
				if (row.Handle.Name == args.Name)
				{
					row.FrameStamp = Time.frameCount;
					break;
				}
			}

			for (var i = 0; i < _viewRows.Count; i++)
			{
				if (_viewRows[i].Handle.Name != args.Name)
					continue;

				_listView.RefreshItem(i);
				break;
			}
		}

		private void OnFilterChanged(ChangeEvent<String> evt)
		{
			_filterText = evt.newValue ?? String.Empty;
			_filterDebounce?.Pause();
			_filterDebounce = _root.schedule.Execute(RebuildView).StartingIn(150);
		}

		private void RebuildView()
		{
			_viewRows.Clear();

			if (_masterRows != null)
			{
				foreach (var row in _masterRows)
				{
					if (String.IsNullOrEmpty(_filterText) ||
					    row.Handle.Name.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0)
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
				_viewRows.Sort((a, b) => ascending
					? String.Compare(a.Handle.Name, b.Handle.Name, StringComparison.OrdinalIgnoreCase)
					: String.Compare(b.Handle.Name, a.Handle.Name, StringComparison.OrdinalIgnoreCase));
			else if (desc.columnName == "timestamp")
				_viewRows.Sort((a, b) => ascending
					? a.FrameStamp.CompareTo(b.FrameStamp)
					: b.FrameStamp.CompareTo(a.FrameStamp));
		}

		private void SetupListView()
		{
			_listView.columnSortingChanged += OnColumnSortingChanged;

			var nameCol = _listView.columns["name"];
			nameCol.makeCell = () => new TextField { isReadOnly = true, style = { flexGrow = 1 } };
			nameCol.bindCell = (element, index) =>
			{
				var row = _viewRows[index];
				((TextField)element).SetValueWithoutNotify(row.Handle.Name);
				element.EnableInClassList("constant-row", row.Handle.IsConstant);
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
				element.EnableInClassList("constant-row", row.Handle.IsConstant);
			};
		}

		private void BindValueCell(VisualElement container, Int32 index)
		{
			container.Clear();

			var row = _viewRows[index];
			if (row.Handle is not Table.VarHandle varHandle)
				return;

			var variable = varHandle.Variable;
			VisualElement valueElement;

			switch (variable.Type)
			{
				case Variable.ValueType.Boolean:
					var toggle = new Toggle { style = { flexGrow = 1 } };
					toggle.SetValueWithoutNotify(variable.AsBoolean());
					toggle.SetEnabled(!row.Handle.IsConstant);
					toggle.RegisterValueChangedCallback(evt => varHandle.Variable = evt.newValue);
					valueElement = toggle;
					break;
				case Variable.ValueType.Number:
					var df = new DoubleField { style = { flexGrow = 1 } };
					df.SetValueWithoutNotify(variable.AsDouble());
					df.SetEnabled(!row.Handle.IsConstant);
					df.RegisterValueChangedCallback(evt => varHandle.Variable = evt.newValue);
					valueElement = df;
					break;
				default: // String
					var tf = new TextField { style = { flexGrow = 1 } };
					tf.SetValueWithoutNotify(variable.AsString());
					tf.SetEnabled(!row.Handle.IsConstant);
					tf.RegisterValueChangedCallback(evt => varHandle.Variable = evt.newValue);
					valueElement = tf;
					break;
			}

			container.Add(valueElement);
			container.EnableInClassList("constant-row", row.Handle.IsConstant);
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
