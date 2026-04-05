#if UNITY_EDITOR
using UnityEditorInternal;
#endif
using Luny;
using Luny.Engine.Bridge;
using LunyScript.Blocks;
using LunyScript.Diagnostics;
using LunyScript.Events;
using System;
using System.Collections.Generic;
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
		private readonly Toggle _showEventsToggle;
		private readonly MultiColumnTreeView _treeView;
		private readonly List<TreeViewItemData<NodeData>> _rootItems = new();

		private ScriptEventScheduler _scheduler;
		private Boolean _showEvents;
		private Int32 _lastFrameCount = -1;
		private Int32 _nextNodeId;

		private static void TryOpenFile(NodeData data)
		{
			var trace = data.BlockState?.Block?.Trace;
			if (trace == null || trace.Count == 0)
				return;

#if UNITY_EDITOR
			var frame = trace[0];
			var path = frame.FullPath;
			if (path != null)
				InternalEditorUtility.OpenFileAtLineExternal(path, frame.Line, frame.Column);
#endif
		}

		private static String GetLocationString(NodeData data) =>
			data.BlockState?.FileName != null ? $"{data.BlockState?.FileName}:{data.BlockState.Line}" : String.Empty;

		private static String GetLineNumber(NodeData data) => data.BlockState?.Line > 0 ? data.BlockState.Line.ToString() : "?";
		private static String GetFileName(NodeData data) => data.BlockState?.FileName != null ? data.BlockState.FileName : "unknown";
		private static String GetRootItemName(Type categoryType) => categoryType.Name.TrimStart("Luny").TrimEnd("Event");

		private static List<TreeViewItemData<NodeData>> SortItemsRecursive(IEnumerable<TreeViewItemData<NodeData>> items,
			Func<NodeData, String> keySelector, Boolean ascending)
		{
			var result = new List<TreeViewItemData<NodeData>>();
			foreach (var item in items)
			{
				var sortedChildren = item.hasChildren
					? SortItemsRecursive(item.children, keySelector, ascending)
					: null;
				result.Add(new TreeViewItemData<NodeData>(item.id, item.data, sortedChildren));
			}

			result.Sort((a, b) =>
			{
				var keyA = keySelector(a.data);
				var keyB = keySelector(b.data);
				if (String.IsNullOrEmpty(keyA) && !String.IsNullOrEmpty(keyB))
					return ascending ? 1 : -1;
				if (!String.IsNullOrEmpty(keyA) && String.IsNullOrEmpty(keyB))
					return ascending ? -1 : 1;

				var cmp = String.Compare(keyA, keyB, StringComparison.OrdinalIgnoreCase);
				return ascending ? cmp : -cmp;
			});

			return result;
		}

		private static FontStyle GetFontStyle(NodeData data)
		{
			if (data.Kind == NodeData.NodeKind.Event || data.Kind == NodeData.NodeKind.Sequence)
				return FontStyle.Bold;

			var block = data.BlockState?.Block;
			if (block is IBlockContainer || data.Kind == NodeData.NodeKind.Branch)
				return FontStyle.Bold;

			return FontStyle.Normal;
		}

		private static Color GetBlockColor(NodeData data)
		{
			if (data.Kind == NodeData.NodeKind.Event)
				return Color.softYellow;
			if (data.Kind == NodeData.NodeKind.Sequence)
				return Color.gold;
			if (data.Kind == NodeData.NodeKind.Branch)
				return Color.blanchedAlmond;

			var block = data.BlockState?.Block;
			if (block is IBlockContainer)
				return Color.blanchedAlmond;
			if (block is ILogicalOperator)
				return Color.springGreen;
			if (block is ConditionBlock)
				return Color.lightGreen;
			if (block is ActionBlock)
				return Color.powderBlue;

			return Color.white;
		}

		// ── Construction ──────────────────────────────────────────────────

		internal ScriptBlocksWindowController(VisualElement root)
			: base(root)
		{
			_filterField = root.Q<TextField>("filter-field");
			_showEventsToggle = root.Q<Toggle>("toggle-show-events");
			_treeView = root.Q<MultiColumnTreeView>("blocks-tree");
			_treeView.itemsChosen += OnItemsChosen;
			_showEvents = _showEventsToggle.value;

			SetupTreeView();
			UpdateEmptyState();

			_filterField.RegisterValueChangedCallback(OnFilterChanged);
			_showEventsToggle.RegisterValueChangedCallback(OnShowEmptyChanged);
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
			if (!Application.isPlaying)
				return;

			var frameCount = Time.frameCount;
			if (frameCount == _lastFrameCount)
				return;

			_lastFrameCount = frameCount;
			UpdateVisibleBlockLabels();
		}

		private void SetupTreeView()
		{
			_treeView.columns["event-blocks"].makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } };
			_treeView.columns["event-blocks"].bindCell = (element, index) =>
			{
				var data = _treeView.GetItemDataForIndex<NodeData>(index);
				var text = data.Kind == NodeData.NodeKind.Sequence || data.Kind == NodeData.NodeKind.Block
					? data.BlockState?.GetDisplayString(ScriptContext) ?? data.Label + " (DisplayString is null)"
					: $"🟢 {data.Label}";
				if (data.IsConditionBranch)
				{
					var label = data.BlockState?.GetBranchLabel(ScriptContext, data.BranchIndex);
					if (!String.IsNullOrEmpty(label))
						text = label;
				}
				if (ShowNodeKind)
					text = $"[{data.Kind}] {text}";
				((Label)element).text = text;
				element.style.unityFontStyleAndWeight = GetFontStyle(data);
				element.style.color = GetBlockColor(data);
				element.EnableInClassList("filtered-out", data.IsFilteredOut);
			};

			_treeView.columns["line-number"].makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } };
			_treeView.columns["line-number"].bindCell = (element, index) =>
			{
				var data = _treeView.GetItemDataForIndex<NodeData>(index);
				((Label)element).text = GetLineNumber(data);
				((Label)element).style.unityTextAlign = TextAnchor.MiddleRight;
				element.EnableInClassList("filtered-out", data.IsFilteredOut);
			};

			_treeView.columns["file-name"].makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } };
			_treeView.columns["file-name"].bindCell = (element, index) =>
			{
				var data = _treeView.GetItemDataForIndex<NodeData>(index);
				((Label)element).text = GetFileName(data);
				element.EnableInClassList("filtered-out", data.IsFilteredOut);
			};

			_treeView.columnSortingChanged += OnColumnSortingChanged;
		}

		private void OnColumnSortingChanged()
		{
			SortColumnDescription firstSort = default;
			var hasSortColumn = false;
			foreach (var desc in _treeView.sortedColumns)
			{
				firstSort = desc;
				hasSortColumn = true;
				break;
			}

			List<TreeViewItemData<NodeData>> displayItems;
			if (!hasSortColumn)
				displayItems = _rootItems;
			else
				displayItems = SortItemsRecursive(_rootItems, GetLocationString, firstSort.direction == SortDirection.Ascending);

			_treeView.SetRootItems(displayItems);
			_treeView.Rebuild();
			ApplyFilter();
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
				var enumNames = Enum.GetNames(categoryType);

				for (var i = 0; i < enumNames.Length; i++)
				{
					var sequences = _scheduler.GetObjectEventSequences(categoryType, i);
					var sequenceChildren = BuildSequenceChildren(rootItems, sequences);
					if (_showEvents)
					{
						var firstSequence = sequences?.Count > 0 ? sequences[0] : null;
						rootItems.Add(CreateTreeItem(enumNames[i], NodeData.NodeKind.Event, sequenceChildren, firstSequence));
					}
				}
			}
		}

		private void BuildInputActionEventChildren(List<TreeViewItemData<NodeData>> rootItems)
		{
			foreach (var actionName in _scheduler.GetInputActionNames())
			{
				var phaseChildren = new List<TreeViewItemData<NodeData>>();
				foreach (LunyInputActionPhase phase in Enum.GetValues(typeof(LunyInputActionPhase)))
				{
					var sequences = _scheduler.GetInputActionEventSequences(actionName, phase);
					var sequenceChildren = BuildSequenceChildren(rootItems, sequences);
					if (_showEvents)
					{
						var firstSequence = sequences?.Count > 0 ? sequences[0] : null;
						phaseChildren.Add(CreateTreeItem(phase.ToString(), NodeData.NodeKind.Event, sequenceChildren, firstSequence));
					}
				}

				if (_showEvents)
				{
					var firstActionSequence = _scheduler.GetInputActionEventSequences(actionName, 0);
					rootItems.Add(CreateTreeItem($"InputAction(\"{actionName}\")", NodeData.NodeKind.Event, phaseChildren,
						firstActionSequence?.Count > 0 ? firstActionSequence[0] : null));
				}
			}
		}

		private List<TreeViewItemData<NodeData>> BuildSequenceChildren(List<TreeViewItemData<NodeData>> rootItems,
			IEnumerable<ISequenceBlock> sequences)
		{
			var items = _showEvents ? new List<TreeViewItemData<NodeData>>() : rootItems;
			if (sequences == null)
				return items;

			foreach (var sequence in sequences)
			{
				if (sequence == null)
					continue;

				var children = new List<TreeViewItemData<NodeData>>();
				foreach (var block in sequence.Blocks)
				{
					if (block == null)
						continue;

					if (block is IBlockContainer container)
					{
						var branches = BuildBlockContainerChildren(container);
						// the single branch node IS the meaningful representation
						if (block is ILogicalOperator)
						{
							if (branches.Count > 0)
								children.Add(branches[0]);
							else
								children.Add(CreateTreeItem(nameof(ILogicalOperator), NodeData.NodeKind.Block, null, block));
						}
						else
						{
							foreach (var branch in branches)
								children.Add(branch);
						}
					}
					else
						children.Add(BuildBlockChildren(block));
				}

				items.Add(CreateTreeItem(sequence.ToString(), NodeData.NodeKind.Sequence, children, sequence));
			}
			return items;
		}

		private TreeViewItemData<NodeData> BuildBlockChildren(ScriptBlock block)
		{
			if (block is IBlockContainer container)
			{
				var branches = BuildBlockContainerChildren(container);

				// the single branch node IS the meaningful representation
				if (block is ILogicalOperator)
					return branches.Count > 0 ? branches[0] : CreateTreeItem(null, NodeData.NodeKind.Block, null, block);

				return CreateTreeItem(null, NodeData.NodeKind.Block, branches, block); // should be NodeKind.Container
			}
			return CreateTreeItem(null, NodeData.NodeKind.Block, null, block);
		}

		private List<TreeViewItemData<NodeData>> BuildBlockContainerChildren(IBlockContainer container)
		{
			var result = new List<TreeViewItemData<NodeData>>();
			var condCount = container.ConditionSequenceCount;
			var actCount = container.ActionSequenceCount;
			var maxCount = Math.Max(condCount, actCount);
			var block = (ScriptBlock)container;

			for (var i = 0; i < maxCount; i++)
			{
				List<TreeViewItemData<NodeData>> conditionChildren = null;
				List<TreeViewItemData<NodeData>> actionChildren = null;
				String conditionBranchLabel = null;
				String actionBranchLabel = null;

				if (i < condCount)
				{
					var sequence = container.GetConditionSequence(i);
					if (sequence != null)
					{
						conditionBranchLabel = container.GetConditionSequenceName(i);
						conditionChildren = BuildBlockContainerBranch(conditionBranchLabel, sequence);
					}
				}
				if (i < actCount)
				{
					var sequence = container.GetActionSequence(i);
					if (sequence != null)
					{
						actionBranchLabel = container.GetActionSequenceName(i);
						actionChildren = BuildBlockContainerBranch(actionBranchLabel, sequence);
					}
				}

				if (conditionChildren != null)
				{
					// collate actions in same list with conditions
					if (actionChildren != null)
						conditionChildren.AddRange(actionChildren);
					result.Add(CreateTreeItem(conditionBranchLabel, NodeData.NodeKind.Branch, conditionChildren, block, i, true));
				}
				else if (actionChildren != null)
				{
					// special case: For() omits Do() branch
					if (condCount == 0)
						return actionChildren;

					result.Add(CreateTreeItem(actionBranchLabel, NodeData.NodeKind.Branch, actionChildren, block, i));
				}
			}

			return result;
		}

		private List<TreeViewItemData<NodeData>> BuildBlockContainerBranch(String name, IEnumerable<IScriptBlock> sequence)
		{
			var children = new List<TreeViewItemData<NodeData>>();
			foreach (var block in sequence)
			{
				if (block is ScriptBlock sb)
					children.Add(BuildBlockChildren(sb));
			}

			return children;
		}

		private TreeViewItemData<NodeData> CreateTreeItem(String label, NodeData.NodeKind nodeKind, List<TreeViewItemData<NodeData>> children,
			IScriptBlock block, Int32 branchIndex = -1, Boolean isCondition = false) => new(NextNodeId(), new NodeData
			{
				Label = label,
				Kind = nodeKind,
				BlockState = block is ScriptBlock b ? new ScriptBlockState(b) : null,
				BranchIndex = branchIndex,
				IsConditionBranch = isCondition,
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
			_showEvents = evt.newValue;
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

			if (Application.isPlaying)
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
				Event,
				Sequence,
				Container,
				Branch,
				Block,
			}

			public NodeKind Kind;
			public String Label;
			public ScriptBlockState BlockState; // non-null only for Block nodes
			public Boolean IsFilteredOut;
			public Int32 BranchIndex;
			public Boolean IsConditionBranch;

			public override String ToString() => $"{Kind}: {Label} => {BlockState}";
		}
	}
}
