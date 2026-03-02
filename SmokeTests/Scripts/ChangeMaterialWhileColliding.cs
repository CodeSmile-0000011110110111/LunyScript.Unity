using System;
using UnityEngine;

namespace LunyScript
{
	[RequireComponent(typeof(Renderer))]
	public sealed class ChangeMaterialWhileColliding : MonoBehaviour
	{
		public Material CollisionMaterial;
		public Material TriggerMaterial;

		private Material _originalMaterial;
		private Renderer _renderer;

		private Int32 _collisionCount;
		private Int32 _triggerCount;

		private void Awake()
		{
			if (CollisionMaterial == null && TriggerMaterial == null)
				throw new ArgumentException("no collision or trigger material assigned");

			_renderer = GetComponent<Renderer>();
			_originalMaterial = _renderer.sharedMaterial;
		}

		private void OnTriggerEnter(Collider other)
		{
			_triggerCount++;
			SetMaterial(TriggerMaterial);
		}

		private void OnTriggerExit(Collider other)
		{
			_triggerCount = Mathf.Max(0, --_triggerCount);
			if (_triggerCount == 0)
				SetMaterial(_originalMaterial);
		}

		private void OnCollisionEnter(Collision other)
		{
			_collisionCount++;
			SetMaterial(CollisionMaterial);
		}

		private void OnCollisionExit(Collision other)
		{
			_collisionCount = Mathf.Max(0, --_collisionCount);
			if (_collisionCount == 0)
				SetMaterial(_originalMaterial);
		}

		private void OnTriggerEnter2D(Collider2D other) => OnTriggerEnter(null);
		private void OnTriggerExit2D(Collider2D other) => OnTriggerExit(null);
		private void OnCollisionEnter2D(Collision2D other) => OnCollisionEnter(null);
		private void OnCollisionExit2D(Collision2D other) => OnCollisionExit(null);

		private void SetMaterial(Material material)
		{
			if (material != null)
				_renderer.sharedMaterial = material;
		}
	}
}
