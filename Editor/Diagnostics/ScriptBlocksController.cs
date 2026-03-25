using Luny.Engine.Bridge;
using LunyScript.Blocks;
using LunyScript.Diagnostics;
using LunyScript.Events;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptBlocksController
	{
		// ── Fields ────────────────────────────────────────────────────────

		private readonly VisualElement _root;
		private readonly TextField _filterField;
		private readonly Toggle _showEmptyToggle;
		private readonly TreeView _treeView;
		private readonly Label _emptyLabel;
		private readonly List<TreeViewItemData<NodeData>> _rootItems = new();

		private ScriptEventScheduler _scheduler;
		private String _filterText = String.Empty;
		private IVisualElementScheduledItem _filterDebounce;
		private Boolean _showEmpty;
		private GameObject _selectedGameObject;
		private Int32 _lastFrameCount = -1;
		private Int32 _nextNodeId;

		private static Int32 CountBlocksInSequences(IEnumerable<ISequenceBlock> sequences)
		{
			if (sequences == null)
				return 0;

			var count = 0;
			foreach (var seq in sequences)
			{
				if (seq != null)
					count += seq.Blocks.Count;
			}
			return count;
		}

		// ── Construction ──────────────────────────────────────────────────

		internal ScriptBlocksController(VisualElement root)
		{
			_root = root;
			_filterField = root.Q<TextField>("filter-field");
			_showEmptyToggle = root.Q<Toggle>("toggle-show-empty");
			_treeView = root.Q<TreeView>("blocks-tree");
			_emptyLabel = root.Q<Label>("empty-label");
			_showEmpty = _showEmptyToggle.value;

			SetupTreeView();
			UpdateEmptyState();

			_filterField.RegisterValueChangedCallback(OnFilterChanged);
			_showEmptyToggle.RegisterValueChangedCallback(OnShowEmptyChanged);
		}

		// ── Public API ────────────────────────────────────────────────────

		internal void Reset()
		{
			_scheduler = null;
			_rootItems.Clear();
			_treeView.SetRootItems(_rootItems);
			_treeView.Rebuild();
			UpdateEmptyState();
		}

		internal void OnSelectionChanged(GameObject go)
		{
			_selectedGameObject = go;
			RefreshScheduler();
		}

		internal void OnEditorUpdate()
		{
			if (!EditorApplication.isPlaying)
				return;

			var frameCount = Time.frameCount;
			if (frameCount == _lastFrameCount)
				return;

			_lastFrameCount = frameCount;
			UpdateVisibleBlockLabels();
		}

		// ── TreeView setup ────────────────────────────────────────────────

		private void SetupTreeView()
		{
			_treeView.makeItem = () => new Label { style = { flexGrow = 1 } };
			_treeView.bindItem = (element, index) =>
			{
				var data = _treeView.GetItemDataForIndex<NodeData>(index);
				((Label)element).text = data.Kind == NodeData.NodeKind.Block
					? data.BlockState.DisplayString
					: data.Label;
				element.EnableInClassList("filtered-out", data.IsFilteredOut);
			};
		}

		// ── Scheduler resolution ──────────────────────────────────────────

		private void RefreshScheduler()
		{
			_scheduler = ResolveScheduler(_selectedGameObject);
			RebuildTree();
		}

		private ScriptEventScheduler ResolveScheduler(GameObject go)
		{
			if (!EditorApplication.isPlaying)
				return null;

			var scriptEngine = ScriptEngine.Instance;
			if (scriptEngine == null)
				return null;

			if (go == null || !go.scene.IsValid())
				return null;

			var context = scriptEngine.GetScriptContext(go.GetInstanceID()) as ScriptRuntimeContext;
			return context?.Scheduler;
		}

		// ── Tree building ─────────────────────────────────────────────────

		private void RebuildTree()
		{
			_nextNodeId = 0;
			_rootItems.Clear();

			if (_scheduler != null)
				PopulateRootItems(_rootItems);

			_treeView.SetRootItems(_rootItems);
			_treeView.Rebuild();
			ApplyFilter();
			UpdateEmptyState();
		}

		private void PopulateRootItems(List<TreeViewItemData<NodeData>> rootItems)
		{
			foreach (var categoryType in ScriptEventScheduler.RegisteredCategories)
			{
				var categoryChildren = new List<TreeViewItemData<NodeData>>();
				var enumNames = Enum.GetNames(categoryType);

				for (var i = 0; i < enumNames.Length; i++)
				{
					var sequences = _scheduler.GetSequences(categoryType, i);
					var seqChildren = BuildSequenceChildren(sequences);

					if (seqChildren.Count == 0 && !_showEmpty)
						continue;

					var blockCount = CountBlocksInSequences(sequences);
					var eventLabel = blockCount > 0 ? $"{enumNames[i]} [{blockCount} Block(s)]" : enumNames[i];
					categoryChildren.Add(new TreeViewItemData<NodeData>(
						NextId(),
						new NodeData { Kind = NodeData.NodeKind.Event, Label = eventLabel },
						seqChildren.Count > 0 ? seqChildren : null));
				}

				if (categoryChildren.Count == 0 && !_showEmpty)
					continue;

				rootItems.Add(new TreeViewItemData<NodeData>(
					NextId(),
					new NodeData { Kind = NodeData.NodeKind.Category, Label = categoryType.Name },
					categoryChildren.Count > 0 ? categoryChildren : null));
			}

			var inputActionChildren = BuildInputActionChildren();
			if (inputActionChildren.Count > 0 || _showEmpty)
			{
				rootItems.Add(new TreeViewItemData<NodeData>(
					NextId(),
					new NodeData { Kind = NodeData.NodeKind.Category, Label = nameof(LunyInputActionEvent) },
					inputActionChildren.Count > 0 ? inputActionChildren : null));
			}
		}

		private List<TreeViewItemData<NodeData>> BuildInputActionChildren()
		{
			var result = new List<TreeViewItemData<NodeData>>();
			foreach (var actionName in _scheduler.GetInputActionNames())
			{
				var phaseChildren = new List<TreeViewItemData<NodeData>>();
				foreach (LunyInputActionPhase phase in Enum.GetValues(typeof(LunyInputActionPhase)))
				{
					var sequences = _scheduler.GetInputActionEventSequences(actionName, phase);
					var seqChildren = BuildSequenceChildren(sequences);

					if (seqChildren.Count == 0 && !_showEmpty)
						continue;

					var blockCount = CountBlocksInSequences(sequences);
					var phaseLabel = blockCount > 0 ? $"{phase} [{blockCount} Block(s)]" : phase.ToString();
					phaseChildren.Add(new TreeViewItemData<NodeData>(
						NextId(),
						new NodeData { Kind = NodeData.NodeKind.Event, Label = phaseLabel },
						seqChildren.Count > 0 ? seqChildren : null));
				}

				if (phaseChildren.Count == 0 && !_showEmpty)
					continue;

				result.Add(new TreeViewItemData<NodeData>(
					NextId(),
					new NodeData { Kind = NodeData.NodeKind.Event, Label = actionName },
					phaseChildren.Count > 0 ? phaseChildren : null));
			}
			return result;
		}

		private List<TreeViewItemData<NodeData>> BuildSequenceChildren(IEnumerable<ISequenceBlock> sequences)
		{
			var result = new List<TreeViewItemData<NodeData>>();
			if (sequences == null)
				return result;

			foreach (var seq in sequences)
			{
				if (seq == null)
					continue;

				var blockChildren = new List<TreeViewItemData<NodeData>>();
				foreach (var block in seq.Blocks)
				{
					if (block == null)
						continue;

					var state = new ScriptBlockState(block);
					blockChildren.Add(new TreeViewItemData<NodeData>(
						NextId(),
						new NodeData { Kind = NodeData.NodeKind.Block, Label = state.DisplayString, BlockState = state }));
				}

				var seqLabel = $"{seq.GetType().Name} [{seq.Blocks.Count} block(s)]";
				result.Add(new TreeViewItemData<NodeData>(
					NextId(),
					new NodeData { Kind = NodeData.NodeKind.Sequence, Label = seqLabel },
					blockChildren.Count > 0 ? blockChildren : null));
			}
			return result;
		}

		// ── Filter ────────────────────────────────────────────────────────

		private void OnFilterChanged(ChangeEvent<String> evt)
		{
			_filterText = evt.newValue ?? String.Empty;
			_filterDebounce?.Pause();
			_filterDebounce = _root.schedule.Execute(ApplyFilter).StartingIn(200);
		}

		private void OnShowEmptyChanged(ChangeEvent<Boolean> evt)
		{
			_showEmpty = evt.newValue;
			RebuildTree();
		}

		private void ApplyFilter()
		{
			_treeView.CollapseAll();

			var hasFilter = !String.IsNullOrEmpty(_filterText);
			foreach (var item in _rootItems)
				ApplyFilterToItem(item, hasFilter);

			if (!hasFilter)
			{
				foreach (var categoryItem in _rootItems)
				{
					_treeView.ExpandItem(categoryItem.id);
					if (categoryItem.hasChildren)
					{
						foreach (var eventItem in categoryItem.children)
						{
							_treeView.ExpandItem(eventItem.id);
							if (eventItem.hasChildren)
							{
								foreach (var blockItem in eventItem.children)
									_treeView.ExpandItem(blockItem.id);
							}
						}
					}
				}
			}

			_treeView.RefreshItems();
		}

		private Boolean ApplyFilterToItem(TreeViewItemData<NodeData> item, Boolean hasFilter)
		{
			var data = item.data;

			if (data.Kind == NodeData.NodeKind.Block)
			{
				data.IsFilteredOut = hasFilter && !data.BlockState.Contains(_filterText);
				return !data.IsFilteredOut;
			}

			var anyChildMatches = false;
			if (item.hasChildren)
			{
				foreach (var child in item.children)
				{
					if (ApplyFilterToItem(child, hasFilter))
						anyChildMatches = true;
				}
			}

			data.IsFilteredOut = hasFilter && !anyChildMatches;

			if (hasFilter && anyChildMatches)
				_treeView.ExpandItem(item.id);

			return anyChildMatches;
		}

		// ── Per-frame update ──────────────────────────────────────────────

		// RefreshItems() rebinds only the currently visible elements; Unity handles
		// viewport culling internally, so this is equivalent to iterating visible indices.
		private void UpdateVisibleBlockLabels() => _treeView.RefreshItems();

		// ── Empty state ───────────────────────────────────────────────────

		private void UpdateEmptyState()
		{
			String message = null;

			if (!EditorApplication.isPlaying)
				message = "Enter Play Mode to inspect script blocks.";
			else if (_selectedGameObject == null)
				message = "Select a GameObject.";
			else if (_scheduler == null)
				message = "Selected object has no LunyScript.";

			var hasContent = message == null;
			_treeView.style.display = hasContent ? DisplayStyle.Flex : DisplayStyle.None;
			_emptyLabel.style.display = hasContent ? DisplayStyle.None : DisplayStyle.Flex;

			if (!hasContent)
				_emptyLabel.text = message;
		}

		private Int32 NextId() => _nextNodeId++;
		// ── Node model ────────────────────────────────────────────────────

		private sealed class NodeData
		{
			public enum NodeKind
			{
				Category,
				Event,
				Sequence,
				Block,
			}

			public NodeKind Kind;
			public String Label;
			public ScriptBlockState BlockState; // non-null only for Block nodes
			public Boolean IsFilteredOut;
		}
	}
}
