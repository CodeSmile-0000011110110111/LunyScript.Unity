using System;
using UnityEngine;

namespace LunyScript.Blocks
{
	internal sealed class ComponentEnableBlock : ScriptActionBlock
	{
		private Type _componentType;

		public static ScriptActionBlock Create(Type componentType) => new ComponentEnableBlock(componentType);

		internal static void SetComponentEnabled(GameObject go, Type componentType, Boolean enabled)
		{
			var components = go.GetComponentsInChildren(componentType);
			foreach (var component in components)
			{
				switch (component)
				{
					case Behaviour behaviour:
						behaviour.enabled = enabled;
						break;
					case Collider collider:
						collider.enabled = enabled;
						break;
					case Renderer renderer:
						renderer.enabled = enabled;
						break;
					case LODGroup lodGroup:
						lodGroup.enabled = enabled;
						break;
					case Cloth cloth:
						cloth.enabled = enabled;
						break;
				}
			}
		}

		private ComponentEnableBlock() {}

		private ComponentEnableBlock(Type componentType) => _componentType = componentType;

		protected internal override void Execute(IScriptRuntimeContext runtimeContext)
		{
			var go = runtimeContext.LunyObject.Cast<GameObject>();
			SetComponentEnabled(go, _componentType, true);
		}

		public override String ToString() => $"{nameof(ComponentEnableBlock)}({_componentType?.Name})";
	}

	internal sealed class ComponentDisableBlock : ScriptActionBlock
	{
		private Type _componentType;

		public static ScriptActionBlock Create(Type componentType) => new ComponentDisableBlock(componentType);

		private ComponentDisableBlock() {}

		private ComponentDisableBlock(Type componentType) => _componentType = componentType;

		protected internal override void Execute(IScriptRuntimeContext runtimeContext)
		{
			var go = runtimeContext.LunyObject.Cast<GameObject>();
			ComponentEnableBlock.SetComponentEnabled(go, _componentType, false);
		}

		public override String ToString() => $"{nameof(ComponentDisableBlock)}({_componentType?.Name})";
	}
}
