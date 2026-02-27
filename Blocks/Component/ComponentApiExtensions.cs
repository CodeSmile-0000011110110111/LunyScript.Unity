using LunyScript.Api.Blocks;
using LunyScript.Blocks;
using System;

namespace LunyScript.Unity
{
	public static class ComponentApiExtensions
	{
		public static ScriptActionBlock Enable(this ComponentApi api, Type componentType) => ComponentEnableBlock.Create(componentType);
		public static ScriptActionBlock Disable(this ComponentApi api, Type componentType) => ComponentDisableBlock.Create(componentType);
	}
}
