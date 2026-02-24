using LunyScript.Api;
using LunyScript.Blocks;
using System;

namespace LunyScript
{
	public static class ComponentApiExtensions
	{
		public static ScriptActionBlock Enable(this ComponentApi api, Type componentType) => ComponentEnableBlock.Create(componentType);
		public static ScriptActionBlock Disable(this ComponentApi api, Type componentType) => ComponentDisableBlock.Create(componentType);
	}
}
