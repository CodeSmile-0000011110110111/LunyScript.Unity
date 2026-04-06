using Luny.UnityEditor;
using System;

namespace LunyScript.UnityEditor
{
	internal sealed class PreventCodeStrippingLinkerProcessor : LunyLinkerProcessor
	{
		public override PreserveDetails[] GetPreserveDetails()
		{
			// preserve all Luny scripts across assemblies (scripts in Editor assemblies will not end up in builds)
			var details = PreserveAllDerivedClasses<Script>();
			return details.ToArray();
		}

		public override String GetAssemblyName() => "LunyScript.Unity";
	}
}
