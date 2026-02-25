using Luny.Engine.Bridge.Physics;
using UnityEngine;

namespace LunyScript.Unity.EventRelays
{
	[DisallowMultipleComponent]
	[AddComponentMenu("GameObject/")] // hide component from user interface
	[RequireComponent(typeof(Collider))]
	internal sealed class LunyScriptCollisionEventRelay : MonoBehaviourEventRelay
	{
		private LunyCollision _collision = new();

		private void OnCollisionEnter(Collision other)
		{
			_collision.NativeObject = other;
			_lunyObject.InvokeOnCollisionStarted(_collision);
		}

		private void OnCollisionExit(Collision other)
		{
			_collision.NativeObject = other;
			_lunyObject.InvokeOnCollisionEnded(_collision);
		}

		private void OnCollisionStay(Collision other)
		{
			_collision.NativeObject = other;
			_lunyObject.InvokeOnColliding(_collision);
		}
	}
}
