using System;
using UnityEngine;

namespace LunyScript
{
	[AddComponentMenu("GameObject/")] // hide from "Add Component" menu
	public sealed class Rotate360 : MonoBehaviour
	{
		[SerializeField] private Vector3 _angles;
		[SerializeField] private Single _timeScale = 1f / 60f;

		private Quaternion _rotation;

		private void Awake()
		{
			if (Mathf.Approximately(_timeScale, 0f))
				_timeScale = 1f;

			UpdateRotation();
		}

		private void OnValidate() => UpdateRotation();
		private void Update() => transform.localRotation *= _rotation;
		private void UpdateRotation() => _rotation = Quaternion.Euler(_angles * _timeScale);
	}
}
