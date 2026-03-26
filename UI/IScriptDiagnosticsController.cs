using UnityEngine;

namespace LunyScript.Unity.UI
{
	internal interface IScriptDiagnosticsController
	{
		void Reset();
		void SetTargetObject(GameObject target);
	}
}
