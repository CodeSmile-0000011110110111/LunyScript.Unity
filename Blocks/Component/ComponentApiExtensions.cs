using LunyScript.Api;
using LunyScript.Blocks;
using System;

namespace LunyScript.Unity.Blocks
{
	public static class ComponentApiExtensions
	{
		public static ActionBlock Enable(this ComponentApi api, Type componentType) => ComponentEnableBlock.Create(componentType);
		public static ActionBlock Disable(this ComponentApi api, Type componentType) => ComponentDisableBlock.Create(componentType);
	}
}
