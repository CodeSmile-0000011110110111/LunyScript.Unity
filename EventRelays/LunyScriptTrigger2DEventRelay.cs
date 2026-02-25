using UnityEngine;

namespace LunyScript.Unity.EventRelays
{
	[DisallowMultipleComponent]
	[AddComponentMenu("GameObject/")] // hide component from user interface
	[RequireComponent(typeof(Collider2D))]
	internal sealed class LunyScriptTrigger2DEventRelay : MonoBehaviourEventRelay
	{
		private void OnTriggerEnter2D(Collider2D other) {}
		private void OnTriggerStay2D(Collider2D other) {}
		private void OnTriggerExit2D(Collider2D other) {}
	}
}
