using LunyEditor;
using LunyScript;
using System;

namespace LunyScriptEditor
{
	internal sealed class PreventCodeStrippingLinkerProcessor : LunyLinkerProcessor
	{
		public override PreserveDetails[] GetPreserveDetails()
		{
			// preserve all user scripts across assemblies (any scripts in Editor assemblies will not be in build)
			var details = PreserveAllDerivedClasses<LunyScript.LunyScript>();

			details.Add(new PreserveDetails
			{
				Assembly = nameof(LunyScript),
				Types = new[]
				{
					// script runner is discovered through reflection
					typeof(LunyScriptRunner).FullName,
				},
			});

			return details.ToArray();
		}

		public override String GetAssemblyName() => $"{nameof(LunyScript)}.Unity";
	}
}
