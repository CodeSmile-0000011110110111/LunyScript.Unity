using UnityEngine;

namespace LunyScript.SmokeTests.InputMoveTests
{
	/// <summary>
	/// Typical Unity tutorial script implementing a 3-way motion relative to object's orientation
	/// => 18 lines of code, 628 characters (excluding: empty lines, comments, namespace, usings)
	/// </summary>
	public class InputTransformMove_MonoBehaviour : MonoBehaviour
	{
		private void Update()
		{
			float horizontal = InputHelper.GetHorizontalInput();
			float vertical = InputHelper.GetVerticalInput();
			float moveSpeed = 4f;

			transform.position += transform.forward * (vertical * moveSpeed * Time.deltaTime);
			transform.position += transform.right * (horizontal * moveSpeed * Time.deltaTime);

			if (InputHelper.GetUpKeyPressed())
				transform.position += transform.up * (moveSpeed * Time.deltaTime);

			if (InputHelper.GetDownKeyPressed())
				transform.position += -transform.up * (moveSpeed * Time.deltaTime);
		}
	}
}
