using Luny.UnityEditor;
using System;

namespace LunyScript.UnityEditor
{
	internal sealed class PreventCodeStrippingLinkerProcessor : LunyLinkerProcessor
	{
		public override PreserveDetails[] GetPreserveDetails()
		{
			// preserve all user scripts across assemblies (any scripts in Editor assemblies will not be in build)
			var details = PreserveAllDerivedClasses<Script>();
			return details.ToArray();
		}

		public override String GetAssemblyName() => "LunyScript.Unity";
	}
}
