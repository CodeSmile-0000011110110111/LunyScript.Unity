using Luny.UnityEditor.Linking;
using System;

namespace LunyScript.UnityEditor
{
	internal sealed class PreventCodeStrippingLinkerProcessor : LunyLinkerProcessor
	{
		public override PreserveDetails[] GetPreserveDetails()
		{
			// preserve all user scripts across assemblies (any scripts in Editor assemblies will not be in build)
			var details = PreserveAllDerivedClasses<Script>();

			details.Add(new PreserveDetails
			{
				Assembly = nameof(Script),
				Types = new[]
				{
					// script runner is discovered through reflection
					typeof(LunyScriptRunner).FullName,
				},
			});

			return details.ToArray();
		}

		public override String GetAssemblyName() => $"{nameof(Script)}.Unity";
	}
}
