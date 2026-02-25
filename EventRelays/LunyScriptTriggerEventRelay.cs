using UnityEngine;

namespace LunyScript.Unity.EventRelays
{
	[DisallowMultipleComponent]
	[AddComponentMenu("GameObject/")] // hide component from user interface
	[RequireComponent(typeof(Collider))]
	internal sealed class LunyScriptTriggerEventRelay : MonoBehaviourEventRelay
	{
		private void OnTriggerEnter(Collider other) {}
		private void OnTriggerStay(Collider other) {}
		private void OnTriggerExit(Collider other) {}
	}
}
