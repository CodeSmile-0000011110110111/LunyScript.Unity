using System;
using UnityEngine;

namespace LunyScript.Unity.EventRelays
{
	[DisallowMultipleComponent]
	[AddComponentMenu("GameObject/")] // hide component from user interface
	[RequireComponent(typeof(Collider2D))]
	internal sealed class LunyScriptCollision2DEventRelay : MonoBehaviourEventRelay
	{
		private void Awake() => throw new NotImplementedException(nameof(LunyScriptCollision2DEventRelay));

		private void OnCollisionEnter2D(Collision2D other) {}
		private void OnCollisionStay2D(Collision2D other) {}
		private void OnCollisionExit2D(Collision2D other) {}
	}
}
