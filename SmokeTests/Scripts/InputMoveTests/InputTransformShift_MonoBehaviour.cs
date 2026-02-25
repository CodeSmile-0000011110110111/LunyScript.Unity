using UnityEngine;

namespace LunyScript.SmokeTests.InputMoveTests
{
	/// <summary>
	/// Typical Unity tutorial script implementing a 3-way world-axis-aligned motion
	/// => 33 lines of code, 865 characters (excluding: empty lines, comments, namespace, usings)
	/// </summary>
	public class InputTransformShift_MonoBehaviour : MonoBehaviour
	{
		private void Update()
		{
			float horizontal = InputHelper.GetHorizontalInput();
			float vertical = InputHelper.GetVerticalInput();
			float shiftSpeed = 4f;

			transform.position = new Vector3(
				transform.position.x + horizontal * shiftSpeed * Time.deltaTime,
				transform.position.y,
				transform.position.z + vertical * shiftSpeed * Time.deltaTime
			);

			if (InputHelper.GetUpKeyPressed())
			{
				transform.position = new Vector3(
					transform.position.x,
					transform.position.y + shiftSpeed * Time.deltaTime,
					transform.position.z
				);
			}

			if (InputHelper.GetDownKeyPressed())
			{
				transform.position = new Vector3(
					transform.position.x,
					transform.position.y + -shiftSpeed * Time.deltaTime,
					transform.position.z
				);
			}
		}
	}
}
