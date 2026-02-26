using Luny.Engine.Bridge;
using Luny.Engine.Bridge.Physics;
using LunyScript.Unity.EventRelays;
using UnityEngine;

namespace LunyScript.Unity.Adapters
{
	internal sealed class LunyScriptMonoBehaviourEventRelayInstaller
	{
		internal void Initialize()
		{
			var scriptEngine = (IScriptEngineInternal)ScriptEngine.Instance;
			scriptEngine.OnScriptBuilt += OnScriptBuilt;
		}

		internal void Shutdown()
		{
			var scriptEngine = (IScriptEngineInternal)ScriptEngine.Instance;
			if (scriptEngine != null)
				scriptEngine.OnScriptBuilt -= OnScriptBuilt;
		}

		private void OnScriptBuilt(ScriptRuntimeContext runtimeContext)
		{
			var scheduler = runtimeContext.Scheduler;
			var hasCollisionEvents = scheduler.IsObservingAnyOf(typeof(LunyCollisionEvent));
			var hasTriggerEvents = scheduler.IsObservingAnyOf(typeof(LunyTriggerEvent));
			var hasCollision2DEvents = scheduler.IsObservingAnyOf(typeof(LunyCollision2DEvent));
			var hasTrigger2DEvents = scheduler.IsObservingAnyOf(typeof(LunyTrigger2DEvent));

			GameObject gameObject = null;
			if (hasCollisionEvents || hasTriggerEvents || hasCollision2DEvents || hasTrigger2DEvents)
				gameObject = (GameObject)runtimeContext.LunyObject.NativeObject;

			if (hasCollisionEvents)
				gameObject.AddComponent<LunyScriptCollisionEventRelay>().Initialize(runtimeContext);
			if (hasTriggerEvents)
				gameObject.AddComponent<LunyScriptTriggerEventRelay>().Initialize(runtimeContext);
			if (hasCollision2DEvents)
				gameObject.AddComponent<LunyScriptCollision2DEventRelay>().Initialize(runtimeContext);
			if (hasTrigger2DEvents)
				gameObject.AddComponent<LunyScriptTrigger2DEventRelay>().Initialize(runtimeContext);
		}
	}
}
