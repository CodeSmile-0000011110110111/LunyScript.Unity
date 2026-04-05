using LunyScript.Unity.UI;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor.Diagnostics
{
	internal abstract class ScriptDiagnosticsWindowController : IScriptDiagnosticsController
	{
		protected readonly VisualElement _root;
		protected readonly Label _emptyLabel;
		protected GameObject _target;
		protected String _filterText = String.Empty;
		protected IVisualElementScheduledItem _filterDebounce;
		private ScriptRuntimeContext _scriptContext;

		protected ScriptRuntimeContext ScriptContext => _scriptContext ??=
			_target != null ? ScriptEngine.Instance?.GetScriptContext(_target.GetInstanceID()) as ScriptRuntimeContext : null;

		protected ScriptDiagnosticsWindowController(VisualElement root)
		{
			_root = root;
			_emptyLabel = root.Q<Label>("empty-label");
		}

		void IScriptDiagnosticsController.Reset() => Reset();
		void IScriptDiagnosticsController.SetTargetObject(GameObject target) => SetTarget(target);

		internal abstract void Reset();

		internal virtual void SetTarget(GameObject target)
		{
			_target = target;
			_scriptContext = null;
		}

		protected void UpdateEmptyState(VisualElement content, Boolean hasContent)
		{
			if (!Application.isPlaying)
				_emptyLabel.text = "Enter Play Mode to view diagnostics.";

			content.style.display = hasContent ? DisplayStyle.Flex : DisplayStyle.None;
			_emptyLabel.style.display = hasContent ? DisplayStyle.None : DisplayStyle.Flex;
		}

		protected Boolean IsTargetValid() =>
			Application.isPlaying && ScriptEngine.Instance != null && _target != null && _target.scene.IsValid();
	}
}
