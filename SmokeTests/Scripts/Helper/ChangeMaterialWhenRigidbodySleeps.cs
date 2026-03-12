using System;
using System.Collections;
using UnityEngine;

namespace LunyScript.SmokeTests
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Rigidbody), typeof(Renderer))]
	internal sealed class ChangeMaterialWhenRigidbodySleeps : MonoBehaviour
	{
		[SerializeField] private Material _rigidbodySleepsMaterial;

		private Material _originalMaterial;
		private Rigidbody _rigidbody;
		private Renderer _renderer;
		private Boolean _isSleeping;

		private void Awake()
		{
			if (_rigidbodySleepsMaterial == null)
				throw new MissingReferenceException("no sleeping material assigned");

			_rigidbody = GetComponent<Rigidbody>();
			_renderer = GetComponent<Renderer>();
			_originalMaterial = _renderer.sharedMaterial;

			StartCoroutine(AfterFixedUpdate());
		}

		private void OnEnable() => _isSleeping = _rigidbody.IsSleeping();

		private IEnumerator AfterFixedUpdate()
		{
			while (true)
			{
				yield return new WaitForFixedUpdate();

				var isSleepingNow = _rigidbody.IsSleeping();
				if (isSleepingNow != _isSleeping)
				{
					_isSleeping = isSleepingNow;
					_renderer.sharedMaterial = isSleepingNow ? _rigidbodySleepsMaterial : _originalMaterial;
				}
			}
		}
	}
}
