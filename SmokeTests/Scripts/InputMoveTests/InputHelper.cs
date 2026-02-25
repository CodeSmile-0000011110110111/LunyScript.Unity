using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LunyScript.SmokeTests.InputMoveTests
{
	public static class InputHelper
	{
		public static Vector2 GetMovementInput()
		{
#if ENABLE_INPUT_SYSTEM
			// You shouldn't write Input System code like this.
			// I only do so to avoid setting up input actions.
			var keyboard = Keyboard.current;
			if (keyboard == null)
				return Vector2.zero;

			var x = keyboard.dKey.ReadValue() - keyboard.aKey.ReadValue();
			var y = keyboard.wKey.ReadValue() - keyboard.sKey.ReadValue();
			var input = new Vector2(x, y);
			return input.normalized;
#elif UNITY_6000_OR_NEWER
			return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
#else
			return Vector2.zero;
#endif
		}

		public static Single GetHorizontalInput() => GetMovementInput().x;
		public static Single GetVerticalInput() => GetMovementInput().y;

		public static Boolean GetUpKeyPressed()
		{
#if ENABLE_INPUT_SYSTEM
			return Keyboard.current.spaceKey.isPressed;
#elif UNITY_6000_OR_NEWER
			return Input.GetKey(KeyCode.Space);
#else
			return false;
#endif
		}

		public static Boolean GetDownKeyPressed()
		{
#if ENABLE_INPUT_SYSTEM
			return Keyboard.current.cKey.isPressed;
#elif UNITY_6000_OR_NEWER
			return Input.GetKey(KeyCode.C);
#else
			return false;
#endif
		}
	}
}
