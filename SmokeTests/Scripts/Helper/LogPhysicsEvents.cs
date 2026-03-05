using Luny;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LunyScript.SmokeTests.Helper
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Collider))]
	public sealed class PhysicsEventLogger : MonoBehaviour
	{
		public Boolean LogTriggerEvents = true;
		public Boolean LogCollisionEvents = true;
		public Boolean LogStayEvents = true;

		private void OnTriggerEnter(Collider other)
		{
			if (LogTriggerEvents)
				Log(other.gameObject);
		}

		private void OnTriggerExit(Collider other)
		{
			if (LogTriggerEvents)
				Log(other.gameObject);
		}

		private void OnTriggerStay(Collider other)
		{
			if (LogTriggerEvents && LogStayEvents)
				Log(other.gameObject);
		}

		private void OnCollisionEnter(Collision other)
		{
			if (LogCollisionEvents)
				Log(other.gameObject);
		}

		private void OnCollisionExit(Collision other)
		{
			if (LogCollisionEvents)
				Log(other.gameObject);
		}

		private void OnCollisionStay(Collision other)
		{
			if (LogCollisionEvents && LogStayEvents)
				Log(other.gameObject);
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (LogTriggerEvents)
				Log(other.gameObject);
		}

		private void OnTriggerExit2D(Collider2D other)
		{
			if (LogTriggerEvents)
				Log(other.gameObject);
		}

		private void OnTriggerStay2D(Collider2D other)
		{
			if (LogTriggerEvents && LogStayEvents)
				Log(other.gameObject);
		}

		private void OnCollisionEnter2D(Collision2D other)
		{
			if (LogCollisionEvents)
				Log(other.gameObject);
		}

		private void OnCollisionExit2D(Collision2D other)
		{
			if (LogCollisionEvents)
				Log(other.gameObject);
		}

		private void OnCollisionStay2D(Collision2D other)
		{
			if (LogCollisionEvents && LogStayEvents)
				Log(other.gameObject);
		}

		private void Log(GameObject other, [CallerMemberName] String caller = "") => LunyLogger.LogInfo(
			$"{caller}: {gameObject.name} ({gameObject.GetInstanceID()}) => other: {other?.name} ({other?.GetInstanceID()})", this);
	}
}
