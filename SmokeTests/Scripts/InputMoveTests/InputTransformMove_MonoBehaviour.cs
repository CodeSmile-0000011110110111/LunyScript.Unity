using UnityEngine;

namespace LunyScript.SmokeTests
{
	/// <summary>
	/// Typical Unity tutorial script implementing a 3-way motion relative to object's orientation
	/// => 18 lines of code, 628 characters (excluding: empty lines, comments, namespace, usings)
	/// </summary>
	[AddComponentMenu("GameObject/")] // hide from "Add Component" menu
	public sealed class InputTransformMove_MonoBehaviour : MonoBehaviour
	{
		private void Update()
		{
			var horizontal = InputHelper.GetHorizontalInput();
			var vertical = InputHelper.GetVerticalInput();
			var moveSpeed = 4f;

			transform.position += transform.forward * (vertical * moveSpeed * Time.deltaTime);
			transform.position += transform.right * (horizontal * moveSpeed * Time.deltaTime);

			if (InputHelper.GetUpKeyPressed())
				transform.position += transform.up * (moveSpeed * Time.deltaTime);

			if (InputHelper.GetDownKeyPressed())
				transform.position += -transform.up * (moveSpeed * Time.deltaTime);
		}
	}
}
