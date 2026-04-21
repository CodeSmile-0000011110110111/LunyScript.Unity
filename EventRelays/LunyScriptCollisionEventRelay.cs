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
				LunyLogger.LogWarning($"{name} w/o Rigidbody: On.Collision events run only when contact object has a Rigidbody.", _lunyGameObject);

			if (TryGetComponent<Collider>(out var collider))
			{
				if (collider.isTrigger)
					LunyLogger.LogWarning($"{name}'s Collider has IsTrigger set: On.Collision events will not run!", _lunyGameObject);

				if (collider is MeshCollider meshCol && !meshCol.convex && rigidbody != null && !rigidbody.isKinematic)
				{
					LunyLogger.LogWarning($"{name} has a non-kinematic Rigidbody with a MeshCollider whose 'Convex' property is NOT set: " +
					                      "On.Collision events will not run!", _lunyGameObject);
				}
			}
			else
				LunyLogger.LogWarning($"{name} has no Collider: On.Collision events will not run!", _lunyGameObject);
		}
#endif

		private void OnCollisionEnter(Collision other)
		{
			_collisionWithOther.SetNativeCollision(other);
			_lunyGameObject.InvokeOnCollisionEntered(_collisionWithOther);
			_collisionWithOther.SetNativeCollision(null);
		}

		private void OnCollisionExit(Collision other)
		{
			_collisionWithOther.SetNativeCollision(other);
			_lunyGameObject.InvokeOnCollisionExited(_collisionWithOther);
			_collisionWithOther.SetNativeCollision(null);
		}

		private void OnCollisionStay(Collision other)
		{
			_collisionWithOther.SetNativeCollision(other);
			_lunyGameObject.InvokeOnCollisionUpdate(_collisionWithOther);
			_collisionWithOther.SetNativeCollision(null);
		}
	}
}
