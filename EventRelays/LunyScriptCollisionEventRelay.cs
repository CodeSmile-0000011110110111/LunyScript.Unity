using Luny;
using Luny.Unity.Bridge;
using UnityEngine;

namespace LunyScript.Unity.EventRelays
{
	[DisallowMultipleComponent]
	[AddComponentMenu("GameObject/")] // hide component from user interface
	internal sealed class LunyScriptCollisionEventRelay : MonoBehaviourEventRelay
	{
		private UnityCollision _collisionWithOther = new();

#if UNITY_EDITOR
		private void Awake()
		{
			if (!TryGetComponent<Rigidbody>(out var rigidbody))
				LunyLogger.LogWarning($"{name} w/o Rigidbody: On.Collision events run only when contact object has a Rigidbody.", _lunyObject);

			if (TryGetComponent<Collider>(out var collider))
			{
				if (collider.isTrigger)
					LunyLogger.LogWarning($"{name}'s Collider has IsTrigger set: On.Collision events will not run!", _lunyObject);

				if (collider is MeshCollider meshCol && !meshCol.convex && rigidbody != null && !rigidbody.isKinematic)
				{
					LunyLogger.LogWarning($"{name} has a non-kinematic Rigidbody with a MeshCollider whose 'Convex' property is NOT set: " +
					                      "On.Collision events will not run!", _lunyObject);
				}
			}
			else
				LunyLogger.LogWarning($"{name} has no Collider: On.Collision events will not run!", _lunyObject);
		}
#endif

		private void OnCollisionEnter(Collision other)
		{
			_collisionWithOther.SetNativeCollision(other);
			_lunyObject.InvokeOnCollisionEntered(_collisionWithOther);
			_collisionWithOther.SetNativeCollision(null);
		}

		private void OnCollisionExit(Collision other)
		{
			_collisionWithOther.SetNativeCollision(other);
			_lunyObject.InvokeOnCollisionExited(_collisionWithOther);
			_collisionWithOther.SetNativeCollision(null);
		}

		private void OnCollisionStay(Collision other)
		{
			_collisionWithOther.SetNativeCollision(other);
			_lunyObject.InvokeOnCollisionUpdate(_collisionWithOther);
			_collisionWithOther.SetNativeCollision(null);
		}
	}
}
