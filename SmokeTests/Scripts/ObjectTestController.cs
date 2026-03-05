using LunyScript.SmokeTests.Object;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LunyScript.SmokeTests
{
	public sealed class ObjectTestController : MonoBehaviour
	{
		[SerializeField] private Boolean _ObjectCreatedAndDestroyed;
		[SerializeField] private Boolean _EmptyObjectCreated;
		[SerializeField] private Boolean _CubeObjectCreated;
		[SerializeField] private Boolean _SphereObjectCreated;

		private Boolean _foundToBeDestroyedObject;

		private void Awake()
		{
			// this causes the corresponding LunyScript (ObjectTestScript) to build & run
			new GameObject(nameof(ObjectTestScript));

			StartCoroutine(AssertAtEndOfFrame());
		}

		private void Start()
		{
			var toBeDestroyed = GameObject.Find(ObjectTestScript.DestroyedObjectName);
			_foundToBeDestroyedObject = toBeDestroyed != null;
		}

		private void AssertObjectsCreated()
		{
			var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
			Debug.Log($"Scene has {rootObjects.Length} root objects");

			foreach (var rootObject in rootObjects)
			{
				var goName = rootObject.name;
				_EmptyObjectCreated = _EmptyObjectCreated || goName == ObjectTestScript.EmptyObjectName;
				_CubeObjectCreated = _CubeObjectCreated || goName == ObjectTestScript.CubeObjectName;
				_SphereObjectCreated = _SphereObjectCreated || goName == ObjectTestScript.SphereObjectName;
			}

			_ObjectCreatedAndDestroyed = _foundToBeDestroyedObject && GameObject.Find(ObjectTestScript.DestroyedObjectName) == null;
		}

		private IEnumerator AssertAtEndOfFrame()
		{
			yield return new WaitForEndOfFrame();

			AssertObjectsCreated();
		}
	}
}
