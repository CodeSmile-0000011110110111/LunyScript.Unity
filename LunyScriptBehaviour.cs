using Luny;
using System;
using UnityEngine;

namespace LunyScript.Unity
{
	public sealed class LunyScriptBehaviour : MonoBehaviour
	{
		[SerializeField] private ScriptInspectorData _data = new();

		private void Awake()
		{
			var entityId = (Int32)GetEntityId();
			var scriptContext = ScriptEngine.Instance.GetScriptContext(entityId);

		}
	}
}
