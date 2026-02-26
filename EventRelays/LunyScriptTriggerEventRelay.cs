using Luny.Engine.Bridge.Physics;
using UnityEngine;

namespace LunyScript.Unity.EventRelays
{
	[DisallowMultipleComponent]
	[AddComponentMenu("GameObject/")] // hide component from user interface
	[RequireComponent(typeof(Collider))]
	internal sealed class LunyScriptTriggerEventRelay : MonoBehaviourEventRelay
	{
		private LunyCollider _collider = new();

		private void OnTriggerEnter(Collider other)
		{
			_collider.NativeObject = other;
			_lunyObject.InvokeOnTriggerEntered(_collider);
		}

		private void OnTriggerStay(Collider other)
		{
			_collider.NativeObject = other;
			_lunyObject.InvokeOnTriggerUpdate(_collider);
		}

		private void OnTriggerExit(Collider other)
		{
			_collider.NativeObject = other;
			_lunyObject.InvokeOnTriggerExited(_collider);
		}
	}
}
