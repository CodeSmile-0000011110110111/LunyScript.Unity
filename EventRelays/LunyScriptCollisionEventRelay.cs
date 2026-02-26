using Luny;
using Luny.Engine.Bridge.Physics;
using UnityEngine;

namespace LunyScript.Unity.EventRelays
{
	[DisallowMultipleComponent]
	[AddComponentMenu("GameObject/")] // hide component from user interface
	internal sealed class LunyScriptCollisionEventRelay : MonoBehaviourEventRelay
	{
		private LunyCollision _collision = new();

#if UNITY_EDITOR
		private void Awake()
		{
			if (!TryGetComponent<Rigidbody>(out var rigidbody))
			{
				// Technically, the *other* object could have the RB, but it usually needs its own.
				LunyLogger.LogWarning("Collision events in script but object has no Rigidbody. Collision events will only run " +
				                      "if the colliding object has a Rigidbody.", _lunyObject);
			}

			if (TryGetComponent<Collider>(out var collider))
			{
				if (collider.isTrigger)
				{
					LunyLogger.LogWarning("Collision events in script but object's Collider has IsTrigger set. " +
					                      "Disregard this message if IsTrigger is set at runtime.", _lunyObject);
				}

				if (collider is MeshCollider meshCol && !meshCol.convex && rigidbody != null && !rigidbody.isKinematic)
				{
					LunyLogger.LogWarning("Collision events in script but object has a non-kinematic Rigidbody with a MeshCollider " +
					                      "which is not set to be 'Convex'!", _lunyObject);
				}
			}
			else
			{
				LunyLogger.LogWarning($"Collision events in script but object has no Collider. Will add default collider.", _lunyObject);
				gameObject.AddComponent<SphereCollider>();
			}
		}
#endif

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
