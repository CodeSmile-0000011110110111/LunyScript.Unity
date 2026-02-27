using Luny.Unity.Engine.Bridge.Physics;
using UnityEngine;

namespace LunyScript.Unity.EventRelays
{
	[DisallowMultipleComponent]
	[AddComponentMenu("GameObject/")] // hide component from user interface
	[RequireComponent(typeof(Collider))]
	internal sealed class LunyScriptTriggerEventRelay : MonoBehaviourEventRelay
	{
		private UnityCollider _collider = new();

		private void OnTriggerEnter(Collider other)
		{
			_collider.SetNativeObject(other);
			_lunyObject.InvokeOnTriggerEntered(_collider);
			_collider.SetNativeObject(null);
		}

		private void OnTriggerStay(Collider other)
		{
			_collider.SetNativeObject(other);
			_lunyObject.InvokeOnTriggerUpdate(_collider);
			_collider.SetNativeObject(null);
		}

		private void OnTriggerExit(Collider other)
		{
			_collider.SetNativeObject(other);
			_lunyObject.InvokeOnTriggerExited(_collider);
			_collider.SetNativeObject(null);
		}
	}
}
