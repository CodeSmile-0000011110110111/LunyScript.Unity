using Luny.Unity.Engine.Bridge.Physics;
using System;
using UnityEngine;

namespace LunyScript.Unity.EventRelays
{
	[DisallowMultipleComponent]
	[AddComponentMenu("GameObject/")] // hide component from user interface
	[RequireComponent(typeof(Collider))]
	internal sealed class LunyScriptTriggerEventRelay : MonoBehaviourEventRelay
	{
		private UnityCollider _otherCollider = new();

		private void OnTriggerEnter(Collider other)
		{
			_otherCollider.SetNativeCollider(other);
			_lunyObject.InvokeOnTriggerEntered(_otherCollider);
			_otherCollider.SetNativeCollider(null);
		}

		private void OnTriggerExit(Collider other)
		{
			_otherCollider.SetNativeCollider(other);
			_lunyObject.InvokeOnTriggerExited(_otherCollider);
			_otherCollider.SetNativeCollider(null);
		}

		private void OnTriggerStay(Collider other)
		{
			_otherCollider.SetNativeCollider(other);
			_lunyObject.InvokeOnTriggerUpdate(_otherCollider);
			_otherCollider.SetNativeCollider(null);
		}
	}
}
