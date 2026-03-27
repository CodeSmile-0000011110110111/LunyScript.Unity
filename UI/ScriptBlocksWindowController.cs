using Luny;
using Luny.Engine.Bridge;
using LunyScript.Blocks;
using LunyScript.Diagnostics;
using LunyScript.Events;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal sealed class ScriptBlocksWindowController : ScriptDiagnosticsWindowController
	{
		// ── TreeView setup ────────────────────────────────────────────────

		private const Boolean ShowNodeKind = false; // debug toggle
		// ── Fields ────────────────────────────────────────────────────────

		private readonly TextField _filterField;
		private readonly Toggle _showEmptyToggle;
		private readonly TreeView _treeView;
		private readonly List<TreeViewItemData<NodeData>> _rootItems = new();

		private ScriptEventScheduler _scheduler;
		private Boolean _showEmpty;
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
					count += seq.BlockCount;
			}
			return count;
		}

		private static void TryOpenFile(NodeData data)
		{
			var trace = data.BlockState?.Block?.Trace;
			if (trace == null || trace.Count == 0)
				return;

			var frame = trace[0];
			var path = frame.FullPath;
			if (path != null)
				InternalEditorUtility.OpenFileAtLineExternal(path, frame.Line, frame.Column);
		}

		private static String GetRootItemName(Type categoryType) => categoryType.Name.TrimStart("Luny").TrimEnd("Event");

		// ── Construction ──────────────────────────────────────────────────

		internal ScriptBlocksWindowController(VisualElement root)
			: base(root)
		{
			_filterField = root.Q<TextField>("filter-field");
			_showEmptyToggle = root.Q<Toggle>("toggle-show-empty");
			_treeView = root.Q<TreeView>("blocks-tree");
			_treeView.itemsChosen += OnItemsChosen;
			_showEmpty = _showEmptyToggle.value;

			SetupTreeView();
			UpdateEmptyState();

			_filterField.RegisterValueChangedCallback(OnFilterChanged);
			_showEmptyToggle.RegisterValueChangedCallback(OnShowEmptyChanged);
		}

		private void OnItemsChosen(IEnumerable<Object> items)
		{
			foreach (var item in items)
			{
				if (item is NodeData data)
					TryOpenFile(data);
			}
		}

		// ── Public API ────────────────────────────────────────────────────

		internal override void Reset()
		{
			_scheduler = null;
			_rootItems.Clear();
			_treeView.SetRootItems(_rootItems);
			_treeView.Rebuild();
			UpdateEmptyState();
		}

		internal override void SetTarget(GameObject target)
		{
			base.SetTarget(target);
			Refresh();
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

		private void SetupTreeView()
		{
			_treeView.makeItem = () => new Label { style = { flexGrow = 1 } };
			_treeView.bindItem = (element, index) =>
			{
				var data = _treeView.GetItemDataForIndex<NodeData>(index);
				var text = data.Kind == NodeData.NodeKind.Block ? data.BlockState.DisplayString : data.Label;
				((Label)element).text = ShowNodeKind ? $"[{data.Kind}] {text}" : text;
				element.EnableInClassList("filtered-out", data.IsFilteredOut);
			};
		}

		// ── Scheduler resolution ──────────────────────────────────────────

		private void Refresh()
		{
			_scheduler = ResolveScheduler();
			RebuildView();
		}

		private ScriptEventScheduler ResolveScheduler() => IsTargetValid() ? ScriptContext?.Scheduler : null;

		// ── Tree building ─────────────────────────────────────────────────

		private void RebuildView()
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
			BuildEventChildren(rootItems);
			BuildInputActionEventChildren(rootItems);
		}

		private void BuildEventChildren(List<TreeViewItemData<NodeData>> rootItems)
		{
			foreach (var categoryType in ScriptEventScheduler.RegisteredCategories)
			{
				var categoryChildren = new List<TreeViewItemData<NodeData>>();
				var enumNames = Enum.GetNames(categoryType);

				for (var i = 0; i < enumNames.Length; i++)
				{
					var sequences = _scheduler.GetObjectEventSequences(categoryType, i);
					var sequenceChildren = BuildBlockSequenceChildren(sequences);
					if (sequenceChildren.Count == 0 && !_showEmpty)
						continue;

					var firstSequence = sequences.Count > 0 ? sequences[0] : null;
					categoryChildren.Add(CreateTreeItem(enumNames[i], NodeData.NodeKind.Event, sequenceChildren, firstSequence));
				}

				if (categoryChildren.Count == 0 && !_showEmpty)
					continue;

				rootItems.Add(CreateTreeItem(GetRootItemName(categoryType), NodeData.NodeKind.Category, categoryChildren, null));
			}
		}

		private void BuildInputActionEventChildren(List<TreeViewItemData<NodeData>> rootItems)
		{
			var inputActionChildren = new List<TreeViewItemData<NodeData>>();

			foreach (var actionName in _scheduler.GetInputActionNames())
			{
				var phaseChildren = new List<TreeViewItemData<NodeData>>();
				foreach (LunyInputActionPhase phase in Enum.GetValues(typeof(LunyInputActionPhase)))
				{
					var sequences = _scheduler.GetInputActionEventSequences(actionName, phase);
					var sequenceChildren = BuildBlockSequenceChildren(sequences);
					if (sequenceChildren.Count == 0 && !_showEmpty)
						continue;

					phaseChildren.Add(CreateTreeItem(phase.ToString(), NodeData.NodeKind.Event, sequenceChildren, sequences[0]));
				}

				if (phaseChildren.Count == 0 && !_showEmpty)
					continue;

				var firstSequence = _scheduler.GetInputActionEventSequences(actionName, 0);
				inputActionChildren.Add(CreateTreeItem($"\"{actionName}\"", NodeData.NodeKind.Event, phaseChildren,
					firstSequence?.Count > 0 ? firstSequence[0] : null));
			}

			if (inputActionChildren.Count > 0 || _showEmpty)
			{
				rootItems.Add(CreateTreeItem(GetRootItemName(typeof(LunyInputActionEvent)), NodeData.NodeKind.Category, inputActionChildren,
					null));
			}
		}

		private List<TreeViewItemData<NodeData>> BuildBlockSequenceChildren(IEnumerable<ISequenceBlock> sequences)
		{
			var result = new List<TreeViewItemData<NodeData>>();
			if (sequences == null)
				return result;

			foreach (var sequence in sequences)
			{
				if (sequence == null)
					continue;

				var blockChildren = new List<TreeViewItemData<NodeData>>();
				foreach (var block in sequence.Blocks)
				{
					if (block == null)
						continue;

					blockChildren.Add(BuildBlockChildren(block));
				}

				result.Add(CreateTreeItem(sequence.ToString(), NodeData.NodeKind.Sequence, blockChildren, sequence));
			}
			return result;
		}

		private TreeViewItemData<NodeData> BuildBlockChildren(ScriptBlock block)
		{
			if (block is IBlockContainer container)
			{
				if (block is ILogicalOperatorBlock)
				{
					// Skip [Block] node; the single branch node IS the meaningful representation
					var branches = BuildBlockContainerChildren(container);
					return branches.Count > 0 ? branches[0] : CreateTreeItem(null, NodeData.NodeKind.Block, null, block);
				}

				var containerChildren = BuildBlockContainerChildren(container);
				return CreateTreeItem(null, NodeData.NodeKind.Block, containerChildren, block);
			}

			return CreateTreeItem(null, NodeData.NodeKind.Block, null, block);
		}

		private List<TreeViewItemData<NodeData>> BuildBlockContainerChildren(IBlockContainer container)
		{
			var result = new List<TreeViewItemData<NodeData>>();
			var condCount = container.ConditionSequenceCount;
			var actCount = container.ActionSequenceCount;
			var maxCount = Math.Max(condCount, actCount);

			for (var i = 0; i < maxCount; i++)
			{
				if (i < condCount)
				{
					var sequence = container.GetConditionSequence(i);
					if (sequence != null)
						result.Add(BuildBlockContainerBranch(container.GetConditionSequenceName(i), sequence));
				}
				if (i < actCount)
				{
					var sequence = container.GetActionSequence(i);
					if (sequence != null)
						result.Add(BuildBlockContainerBranch(container.GetActionSequenceName(i), sequence));
				}
			}

			return result;
		}

		private TreeViewItemData<NodeData> BuildBlockContainerBranch(String name, IEnumerable<IScriptBlock> blocks)
		{
			var children = new List<TreeViewItemData<NodeData>>();
			foreach (var block in blocks)
			{
				if (block is ScriptBlock sb)
					children.Add(BuildBlockChildren(sb));
			}

			return CreateTreeItem(name, NodeData.NodeKind.Branch, children, null);
		}

		private TreeViewItemData<NodeData> CreateTreeItem(String label, NodeData.NodeKind nodeKind, List<TreeViewItemData<NodeData>> children,
			IScriptBlock block) => new(NextNodeId(), new NodeData
			{
				Label = label,
				Kind = nodeKind,
				BlockState = block is ScriptBlock b ? new ScriptBlockState(b) : null,
			},
			children?.Count > 0 ? children : null);

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
			RebuildView();
		}

		private void ApplyFilter()
		{
			_treeView.CollapseAll();

			var hasFilter = !String.IsNullOrEmpty(_filterText);
			foreach (var item in _rootItems)
				ApplyFilterToItem(item, hasFilter);

			if (!hasFilter)
			{
				foreach (var item in _rootItems)
					ExpandItemRecursive(item);
			}

			_treeView.RefreshItems();
		}

		private void ExpandItemRecursive(TreeViewItemData<NodeData> item)
		{
			_treeView.ExpandItem(item.id);
			if (item.hasChildren)
			{
				foreach (var child in item.children)
					ExpandItemRecursive(child);
			}
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
			UpdateEmptyState(_treeView, ScriptContext != null);

			if (EditorApplication.isPlaying)
			{
				if (_target == null)
					_emptyLabel.text = "Select a GameObject in Hierarchy window.";
				else if (ScriptContext == null)
					_emptyLabel.text = "Selected GameObject does not run a LunyScript.";
			}
		}

		private Int32 NextNodeId() => _nextNodeId++;
		// ── Node model ────────────────────────────────────────────────────

		private sealed class NodeData
		{
			public enum NodeKind
			{
				Category,
				Event,
				Sequence,
				Branch,
				Block,
			}

			public NodeKind Kind;
			public String Label;
			public ScriptBlockState BlockState; // non-null only for Block nodes
			public Boolean IsFilteredOut;

			public override String ToString() => $"{Kind}: {Label} => {BlockState}";
		}
	}
}
